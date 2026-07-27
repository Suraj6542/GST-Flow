namespace GstInvoiceTool.Api.Repositories;

using MongoDB.Driver;
using GstInvoiceTool.Api.Models;

public class UserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");

        // Ensure unique index on email
        var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _users.Indexes.CreateOne(new CreateIndexModel<User>(indexKeys, indexOptions));
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email.ToLowerInvariant()).FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLowerInvariant();
        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
    }

    public async Task AddRefreshTokenAsync(string userId, RefreshToken token)
    {
        var update = Builders<User>.Update.Push(u => u.RefreshTokens, token);
        await _users.UpdateOneAsync(u => u.Id == userId, update);
    }

    public async Task<User?> GetByRefreshTokenAsync(string token)
    {
        var filter = Builders<User>.Filter.ElemMatch(
            u => u.RefreshTokens,
            rt => rt.Token == token);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task RevokeRefreshTokenAsync(string userId, string token)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.Id, userId),
            Builders<User>.Filter.ElemMatch(u => u.RefreshTokens, rt => rt.Token == token));

        var update = Builders<User>.Update.Set("refreshTokens.$.revokedAt", DateTime.UtcNow);
        await _users.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Clean up expired and revoked refresh tokens to prevent unbounded growth.
    /// </summary>
    public async Task CleanupExpiredTokensAsync(string userId)
    {
        var update = Builders<User>.Update.PullFilter(
            u => u.RefreshTokens,
            rt => rt.ExpiresAt < DateTime.UtcNow || rt.RevokedAt != null);
        await _users.UpdateOneAsync(u => u.Id == userId, update);
    }
}
