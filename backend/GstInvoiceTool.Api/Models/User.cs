namespace GstInvoiceTool.Api.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = null!;

    [BsonElement("role")]
    public string Role { get; set; } = "owner";

    [BsonElement("business")]
    public BusinessInfo Business { get; set; } = new();

    [BsonElement("smtpConfig")]
    public UserSmtpConfig SmtpConfig { get; set; } = new();

    [BsonElement("refreshTokens")]
    public List<RefreshToken> RefreshTokens { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserSmtpConfig
{
    [BsonElement("smtpHost")]
    public string? SmtpHost { get; set; }

    [BsonElement("smtpPort")]
    public int SmtpPort { get; set; } = 587;

    [BsonElement("smtpUser")]
    public string? SmtpUser { get; set; }

    [BsonElement("smtpPass")]
    public string? SmtpPass { get; set; }

    [BsonElement("fromEmail")]
    public string? FromEmail { get; set; }
}

public class BusinessInfo
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("gstin")]
    public string? Gstin { get; set; }

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;
}

public class RefreshToken
{
    [BsonElement("token")]
    public string Token { get; set; } = null!;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
