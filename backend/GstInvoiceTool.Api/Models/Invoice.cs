namespace GstInvoiceTool.Api.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Invoice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("ownerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; } = null!;

    [BsonElement("clientId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ClientId { get; set; } = null!;

    [BsonElement("clientName")]
    public string ClientName { get; set; } = null!;

    [BsonElement("clientState")]
    public string ClientState { get; set; } = null!;

    [BsonElement("clientGstin")]
    public string? ClientGstin { get; set; }

    [BsonElement("invoiceNumber")]
    public string InvoiceNumber { get; set; } = null!;

    [BsonElement("issueDate")]
    public DateTime IssueDate { get; set; }

    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    [BsonElement("lineItems")]
    public List<LineItem> LineItems { get; set; } = new();

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("cgst")]
    public decimal Cgst { get; set; }

    [BsonElement("sgst")]
    public decimal Sgst { get; set; }

    [BsonElement("igst")]
    public decimal Igst { get; set; }

    [BsonElement("totalTax")]
    public decimal TotalTax { get; set; }

    [BsonElement("grandTotal")]
    public decimal GrandTotal { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "INR";

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = InvoiceStatus.Draft;

    [BsonElement("payments")]
    public List<Payment> Payments { get; set; } = new();

    [BsonElement("auditLog")]
    public List<AuditEntry> AuditLog { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total amount paid across all payments.
    /// </summary>
    [BsonIgnore]
    public decimal TotalPaid => Payments.Sum(p => p.Amount);

    [BsonIgnore]
    public decimal BalanceDue => GrandTotal - TotalPaid;
}

public class LineItem
{
    [BsonElement("description")]
    public string Description { get; set; } = null!;

    [BsonElement("hsnCode")]
    public string? HsnCode { get; set; }

    [BsonElement("quantity")]
    public decimal Quantity { get; set; }

    [BsonElement("rate")]
    public decimal Rate { get; set; }

    /// <summary>
    /// GST tax rate percentage (e.g. 5, 12, 18, 28).
    /// </summary>
    [BsonElement("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Pre-tax amount: quantity × rate.
    /// </summary>
    [BsonElement("amount")]
    public decimal Amount { get; set; }
}

public class Payment
{
    [BsonElement("id")]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("method")]
    public string Method { get; set; } = "bank_transfer";

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditEntry
{
    [BsonElement("action")]
    public string Action { get; set; } = null!;

    [BsonElement("byUserId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ByUserId { get; set; } = null!;

    [BsonElement("at")]
    public DateTime At { get; set; } = DateTime.UtcNow;

    [BsonElement("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Invoice status constants.
/// </summary>
public static class InvoiceStatus
{
    public const string Draft = "draft";
    public const string Sent = "sent";
    public const string Partial = "partial";
    public const string Paid = "paid";
    public const string Overdue = "overdue";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = { Draft, Sent, Partial, Paid, Overdue, Cancelled };
}

public class Counter
{
    [BsonId]
    public string Id { get; set; } = null!;

    [BsonElement("seq")]
    public int Seq { get; set; }
}
