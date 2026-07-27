namespace GstInvoiceTool.Api.Services;

using MongoDB.Driver;
using GstInvoiceTool.Api.Models;

/// <summary>
/// Generates unique, sequential invoice numbers using MongoDB's atomic findOneAndUpdate.
/// Format: INV-{YYYY}-{NNNN} (e.g. INV-2026-0001)
/// Scoped per owner per year to avoid collisions.
/// </summary>
public class CounterService
{
    private readonly IMongoCollection<Counter> _counters;

    public CounterService(IMongoDatabase database)
    {
        _counters = database.GetCollection<Counter>("counters");
    }

    public async Task<string> GetNextInvoiceNumberAsync(string ownerId)
    {
        var year = DateTime.UtcNow.Year;
        var counterId = $"invoiceNumber:{ownerId}:{year}";

        var filter = Builders<Counter>.Filter.Eq(c => c.Id, counterId);
        var update = Builders<Counter>.Update.Inc(c => c.Seq, 1);
        var options = new FindOneAndUpdateOptions<Counter>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var counter = await _counters.FindOneAndUpdateAsync(filter, update, options);
        return $"INV-{year}-{counter.Seq:D4}";
    }
}
