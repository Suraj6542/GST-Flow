namespace GstInvoiceTool.Api.Jobs;

using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;
using GstInvoiceTool.Api.Services;

public class PaymentReminderJob
{
    private readonly InvoiceRepository _invoiceRepo;
    private readonly ClientRepository _clientRepo;
    private readonly UserRepository _userRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentReminderJob> _logger;

    public PaymentReminderJob(
        InvoiceRepository invoiceRepo,
        ClientRepository clientRepo,
        UserRepository userRepo,
        IEmailService emailService,
        ILogger<PaymentReminderJob> logger)
    {
        _invoiceRepo = invoiceRepo;
        _clientRepo = clientRepo;
        _userRepo = userRepo;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessPaymentRemindersAsync()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("🚀 [HANGFIRE JOB] Checking for invoices needing payment reminders...");

        // 1. Mark overdue invoices automatically
        var overdueInvoices = await _invoiceRepo.GetOverdueInvoicesAsync();
        foreach (var inv in overdueInvoices)
        {
            await _invoiceRepo.UpdateStatusAsync(inv.Id, inv.OwnerId, InvoiceStatus.Overdue);
            _logger.LogInformation("⚠️ Auto-marked invoice #{Number} as OVERDUE", inv.InvoiceNumber);

            var client = await _clientRepo.GetByIdAsync(inv.ClientId, inv.OwnerId);
            var owner = await _userRepo.GetByIdAsync(inv.OwnerId);

            if (client != null)
            {
                await _emailService.SendPaymentReminderEmailAsync(
                    client.Email, client.Name, inv.InvoiceNumber, inv.BalanceDue, inv.DueDate, isOverdue: true, owner: owner);
            }
        }

        _logger.LogInformation("Completed payment reminders check. Processed {Count} overdue invoice(s).", overdueInvoices.Count);
    }
}
