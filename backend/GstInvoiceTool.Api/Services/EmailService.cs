namespace GstInvoiceTool.Api.Services;

using System.Net;
using System.Net.Mail;
using GstInvoiceTool.Api.Models;

public interface IEmailService
{
    Task SendInvoiceEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal amount, byte[] pdfBytes, User? owner = null);
    Task SendPaymentReminderEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal balanceDue, DateTime dueDate, bool isOverdue, User? owner = null);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendInvoiceEmailAsync(
        string toEmail,
        string clientName,
        string invoiceNumber,
        decimal amount,
        byte[] pdfBytes,
        User? owner = null)
    {
        var senderEmail = owner?.Email;
        var senderName = !string.IsNullOrWhiteSpace(owner?.Business?.Name) ? owner.Business.Name : owner?.Name;

        _logger.LogInformation("==================================================");
        _logger.LogInformation("📧 [EMAIL JOB] Sending Invoice {InvoiceNumber} to {Client} <{Email}>", invoiceNumber, clientName, toEmail);
        _logger.LogInformation("   Sender Owner: {SenderName} <{SenderEmail}>", senderName ?? "Default Sender", senderEmail ?? "Default Email");
        _logger.LogInformation("   Grand Total: ₹{Amount:N2} | PDF Attachment Size: {Size} bytes", amount, pdfBytes.Length);
        _logger.LogInformation("==================================================");

        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
        {
            _logger.LogError("❌ [EMAIL ERROR] Cannot send email. '{Email}' is not a valid email address.", toEmail);
            return;
        }

        // Per-owner custom SMTP settings first, falling back to appsettings.json
        var smtpHost = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.SmtpHost)
            ? owner.SmtpConfig.SmtpHost
            : _config["Email:SmtpHost"];

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("⚠️ Cannot send email: SMTP Host not configured for owner or appsettings.");
            throw new AppException("SMTP server is not configured. Please go to Email Settings in the sidebar and save your Gmail/Outlook SMTP credentials.", 400);
        }

        try
        {
            var smtpPort = owner?.SmtpConfig?.SmtpPort > 0
                ? owner.SmtpConfig.SmtpPort
                : int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.SmtpUser)
                ? owner.SmtpConfig.SmtpUser
                : _config["Email:SmtpUser"];
            var smtpPass = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.SmtpPass)
                ? owner.SmtpConfig.SmtpPass
                : _config["Email:SmtpPass"];
            var fromEmail = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.FromEmail)
                ? owner.SmtpConfig.FromEmail
                : (!string.IsNullOrWhiteSpace(owner?.Email) ? owner.Email : (_config["Email:FromEmail"] ?? "invoices@gstflow.com"));
            var displayName = !string.IsNullOrWhiteSpace(senderName) ? senderName : "GSTFlow Invoicing";

            if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
            {
                throw new AppException("SMTP Username or Password is missing. Please update your credentials under Email Settings.", 400);
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, displayName),
                Subject = $"Tax Invoice #{invoiceNumber} from {displayName}",
                Body = $"Dear {clientName},\n\nPlease find attached tax invoice #{invoiceNumber} for the amount of ₹{amount:N2}.\n\nThank you for your business!\n\nBest regards,\n{displayName}",
                IsBodyHtml = false
            };

            if (!string.IsNullOrWhiteSpace(senderEmail) && senderEmail.Contains('@'))
            {
                message.ReplyToList.Add(new MailAddress(senderEmail, displayName));
            }
            message.To.Add(toEmail);

            using var ms = new MemoryStream(pdfBytes);
            message.Attachments.Add(new Attachment(ms, $"{invoiceNumber}.pdf", "application/pdf"));

            await client.SendMailAsync(message);
            _logger.LogInformation("✅ Invoice email sent successfully via SMTP from {From} to {To}", fromEmail, toEmail);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email via SMTP");
            throw new AppException($"SMTP delivery failed: {ex.Message}", 400);
        }
    }

    public async Task SendPaymentReminderEmailAsync(
        string toEmail,
        string clientName,
        string invoiceNumber,
        decimal balanceDue,
        DateTime dueDate,
        bool isOverdue,
        User? owner = null)
    {
        var subject = isOverdue
            ? $"⚠️ OVERDUE PAYMENT REMINDER: Invoice #{invoiceNumber}"
            : $"🔔 Payment Reminder: Invoice #{invoiceNumber} Due Soon";

        var senderEmail = owner?.Email;
        var senderName = !string.IsNullOrWhiteSpace(owner?.Business?.Name) ? owner.Business.Name : owner?.Name;

        _logger.LogInformation("==================================================");
        _logger.LogInformation("📧 [REMINDER JOB] Sending Reminder for Invoice {InvoiceNumber} to {Client} <{Email}>", invoiceNumber, clientName, toEmail);
        _logger.LogInformation("   Balance Due: ₹{Balance:N2} | Due Date: {DueDate:dd MMM yyyy} | Overdue: {IsOverdue}", balanceDue, dueDate, isOverdue);
        _logger.LogInformation("==================================================");

        var smtpHost = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.SmtpHost)
            ? owner.SmtpConfig.SmtpHost
            : _config["Email:SmtpHost"];

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("⚠️ Cannot send reminder email: SMTP Host not configured.");
            throw new AppException("SMTP server is not configured. Please go to Email Settings in the sidebar and save your Gmail/Outlook SMTP credentials.", 400);
        }

        try
        {
            var smtpPort = owner?.SmtpConfig?.SmtpPort > 0
                ? owner.SmtpConfig.SmtpPort
                : int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.SmtpUser)
                ? owner.SmtpConfig.SmtpUser
                : _config["Email:SmtpUser"];
            var smtpPass = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.SmtpPass)
                ? owner.SmtpConfig.SmtpPass
                : _config["Email:SmtpPass"];
            var fromEmail = !string.IsNullOrWhiteSpace(owner?.SmtpConfig?.FromEmail)
                ? owner.SmtpConfig.FromEmail
                : (!string.IsNullOrWhiteSpace(owner?.Email) ? owner.Email : (_config["Email:FromEmail"] ?? "reminders@gstflow.com"));
            var displayName = !string.IsNullOrWhiteSpace(senderName) ? senderName : "GSTFlow Reminders";

            if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
            {
                throw new AppException("SMTP Username or Password is missing. Please update your credentials under Email Settings.", 400);
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, displayName),
                Subject = subject,
                Body = $"Dear {clientName},\n\nThis is a friendly reminder regarding tax invoice #{invoiceNumber}.\n\nBalance Due: ₹{balanceDue:N2}\nDue Date: {dueDate:dd MMM yyyy}\n\nPlease arrange for payment at your earliest convenience.\n\nThank you,\n{displayName}",
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("✅ Reminder email sent successfully via SMTP from {From} to {To}", fromEmail, toEmail);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder email via SMTP");
            throw new AppException($"SMTP delivery failed: {ex.Message}", 400);
        }
    }
}
