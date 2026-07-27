namespace GstInvoiceTool.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Services;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ClientService _clientService;

    public ClientsController(ClientService clientService)
    {
        _clientService = clientService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AppException("User not authenticated.", 401);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClientResponse>>>> GetAll()
    {
        var clients = await _clientService.GetAllAsync(GetUserId());
        return Ok(ApiResponse<List<ClientResponse>>.Ok(clients));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> GetById(string id)
    {
        var client = await _clientService.GetByIdAsync(id, GetUserId());
        return Ok(ApiResponse<ClientResponse>.Ok(client));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> Create(
        [FromBody] ClientRequest request)
    {
        var client = await _clientService.CreateAsync(request, GetUserId());
        return StatusCode(201, ApiResponse<ClientResponse>.Ok(client));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> Update(
        string id, [FromBody] ClientRequest request)
    {
        var client = await _clientService.UpdateAsync(id, request, GetUserId());
        return Ok(ApiResponse<ClientResponse>.Ok(client));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        await _clientService.DeleteAsync(id, GetUserId());
        return Ok(ApiResponse.Ok());
    }
}
