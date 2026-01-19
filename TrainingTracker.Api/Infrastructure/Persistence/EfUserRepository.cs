using Microsoft.EntityFrameworkCore;
using TrainingTracker.Api.Domain.Entities;
using TrainingTracker.Api.Domain.Repositories;

namespace TrainingTracker.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUserRepository with full CRUD operations.
/// </summary>
public class EfUserRepository : IUserRepository
{
    private readonly TrainingTrackerDbContext _db;
    
    public EfUserRepository(TrainingTrackerDbContext db) 
    { 
        _db = db; 
    }
    
    public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0) page = 1; 
        if (pageSize <= 0) pageSize = 10;
        var q = _db.Users.AsNoTracking().OrderBy(u => u.Email);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
    
    public Task<User?> GetAsync(Guid id, CancellationToken ct) 
    {
        return _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct)
    {
        // Generate ID if not set
        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
        }

        // Set server-side timestamp
        user.CreatedUtc = DateTime.UtcNow;

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User?> UpdateAsync(User user, CancellationToken ct)
    {
        var existing = await _db.Users.FindAsync(new object[] { user.Id }, ct);
        if (existing == null)
        {
            return null;
        }

        // Update properties
        existing.Email = user.Email;
        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.IsActive = user.IsActive;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user == null)
        {
            return false;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken ct)
    {
        var query = _db.Users.Where(u => u.Email == email);
        
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }
}
