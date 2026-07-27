namespace GstInvoiceTool.Api.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request payload for updating the user's SMTP configuration.
/// </summary>
public class SmtpConfigRequest
{
    [Required(ErrorMessage = "SMTP Host is required")]
    public string SmtpHost { get; set; } = null!;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int SmtpPort { get; set; } = 587;

    [Required(ErrorMessage = "SMTP Username / email is required")]
    public string SmtpUser { get; set; } = null!;

    [Required(ErrorMessage = "SMTP Password or App Password is required")]
    public string SmtpPass { get; set; } = null!;

    /// <summary>
    /// Optional "From" email address. Defaults to SmtpUser if not specified.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid From email address")]
    public string? FromEmail { get; set; }
}

/// <summary>
/// Response DTO — never returns the raw password.
/// </summary>
public class SmtpConfigResponse
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string? SmtpUser { get; set; }
    public string? FromEmail { get; set; }
    public bool IsConfigured { get; set; }
}

/// <summary>
/// Request payload for sending a test email.
/// </summary>
public class TestEmailRequest
{
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Invalid recipient email address")]
    public string ToEmail { get; set; } = null!;
}
