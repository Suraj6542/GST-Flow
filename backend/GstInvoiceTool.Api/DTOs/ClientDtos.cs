namespace GstInvoiceTool.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public class ClientRequest
{
    [Required(ErrorMessage = "Client name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = null!;

    [StringLength(15)]
    [RegularExpression(@"^(?:[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1})?$",
        ErrorMessage = "Invalid GSTIN format. Expected 15-character GSTIN (e.g. 29ABCDE1234F1Z5)")]
    public string? Gstin { get; set; }

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = null!;

    public string? BillingAddress { get; set; }

    public string? Phone { get; set; }
}

public class ClientResponse
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Gstin { get; set; }
    public string State { get; set; } = null!;
    public string BillingAddress { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
