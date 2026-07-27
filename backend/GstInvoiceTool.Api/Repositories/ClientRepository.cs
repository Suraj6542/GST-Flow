namespace GstInvoiceTool.Api.Repositories;

using MongoDB.Driver;
using GstInvoiceTool.Api.Models;

public class ClientRepository
{
    private readonly IMongoCollection<Client> _clients;

    public ClientRepository(IMongoDatabase database)
    {
        _clients = database.GetCollection<Client>("clients");

        // Compound index: ownerId + name for fast lookups
        var indexKeys = Builders<Client>.IndexKeys
            .Ascending(c => c.OwnerId)
            .Ascending(c => c.Name);
        _clients.Indexes.CreateOne(new CreateIndexModel<Client>(indexKeys));
    }

    public async Task<List<Client>> GetByOwnerAsync(string ownerId)
    {
        return await _clients
            .Find(c => c.OwnerId == ownerId)
            .SortBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(string id, string ownerId)
    {
        return await _clients
            .Find(c => c.Id == id && c.OwnerId == ownerId)
            .FirstOrDefaultAsync();
    }

    public async Task<Client> CreateAsync(Client client)
    {
        await _clients.InsertOneAsync(client);
        return client;
    }

    public async Task<bool> UpdateAsync(Client client)
    {
        var result = await _clients.ReplaceOneAsync(
            c => c.Id == client.Id && c.OwnerId == client.OwnerId,
            client);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string ownerId)
    {
        var result = await _clients.DeleteOneAsync(
            c => c.Id == id && c.OwnerId == ownerId);
        return result.DeletedCount > 0;
    }

    public async Task<long> CountByOwnerAsync(string ownerId)
    {
        return await _clients.CountDocumentsAsync(c => c.OwnerId == ownerId);
    }
}
