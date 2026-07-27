namespace GstInvoiceTool.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;
using GstInvoiceTool.Api.Services;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _invoiceService;
    private readonly PdfService _pdfService;
    private readonly UserRepository _userRepo;
    private readonly ClientRepository _clientRepo;
    private readonly InvoiceRepository _invoiceRepo;
    private readonly IEmailService _emailService;

    public InvoicesController(
        InvoiceService invoiceService,
        PdfService pdfService,
        UserRepository userRepo,
        ClientRepository clientRepo,
        InvoiceRepository invoiceRepo,
        IEmailService emailService)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _userRepo = userRepo;
        _clientRepo = clientRepo;
        _invoiceRepo = invoiceRepo;
        _emailService = emailService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AppException("User not authenticated.", 401);

    private string GetBusinessState() =>
        User.FindFirstValue("businessState")
        ?? throw new AppException("Business state not found in token.", 400);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InvoiceResponse>>>> GetAll(
        [FromQuery] InvoiceFilterQuery filter)
    {
        var invoices = await _invoiceService.GetAllAsync(GetUserId(), filter);
        return Ok(ApiResponse<List<InvoiceResponse>>.Ok(invoices));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetById(string id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, GetUserId());
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Create(
        [FromBody] InvoiceCreateRequest request)
    {
        var invoice = await _invoiceService.CreateAsync(
            request, GetUserId(), GetBusinessState());
        return StatusCode(201, ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Update(
        string id, [FromBody] InvoiceUpdateRequest request)
    {
        var invoice = await _invoiceService.UpdateAsync(
            id, request, GetUserId(), GetBusinessState());
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    [HttpPost("{id}/payments")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> RecordPayment(
        string id, [FromBody] PaymentRequest request)
    {
        var invoice = await _invoiceService.RecordPaymentAsync(id, request, GetUserId());
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateStatus(
        string id, [FromBody] StatusUpdateRequest request)
    {
        var invoice = await _invoiceService.UpdateStatusAsync(id, request.Status, GetUserId());
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    /// <summary>
    /// Download PDF invoice.
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(string id)
    {
        var ownerId = GetUserId();
        var invoice = await _invoiceRepo.GetByIdAsync(id, ownerId);
        if (invoice == null) return NotFound(ApiResponse.Fail("Invoice not found."));

        var owner = await _userRepo.GetByIdAsync(ownerId);
        if (owner == null) return NotFound(ApiResponse.Fail("User not found."));

        var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, owner);
        return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }

    /// <summary>
    /// Send invoice PDF to client via email.
    /// </summary>
    [HttpPost("{id}/send-email")]
    public async Task<ActionResult<ApiResponse>> SendEmail(string id)
    {
        var ownerId = GetUserId();
        var invoice = await _invoiceRepo.GetByIdAsync(id, ownerId);
        if (invoice == null) return NotFound(ApiResponse.Fail("Invoice not found."));

        var owner = await _userRepo.GetByIdAsync(ownerId);
        if (owner == null) return NotFound(ApiResponse.Fail("User not found."));

        var client = await _clientRepo.GetByIdAsync(invoice.ClientId, ownerId);
        if (client == null || string.IsNullOrWhiteSpace(client.Email))
        {
            return BadRequest(ApiResponse.Fail("Client email address is missing or invalid."));
        }

        var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, owner);
        await _emailService.SendInvoiceEmailAsync(
            client.Email,
            invoice.ClientName,
            invoice.InvoiceNumber,
            invoice.GrandTotal,
            pdfBytes,
            owner);

        // Auto transition status to sent if draft
        if (invoice.Status == InvoiceStatus.Draft)
        {
            await _invoiceService.UpdateStatusAsync(id, InvoiceStatus.Sent, ownerId);
        }

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Preview tax calculation without saving. Used for live tax calc in the UI.
    /// </summary>
    [HttpPost("tax-preview")]
    public ActionResult<ApiResponse<TaxBreakdown>> TaxPreview(
        [FromBody] TaxPreviewRequest request)
    {
        var breakdown = _invoiceService.PreviewTax(GetBusinessState(), request);
        return Ok(ApiResponse<TaxBreakdown>.Ok(breakdown));
    }
}

public class StatusUpdateRequest
{
    public string Status { get; set; } = null!;
}
