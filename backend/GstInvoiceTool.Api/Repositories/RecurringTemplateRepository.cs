namespace GstInvoiceTool.Api.Repositories;

using MongoDB.Driver;
using GstInvoiceTool.Api.Models;

public class RecurringTemplateRepository
{
    private readonly IMongoCollection<RecurringTemplate> _templates;

    public RecurringTemplateRepository(IMongoDatabase database)
    {
        _templates = database.GetCollection<RecurringTemplate>("recurringTemplates");
    }

    public async Task<List<RecurringTemplate>> GetByOwnerAsync(string ownerId)
    {
        return await _templates
            .Find(t => t.OwnerId == ownerId)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<RecurringTemplate?> GetByIdAsync(string id, string ownerId)
    {
        return await _templates
            .Find(t => t.Id == id && t.OwnerId == ownerId)
            .FirstOrDefaultAsync();
    }

    public async Task<RecurringTemplate> CreateAsync(RecurringTemplate template)
    {
        await _templates.InsertOneAsync(template);
        return template;
    }

    public async Task<bool> UpdateAsync(RecurringTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        var result = await _templates.ReplaceOneAsync(
            t => t.Id == template.Id && t.OwnerId == template.OwnerId,
            template);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string ownerId)
    {
        var result = await _templates.DeleteOneAsync(
            t => t.Id == id && t.OwnerId == ownerId);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// Find active templates due for execution (NextRunDate <= today).
    /// Used by RecurringInvoiceJob.
    /// </summary>
    public async Task<List<RecurringTemplate>> GetDueTemplatesAsync(DateTime date)
    {
        var filter = Builders<RecurringTemplate>.Filter.And(
            Builders<RecurringTemplate>.Filter.Eq(t => t.IsActive, true),
            Builders<RecurringTemplate>.Filter.Lte(t => t.NextRunDate, date));

        return await _templates.Find(filter).ToListAsync();
    }
}
