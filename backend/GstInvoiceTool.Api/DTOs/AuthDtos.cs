namespace GstInvoiceTool.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Business name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string BusinessName { get; set; } = null!;

    [StringLength(15)]
    [RegularExpression(@"^(?:[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1})?$",
        ErrorMessage = "Invalid GSTIN format")]
    public string? Gstin { get; set; }

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = null!;

    public string? Address { get; set; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public BusinessDto Business { get; set; } = null!;
}

public class BusinessDto
{
    public string Name { get; set; } = null!;
    public string? Gstin { get; set; }
    public string State { get; set; } = null!;
    public string Address { get; set; } = null!;
}
