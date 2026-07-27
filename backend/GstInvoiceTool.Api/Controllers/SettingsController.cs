namespace GstInvoiceTool.Api.Controllers;

using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;

[Authorize]
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly UserRepository _userRepo;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(UserRepository userRepo, ILogger<SettingsController> logger)
    {
        _userRepo = userRepo;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// GET /api/settings/smtp — Returns the current user's SMTP configuration (password masked).
    /// </summary>
    [HttpGet("smtp")]
    public async Task<ActionResult<ApiResponse<SmtpConfigResponse>>> GetSmtpConfig()
    {
        var user = await _userRepo.GetByIdAsync(GetUserId());
        if (user == null) return NotFound(ApiResponse<SmtpConfigResponse>.Fail("User not found"));

        var cfg = user.SmtpConfig;
        var response = new SmtpConfigResponse
        {
            SmtpHost = cfg.SmtpHost,
            SmtpPort = cfg.SmtpPort,
            SmtpUser = cfg.SmtpUser,
            FromEmail = cfg.FromEmail,
            IsConfigured = !string.IsNullOrWhiteSpace(cfg.SmtpHost)
                           && !string.IsNullOrWhiteSpace(cfg.SmtpUser)
                           && !string.IsNullOrWhiteSpace(cfg.SmtpPass)
        };

        return Ok(ApiResponse<SmtpConfigResponse>.Ok(response));
    }

    /// <summary>
    /// PUT /api/settings/smtp — Save SMTP configuration for the logged-in user.
    /// </summary>
    [HttpPut("smtp")]
    public async Task<ActionResult<ApiResponse<SmtpConfigResponse>>> SaveSmtpConfig(
        [FromBody] SmtpConfigRequest request)
    {
        var user = await _userRepo.GetByIdAsync(GetUserId());
        if (user == null) return NotFound(ApiResponse<SmtpConfigResponse>.Fail("User not found"));

        // If password is blank, keep existing (only for updates with existing config)
        var existingPass = user.SmtpConfig?.SmtpPass;
        var newPass = !string.IsNullOrWhiteSpace(request.SmtpPass)
            ? request.SmtpPass
            : existingPass;

        if (string.IsNullOrWhiteSpace(newPass))
        {
            return BadRequest(ApiResponse<SmtpConfigResponse>.Fail("SMTP Password is required for initial setup."));
        }

        user.SmtpConfig = new UserSmtpConfig
        {
            SmtpHost = request.SmtpHost.Trim(),
            SmtpPort = request.SmtpPort,
            SmtpUser = request.SmtpUser.Trim(),
            SmtpPass = newPass,
            FromEmail = string.IsNullOrWhiteSpace(request.FromEmail)
                ? request.SmtpUser.Trim()
                : request.FromEmail.Trim()
        };

        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("SMTP config updated for user {UserId}", user.Id);

        return Ok(ApiResponse<SmtpConfigResponse>.Ok(new SmtpConfigResponse
        {
            SmtpHost = user.SmtpConfig.SmtpHost,
            SmtpPort = user.SmtpConfig.SmtpPort,
            SmtpUser = user.SmtpConfig.SmtpUser,
            FromEmail = user.SmtpConfig.FromEmail,
            IsConfigured = true
        }));
    }

    /// <summary>
    /// DELETE /api/settings/smtp — Remove SMTP configuration (revert to default fallback).
    /// </summary>
    [HttpDelete("smtp")]
    public async Task<ActionResult<ApiResponse>> RemoveSmtpConfig()
    {
        var user = await _userRepo.GetByIdAsync(GetUserId());
        if (user == null) return NotFound(ApiResponse.Fail("User not found"));

        user.SmtpConfig = new UserSmtpConfig();
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("SMTP config removed for user {UserId}", user.Id);

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// POST /api/settings/smtp/test — Send a test email to verify SMTP configuration.
    /// </summary>
    [HttpPost("smtp/test")]
    public async Task<ActionResult<ApiResponse>> SendTestEmail([FromBody] TestEmailRequest request)
    {
        var user = await _userRepo.GetByIdAsync(GetUserId());
        if (user == null) return NotFound(ApiResponse.Fail("User not found"));

        var cfg = user.SmtpConfig;
        if (string.IsNullOrWhiteSpace(cfg.SmtpHost) || string.IsNullOrWhiteSpace(cfg.SmtpUser) || string.IsNullOrWhiteSpace(cfg.SmtpPass))
        {
            return BadRequest(ApiResponse.Fail("SMTP is not configured. Please save your SMTP settings first."));
        }

        try
        {
            using var client = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort)
            {
                Credentials = new NetworkCredential(cfg.SmtpUser, cfg.SmtpPass),
                EnableSsl = true
            };

            var fromEmail = !string.IsNullOrWhiteSpace(cfg.FromEmail) ? cfg.FromEmail : cfg.SmtpUser;
            var displayName = !string.IsNullOrWhiteSpace(user.Business?.Name) ? user.Business.Name : user.Name;

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, displayName),
                Subject = "✅ GSTFlow Test Email — SMTP Configuration Verified",
                Body = $"Hello!\n\nThis is a test email from GSTFlow to verify that your SMTP settings are working correctly.\n\n" +
                       $"SMTP Host: {cfg.SmtpHost}\n" +
                       $"SMTP Port: {cfg.SmtpPort}\n" +
                       $"From: {fromEmail} ({displayName})\n\n" +
                       $"If you received this email, your email configuration is working perfectly! 🎉\n\n" +
                       $"— GSTFlow Invoicing",
                IsBodyHtml = false
            };
            message.To.Add(request.ToEmail);

            await client.SendMailAsync(message);

            _logger.LogInformation("Test email sent successfully from {From} to {To}", fromEmail, request.ToEmail);
            return Ok(ApiResponse.Ok());
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP test email failed for user {UserId}", user.Id);
            return BadRequest(ApiResponse.Fail($"SMTP Error: {ex.Message}. Check your host, port, and credentials."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test email failed unexpectedly for user {UserId}", user.Id);
            return BadRequest(ApiResponse.Fail($"Failed to send test email: {ex.Message}"));
        }
    }
}
