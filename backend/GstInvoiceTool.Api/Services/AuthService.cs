namespace GstInvoiceTool.Api.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;

public class AuthService
{
    private readonly UserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserRepository userRepo, IConfiguration config, ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
        {
            throw new AppException("An account with this email already exists.", 409);
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "owner",
            Business = new BusinessInfo
            {
                Name = request.BusinessName,
                Gstin = request.Gstin,
                State = request.State,
                Address = request.Address ?? string.Empty
            }
        };

        await _userRepo.CreateAsync(user);
        _logger.LogInformation("New user registered: {Email}", user.Email);

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(refreshToken);
        if (user == null)
        {
            throw new AppException("Invalid refresh token.", 401);
        }

        var storedToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
        if (storedToken == null || !storedToken.IsActive)
        {
            throw new AppException("Refresh token expired or revoked.", 401);
        }

        // Rotate: revoke old, issue new
        await _userRepo.RevokeRefreshTokenAsync(user.Id, refreshToken);

        // Cleanup old tokens
        await _userRepo.CleanupExpiredTokensAsync(user.Id);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(
            double.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "15"));

        var storedRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddRefreshTokenAsync(user.Id, storedRefreshToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToDto(user)
        };
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("businessState", user.Business.State)
        };

        var expiryMinutes = double.Parse(
            _config["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        Business = new BusinessDto
        {
            Name = user.Business.Name,
            Gstin = user.Business.Gstin,
            State = user.Business.State,
            Address = user.Business.Address
        }
    };
}

/// <summary>
/// Custom application exception with HTTP status code.
/// Used by the exception handling middleware to return structured error responses.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
