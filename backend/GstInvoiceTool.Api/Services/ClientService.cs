namespace GstInvoiceTool.Api.Services;

using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;

public class ClientService
{
    private readonly ClientRepository _clientRepo;
    private readonly ILogger<ClientService> _logger;

    public ClientService(ClientRepository clientRepo, ILogger<ClientService> logger)
    {
        _clientRepo = clientRepo;
        _logger = logger;
    }

    public async Task<List<ClientResponse>> GetAllAsync(string ownerId)
    {
        var clients = await _clientRepo.GetByOwnerAsync(ownerId);
        return clients.Select(MapToResponse).ToList();
    }

    public async Task<ClientResponse> GetByIdAsync(string id, string ownerId)
    {
        var client = await _clientRepo.GetByIdAsync(id, ownerId);
        if (client == null)
            throw new AppException("Client not found.", 404);

        return MapToResponse(client);
    }

    public async Task<ClientResponse> CreateAsync(ClientRequest request, string ownerId)
    {
        var client = new Client
        {
            OwnerId = ownerId,
            Name = request.Name,
            Email = request.Email,
            Gstin = request.Gstin,
            State = request.State,
            BillingAddress = request.BillingAddress ?? string.Empty,
            Phone = request.Phone
        };

        await _clientRepo.CreateAsync(client);
        _logger.LogInformation("Client created: {Name} for owner {OwnerId}", client.Name, ownerId);

        return MapToResponse(client);
    }

    public async Task<ClientResponse> UpdateAsync(string id, ClientRequest request, string ownerId)
    {
        var client = await _clientRepo.GetByIdAsync(id, ownerId);
        if (client == null)
            throw new AppException("Client not found.", 404);

        client.Name = request.Name;
        client.Email = request.Email;
        client.Gstin = request.Gstin;
        client.State = request.State;
        client.BillingAddress = request.BillingAddress ?? string.Empty;
        client.Phone = request.Phone;
        client.UpdatedAt = DateTime.UtcNow;

        await _clientRepo.UpdateAsync(client);
        _logger.LogInformation("Client updated: {Name} ({Id})", client.Name, id);

        return MapToResponse(client);
    }

    public async Task DeleteAsync(string id, string ownerId)
    {
        var deleted = await _clientRepo.DeleteAsync(id, ownerId);
        if (!deleted)
            throw new AppException("Client not found.", 404);

        _logger.LogInformation("Client deleted: {Id}", id);
    }

    private static ClientResponse MapToResponse(Client client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        Email = client.Email,
        Gstin = client.Gstin,
        State = client.State,
        BillingAddress = client.BillingAddress,
        Phone = client.Phone,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt
    };
}
