namespace GstInvoiceTool.Api.Jobs;

using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;
using GstInvoiceTool.Api.Services;

public class RecurringInvoiceJob
{
    private readonly RecurringTemplateRepository _templateRepo;
    private readonly InvoiceService _invoiceService;
    private readonly UserRepository _userRepo;
    private readonly ClientRepository _clientRepo;
    private readonly PdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RecurringInvoiceJob> _logger;

    public RecurringInvoiceJob(
        RecurringTemplateRepository templateRepo,
        InvoiceService invoiceService,
        UserRepository userRepo,
        ClientRepository clientRepo,
        PdfService pdfService,
        IEmailService emailService,
        ILogger<RecurringInvoiceJob> logger)
    {
        _templateRepo = templateRepo;
        _invoiceService = invoiceService;
        _userRepo = userRepo;
        _clientRepo = clientRepo;
        _pdfService = pdfService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessDueRecurringInvoicesAsync()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("🚀 [HANGFIRE JOB] Processing recurring invoices due as of {Time}", now);

        var dueTemplates = await _templateRepo.GetDueTemplatesAsync(now);
        _logger.LogInformation("Found {Count} recurring invoice template(s) due for creation.", dueTemplates.Count);

        foreach (var template in dueTemplates)
        {
            try
            {
                var owner = await _userRepo.GetByIdAsync(template.OwnerId);
                if (owner == null) continue;

                var lineItemRequests = template.LineItems.Select(li => new LineItemRequest
                {
                    Description = li.Description,
                    HsnCode = li.HsnCode,
                    Quantity = li.Quantity,
                    Rate = li.Rate,
                    TaxRate = li.TaxRate
                }).ToList();

                var createReq = new InvoiceCreateRequest
                {
                    ClientId = template.ClientId,
                    IssueDate = now,
                    DueDate = now.AddDays(15),
                    LineItems = lineItemRequests,
                    Notes = template.Notes ?? "Auto-generated recurring invoice."
                };

                // Auto-create invoice using core InvoiceService
                var invoiceResponse = await _invoiceService.CreateAsync(
                    createReq, template.OwnerId, owner.Business.State);

                // Auto-mark sent so it's ready for payment
                await _invoiceService.UpdateStatusAsync(invoiceResponse.Id, InvoiceStatus.Sent, template.OwnerId);

                _logger.LogInformation("✅ Auto-generated invoice #{Number} for client {Client}",
                    invoiceResponse.InvoiceNumber, template.ClientName);

                // If autoSendEmail enabled, send PDF attachment via email!
                if (template.AutoSendEmail)
                {
                    var client = await _clientRepo.GetByIdAsync(template.ClientId, template.OwnerId);
                    if (client != null && !string.IsNullOrWhiteSpace(client.Email))
                    {
                        var invoice = new Invoice
                        {
                            InvoiceNumber = invoiceResponse.InvoiceNumber,
                            IssueDate = invoiceResponse.IssueDate,
                            DueDate = invoiceResponse.DueDate,
                            ClientName = invoiceResponse.ClientName,
                            ClientState = invoiceResponse.ClientState,
                            ClientGstin = invoiceResponse.ClientGstin,
                            Subtotal = invoiceResponse.Subtotal,
                            Cgst = invoiceResponse.Cgst,
                            Sgst = invoiceResponse.Sgst,
                            Igst = invoiceResponse.Igst,
                            TotalTax = invoiceResponse.TotalTax,
                            GrandTotal = invoiceResponse.GrandTotal,
                            Status = invoiceResponse.Status,
                            LineItems = template.LineItems
                        };

                        var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, owner);
                        await _emailService.SendInvoiceEmailAsync(
                            client.Email,
                            template.ClientName,
                            invoiceResponse.InvoiceNumber,
                            invoiceResponse.GrandTotal,
                            pdfBytes,
                            owner);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Client email not found for client {ClientId}, skipping email dispatch.", template.ClientId);
                    }
                }

                // Advance next run date according to frequency
                template.LastRunDate = now;
                template.NextRunDate = template.Frequency.ToLower() switch
                {
                    "weekly" => template.NextRunDate.AddDays(7),
                    "quarterly" => template.NextRunDate.AddMonths(3),
                    _ => template.NextRunDate.AddMonths(1) // Default monthly
                };

                await _templateRepo.UpdateAsync(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring template {Id}", template.Id);
            }
        }
    }
}
