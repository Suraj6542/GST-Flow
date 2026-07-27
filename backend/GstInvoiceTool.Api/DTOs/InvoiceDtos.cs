namespace GstInvoiceTool.Api.DTOs;

using System.ComponentModel.DataAnnotations;

// ─── Invoice Request/Response ─────────────────────────────────

public class InvoiceCreateRequest
{
    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one line item is required")]
    public List<LineItemRequest> LineItems { get; set; } = new();

    public string? Notes { get; set; }
}

public class InvoiceUpdateRequest
{
    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one line item is required")]
    public List<LineItemRequest> LineItems { get; set; } = new();

    public string? Notes { get; set; }
}

public class LineItemRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Description { get; set; } = null!;

    public string? HsnCode { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Rate must be greater than 0")]
    public decimal Rate { get; set; }

    /// <summary>
    /// GST rate: 0, 5, 12, 18, or 28
    /// </summary>
    [Range(0, 28)]
    public decimal TaxRate { get; set; }
}

public class InvoiceResponse
{
    public string Id { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public string ClientState { get; set; } = null!;
    public string? ClientGstin { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public List<LineItemResponse> LineItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = null!;
    public string? Notes { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public List<PaymentResponse> Payments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LineItemResponse
{
    public string Description { get; set; } = null!;
    public string? HsnCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentResponse
{
    public string Id { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Method { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Tax Preview ──────────────────────────────────────────────

public class TaxPreviewRequest
{
    [Required]
    public string ClientState { get; set; } = null!;

    [Required]
    [MinLength(1)]
    public List<LineItemRequest> LineItems { get; set; } = new();
}

public class TaxBreakdown
{
    public decimal Subtotal { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// "intra" if same state (CGST+SGST), "inter" if different state (IGST)
    /// </summary>
    public string TaxType { get; set; } = null!;

    public List<LineItemTaxDetail> LineItemDetails { get; set; } = new();
}

public class LineItemTaxDetail
{
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal TotalTax { get; set; }
    public decimal Total { get; set; }
}

// ─── Payment Recording ───────────────────────────────────────

public class PaymentRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public string Method { get; set; } = "bank_transfer";

    public string? Notes { get; set; }
}

// ─── Invoice List Filters ────────────────────────────────────

public class InvoiceFilterQuery
{
    public string? Status { get; set; }
    public string? ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
