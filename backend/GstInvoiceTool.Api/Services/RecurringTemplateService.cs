namespace GstInvoiceTool.Api.Services;

using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;

public class RecurringTemplateService
{
    private readonly RecurringTemplateRepository _templateRepo;
    private readonly ClientRepository _clientRepo;
    private readonly ILogger<RecurringTemplateService> _logger;

    public RecurringTemplateService(
        RecurringTemplateRepository templateRepo,
        ClientRepository clientRepo,
        ILogger<RecurringTemplateService> logger)
    {
        _templateRepo = templateRepo;
        _clientRepo = clientRepo;
        _logger = logger;
    }

    public async Task<List<RecurringTemplateResponse>> GetAllAsync(string ownerId)
    {
        var templates = await _templateRepo.GetByOwnerAsync(ownerId);
        return templates.Select(MapToResponse).ToList();
    }

    public async Task<RecurringTemplateResponse> CreateAsync(RecurringTemplateRequest request, string ownerId)
    {
        var client = await _clientRepo.GetByIdAsync(request.ClientId, ownerId);
        if (client == null) throw new AppException("Client not found.", 404);

        var template = new RecurringTemplate
        {
            OwnerId = ownerId,
            ClientId = client.Id,
            ClientName = client.Name,
            Frequency = request.Frequency.ToLower(),
            NextRunDate = request.StartDate,
            AutoSendEmail = request.AutoSendEmail,
            Notes = request.Notes,
            LineItems = request.LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                HsnCode = li.HsnCode,
                Quantity = li.Quantity,
                Rate = li.Rate,
                TaxRate = li.TaxRate,
                Amount = Math.Round(li.Quantity * li.Rate, 2)
            }).ToList()
        };

        await _templateRepo.CreateAsync(template);
        _logger.LogInformation("Created recurring invoice template for client {Client}", client.Name);

        return MapToResponse(template);
    }

    public async Task DeleteAsync(string id, string ownerId)
    {
        var deleted = await _templateRepo.DeleteAsync(id, ownerId);
        if (!deleted) throw new AppException("Template not found.", 404);
    }

    private static RecurringTemplateResponse MapToResponse(RecurringTemplate t) => new()
    {
        Id = t.Id,
        ClientId = t.ClientId,
        ClientName = t.ClientName,
        Frequency = t.Frequency,
        NextRunDate = t.NextRunDate,
        LastRunDate = t.LastRunDate,
        Notes = t.Notes,
        AutoSendEmail = t.AutoSendEmail,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        LineItems = t.LineItems.Select(li => new LineItemResponse
        {
            Description = li.Description,
            HsnCode = li.HsnCode,
            Quantity = li.Quantity,
            Rate = li.Rate,
            TaxRate = li.TaxRate,
            Amount = li.Amount
        }).ToList()
    };
}
