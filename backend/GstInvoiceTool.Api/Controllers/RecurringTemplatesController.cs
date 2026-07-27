namespace GstInvoiceTool.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Services;

[ApiController]
[Route("api/recurring-templates")]
[Authorize]
public class RecurringTemplatesController : ControllerBase
{
    private readonly RecurringTemplateService _service;

    public RecurringTemplatesController(RecurringTemplateService service)
    {
        _service = service;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AppException("User not authenticated.", 401);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RecurringTemplateResponse>>>> GetAll()
    {
        var result = await _service.GetAllAsync(GetUserId());
        return Ok(ApiResponse<List<RecurringTemplateResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RecurringTemplateResponse>>> Create(
        [FromBody] RecurringTemplateRequest request)
    {
        var result = await _service.CreateAsync(request, GetUserId());
        return StatusCode(201, ApiResponse<RecurringTemplateResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        await _service.DeleteAsync(id, GetUserId());
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("trigger")]
    public async Task<ActionResult<ApiResponse>> TriggerWorker([FromServices] GstInvoiceTool.Api.Jobs.RecurringInvoiceJob job)
    {
        await job.ProcessDueRecurringInvoicesAsync();
        return Ok(ApiResponse.Ok());
    }
}
