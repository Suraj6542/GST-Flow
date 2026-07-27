using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Hangfire;
using Hangfire.MemoryStorage;
using GstInvoiceTool.Api.Middleware;
using GstInvoiceTool.Api.Repositories;
using GstInvoiceTool.Api.Services;
using GstInvoiceTool.Api.Jobs;

var builder = WebApplication.CreateBuilder(args);

// ─── MongoDB ──────────────────────────────────────────────────
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
    ?? throw new InvalidOperationException("MongoDB:ConnectionString is not configured");
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "gst_invoice_tool";

var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// ─── Repositories ─────────────────────────────────────────────
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<ClientRepository>();
builder.Services.AddSingleton<InvoiceRepository>();
builder.Services.AddSingleton<RecurringTemplateRepository>();

// ─── Services ─────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddSingleton<CounterService>();
builder.Services.AddSingleton<TaxCalculationService>();
builder.Services.AddSingleton<PdfService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<RecurringTemplateService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// ─── Hangfire Background Jobs ─────────────────────────────
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseMemoryStorage();
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
});

builder.Services.AddTransient<RecurringInvoiceJob>();
builder.Services.AddTransient<PaymentReminderJob>();

// ─── JWT Authentication ───────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ─── Rate Limiting ────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 5;
    });

    options.RejectionStatusCode = 429;
});

// ─── CORS ─────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─── Controllers ──────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Hangfire Dashboard Route
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
});

// Register Cron Background Jobs
RecurringJob.AddOrUpdate<RecurringInvoiceJob>(
    "recurring-invoice-generation",
    job => job.ProcessDueRecurringInvoicesAsync(),
    Cron.Daily());

RecurringJob.AddOrUpdate<PaymentReminderJob>(
    "payment-overdue-reminders",
    job => job.ProcessPaymentRemindersAsync(),
    Cron.Daily());

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Seed demo data if database is empty
try
{
    await GstInvoiceTool.Api.Data.SeedData.SeedAsync(mongoDatabase);
}
catch (Exception ex)
{
    Console.WriteLine($"Seed data notice: {ex.Message}");
}

app.Run();
