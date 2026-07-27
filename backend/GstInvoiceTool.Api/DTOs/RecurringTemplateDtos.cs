namespace GstInvoiceTool.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public class RecurringTemplateRequest
{
    [Required]
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// Frequency: "weekly", "monthly", "quarterly"
    /// </summary>
    [Required]
    public string Frequency { get; set; } = "monthly";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    [MinLength(1)]
    public List<LineItemRequest> LineItems { get; set; } = new();

    public string? Notes { get; set; }

    public bool AutoSendEmail { get; set; } = true;
}

public class RecurringTemplateResponse
{
    public string Id { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public DateTime NextRunDate { get; set; }
    public DateTime? LastRunDate { get; set; }
    public List<LineItemResponse> LineItems { get; set; } = new();
    public string? Notes { get; set; }
    public bool AutoSendEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
