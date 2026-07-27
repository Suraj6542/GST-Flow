namespace GstInvoiceTool.Api.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class RecurringTemplate
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

    /// <summary>
    /// Frequency: "weekly", "monthly", "quarterly"
    /// </summary>
    [BsonElement("frequency")]
    public string Frequency { get; set; } = "monthly";

    [BsonElement("nextRunDate")]
    public DateTime NextRunDate { get; set; }

    [BsonElement("lastRunDate")]
    public DateTime? LastRunDate { get; set; }

    [BsonElement("lineItems")]
    public List<LineItem> LineItems { get; set; } = new();

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("autoSendEmail")]
    public bool AutoSendEmail { get; set; } = true;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
