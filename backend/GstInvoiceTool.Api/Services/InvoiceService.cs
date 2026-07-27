namespace GstInvoiceTool.Api.Services;

using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;

public class InvoiceService
{
    private readonly InvoiceRepository _invoiceRepo;
    private readonly ClientRepository _clientRepo;
    private readonly CounterService _counterService;
    private readonly TaxCalculationService _taxService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        InvoiceRepository invoiceRepo,
        ClientRepository clientRepo,
        CounterService counterService,
        TaxCalculationService taxService,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepo = invoiceRepo;
        _clientRepo = clientRepo;
        _counterService = counterService;
        _taxService = taxService;
        _logger = logger;
    }

    public async Task<List<InvoiceResponse>> GetAllAsync(
        string ownerId, InvoiceFilterQuery? filter = null)
    {
        var invoices = await _invoiceRepo.GetByOwnerAsync(
            ownerId, filter?.Status, filter?.ClientId,
            filter?.FromDate, filter?.ToDate);

        return invoices.Select(MapToResponse).ToList();
    }

    public async Task<InvoiceResponse> GetByIdAsync(string id, string ownerId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(id, ownerId);
        if (invoice == null)
            throw new AppException("Invoice not found.", 404);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateAsync(
        InvoiceCreateRequest request, string ownerId, string businessState)
    {
        // Verify client belongs to owner
        var client = await _clientRepo.GetByIdAsync(request.ClientId, ownerId);
        if (client == null)
            throw new AppException("Client not found.", 404);

        // Generate unique invoice number
        var invoiceNumber = await _counterService.GetNextInvoiceNumberAsync(ownerId);

        // Calculate tax
        var taxBreakdown = _taxService.Calculate(businessState, client.State, request.LineItems);

        // Build line items
        var lineItems = request.LineItems.Select(li => new LineItem
        {
            Description = li.Description,
            HsnCode = li.HsnCode,
            Quantity = li.Quantity,
            Rate = li.Rate,
            TaxRate = li.TaxRate,
            Amount = Math.Round(li.Quantity * li.Rate, 2)
        }).ToList();

        var invoice = new Invoice
        {
            OwnerId = ownerId,
            ClientId = client.Id,
            ClientName = client.Name,
            ClientState = client.State,
            ClientGstin = client.Gstin,
            InvoiceNumber = invoiceNumber,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            LineItems = lineItems,
            Subtotal = taxBreakdown.Subtotal,
            Cgst = taxBreakdown.Cgst,
            Sgst = taxBreakdown.Sgst,
            Igst = taxBreakdown.Igst,
            TotalTax = taxBreakdown.TotalTax,
            GrandTotal = taxBreakdown.GrandTotal,
            Notes = request.Notes,
            Status = InvoiceStatus.Draft,
            AuditLog = new List<AuditEntry>
            {
                new() { Action = "created", ByUserId = ownerId }
            }
        };

        await _invoiceRepo.CreateAsync(invoice);
        _logger.LogInformation("Invoice {Number} created for client {Client}",
            invoiceNumber, client.Name);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> UpdateAsync(
        string id, InvoiceUpdateRequest request, string ownerId, string businessState)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(id, ownerId);
        if (invoice == null)
            throw new AppException("Invoice not found.", 404);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new AppException("Only draft invoices can be edited.", 400);

        // Recalculate tax
        var taxBreakdown = _taxService.Calculate(
            businessState, invoice.ClientState, request.LineItems);

        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.Notes = request.Notes;
        invoice.LineItems = request.LineItems.Select(li => new LineItem
        {
            Description = li.Description,
            HsnCode = li.HsnCode,
            Quantity = li.Quantity,
            Rate = li.Rate,
            TaxRate = li.TaxRate,
            Amount = Math.Round(li.Quantity * li.Rate, 2)
        }).ToList();
        invoice.Subtotal = taxBreakdown.Subtotal;
        invoice.Cgst = taxBreakdown.Cgst;
        invoice.Sgst = taxBreakdown.Sgst;
        invoice.Igst = taxBreakdown.Igst;
        invoice.TotalTax = taxBreakdown.TotalTax;
        invoice.GrandTotal = taxBreakdown.GrandTotal;

        invoice.AuditLog.Add(new AuditEntry
        {
            Action = "updated",
            ByUserId = ownerId
        });

        await _invoiceRepo.UpdateAsync(invoice);
        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> RecordPaymentAsync(
        string invoiceId, PaymentRequest request, string ownerId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId, ownerId);
        if (invoice == null)
            throw new AppException("Invoice not found.", 404);

        if (invoice.Status == InvoiceStatus.Draft)
            throw new AppException("Cannot record payment on a draft invoice. Send it first.", 400);

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new AppException("Cannot record payment on a cancelled invoice.", 400);

        if (invoice.Status == InvoiceStatus.Paid)
            throw new AppException("Invoice is already fully paid.", 400);

        var payment = new Payment
        {
            Amount = request.Amount,
            Date = request.Date,
            Method = request.Method,
            Notes = request.Notes
        };

        // Add payment
        await _invoiceRepo.AddPaymentAsync(invoiceId, ownerId, payment);

        // Update status based on total paid
        var newTotalPaid = invoice.TotalPaid + request.Amount;
        string newStatus;
        if (newTotalPaid >= invoice.GrandTotal)
            newStatus = InvoiceStatus.Paid;
        else
            newStatus = InvoiceStatus.Partial;

        await _invoiceRepo.UpdateStatusAsync(invoiceId, ownerId, newStatus);

        // Audit
        await _invoiceRepo.AddAuditEntryAsync(invoiceId, ownerId, new AuditEntry
        {
            Action = "payment_recorded",
            ByUserId = ownerId,
            Details = $"Amount: {request.Amount:F2}, Method: {request.Method}"
        });

        _logger.LogInformation("Payment of {Amount} recorded on invoice {Id}",
            request.Amount, invoiceId);

        // Re-fetch to return updated state
        return MapToResponse((await _invoiceRepo.GetByIdAsync(invoiceId, ownerId))!);
    }

    public async Task<InvoiceResponse> UpdateStatusAsync(
        string id, string newStatus, string ownerId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(id, ownerId);
        if (invoice == null)
            throw new AppException("Invoice not found.", 404);

        // Validate status transition
        ValidateStatusTransition(invoice.Status, newStatus);

        await _invoiceRepo.UpdateStatusAsync(id, ownerId, newStatus);
        await _invoiceRepo.AddAuditEntryAsync(id, ownerId, new AuditEntry
        {
            Action = $"status_changed_to_{newStatus}",
            ByUserId = ownerId,
            Details = $"From: {invoice.Status}, To: {newStatus}"
        });

        // Re-fetch
        return MapToResponse((await _invoiceRepo.GetByIdAsync(id, ownerId))!);
    }

    private static void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var allowed = currentStatus switch
        {
            InvoiceStatus.Draft => new[] { InvoiceStatus.Sent, InvoiceStatus.Cancelled },
            InvoiceStatus.Sent => new[] { InvoiceStatus.Paid, InvoiceStatus.Partial, InvoiceStatus.Overdue, InvoiceStatus.Cancelled },
            InvoiceStatus.Partial => new[] { InvoiceStatus.Paid, InvoiceStatus.Overdue, InvoiceStatus.Cancelled },
            InvoiceStatus.Overdue => new[] { InvoiceStatus.Paid, InvoiceStatus.Partial, InvoiceStatus.Cancelled },
            _ => Array.Empty<string>()
        };

        if (!allowed.Contains(newStatus))
            throw new AppException(
                $"Cannot change status from '{currentStatus}' to '{newStatus}'.", 400);
    }

    public TaxBreakdown PreviewTax(string businessState, TaxPreviewRequest request)
    {
        return _taxService.Calculate(businessState, request.ClientState, request.LineItems);
    }

    private static InvoiceResponse MapToResponse(Invoice invoice) => new()
    {
        Id = invoice.Id,
        ClientId = invoice.ClientId,
        ClientName = invoice.ClientName,
        ClientState = invoice.ClientState,
        ClientGstin = invoice.ClientGstin,
        InvoiceNumber = invoice.InvoiceNumber,
        IssueDate = invoice.IssueDate,
        DueDate = invoice.DueDate,
        LineItems = invoice.LineItems.Select(li => new LineItemResponse
        {
            Description = li.Description,
            HsnCode = li.HsnCode,
            Quantity = li.Quantity,
            Rate = li.Rate,
            TaxRate = li.TaxRate,
            Amount = li.Amount
        }).ToList(),
        Subtotal = invoice.Subtotal,
        Cgst = invoice.Cgst,
        Sgst = invoice.Sgst,
        Igst = invoice.Igst,
        TotalTax = invoice.TotalTax,
        GrandTotal = invoice.GrandTotal,
        Currency = invoice.Currency,
        Notes = invoice.Notes,
        Status = invoice.Status,
        TotalPaid = invoice.TotalPaid,
        BalanceDue = invoice.BalanceDue,
        Payments = invoice.Payments.Select(p => new PaymentResponse
        {
            Id = p.Id,
            Amount = p.Amount,
            Date = p.Date,
            Method = p.Method,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt
        }).ToList(),
        CreatedAt = invoice.CreatedAt,
        UpdatedAt = invoice.UpdatedAt
    };
}
