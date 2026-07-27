namespace GstInvoiceTool.Api.Repositories;

using MongoDB.Driver;
using GstInvoiceTool.Api.Models;

public class InvoiceRepository
{
    private readonly IMongoCollection<Invoice> _invoices;

    public InvoiceRepository(IMongoDatabase database)
    {
        _invoices = database.GetCollection<Invoice>("invoices");

        // Indexes for common query patterns
        var indexes = new List<CreateIndexModel<Invoice>>
        {
            new(Builders<Invoice>.IndexKeys.Ascending(i => i.OwnerId).Descending(i => i.CreatedAt)),
            new(Builders<Invoice>.IndexKeys.Ascending(i => i.OwnerId).Ascending(i => i.Status)),
            new(Builders<Invoice>.IndexKeys.Ascending(i => i.OwnerId).Ascending(i => i.ClientId)),
            new(Builders<Invoice>.IndexKeys.Ascending(i => i.InvoiceNumber),
                new CreateIndexOptions { Unique = true })
        };
        _invoices.Indexes.CreateMany(indexes);
    }

    public async Task<List<Invoice>> GetByOwnerAsync(
        string ownerId, string? status = null, string? clientId = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var filterBuilder = Builders<Invoice>.Filter;
        var filter = filterBuilder.Eq(i => i.OwnerId, ownerId);

        if (!string.IsNullOrEmpty(status))
            filter &= filterBuilder.Eq(i => i.Status, status);

        if (!string.IsNullOrEmpty(clientId))
            filter &= filterBuilder.Eq(i => i.ClientId, clientId);

        if (fromDate.HasValue)
            filter &= filterBuilder.Gte(i => i.IssueDate, fromDate.Value);

        if (toDate.HasValue)
            filter &= filterBuilder.Lte(i => i.IssueDate, toDate.Value);

        return await _invoices
            .Find(filter)
            .SortByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(string id, string ownerId)
    {
        return await _invoices
            .Find(i => i.Id == id && i.OwnerId == ownerId)
            .FirstOrDefaultAsync();
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        await _invoices.InsertOneAsync(invoice);
        return invoice;
    }

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        var result = await _invoices.ReplaceOneAsync(
            i => i.Id == invoice.Id && i.OwnerId == invoice.OwnerId,
            invoice);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AddPaymentAsync(string id, string ownerId, Payment payment)
    {
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.Id, id),
            Builders<Invoice>.Filter.Eq(i => i.OwnerId, ownerId));

        var update = Builders<Invoice>.Update
            .Push(i => i.Payments, payment)
            .Set(i => i.UpdatedAt, DateTime.UtcNow);

        var result = await _invoices.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateStatusAsync(string id, string ownerId, string status)
    {
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.Id, id),
            Builders<Invoice>.Filter.Eq(i => i.OwnerId, ownerId));

        var update = Builders<Invoice>.Update
            .Set(i => i.Status, status)
            .Set(i => i.UpdatedAt, DateTime.UtcNow);

        var result = await _invoices.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AddAuditEntryAsync(string id, string ownerId, AuditEntry entry)
    {
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.Id, id),
            Builders<Invoice>.Filter.Eq(i => i.OwnerId, ownerId));

        var update = Builders<Invoice>.Update.Push(i => i.AuditLog, entry);
        var result = await _invoices.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Find invoices that are past due date but not yet marked overdue.
    /// Used by the background job to auto-update status.
    /// </summary>
    public async Task<List<Invoice>> GetOverdueInvoicesAsync()
    {
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.In(i => i.Status, new[] { InvoiceStatus.Sent, InvoiceStatus.Partial }),
            Builders<Invoice>.Filter.Lt(i => i.DueDate, DateTime.UtcNow));

        return await _invoices.Find(filter).ToListAsync();
    }

    /// <summary>
    /// Get invoices for dashboard aggregation (by owner).
    /// </summary>
    public IMongoCollection<Invoice> GetCollection() => _invoices;
}
