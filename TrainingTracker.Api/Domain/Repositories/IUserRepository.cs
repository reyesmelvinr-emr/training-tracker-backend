using TrainingTracker.Api.Domain.Entities;

namespace TrainingTracker.Api.Domain.Repositories;

public interface IUserRepository
{
    Task<(IReadOnlyList<User> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<User?> GetAsync(Guid id, CancellationToken ct);
    Task<User> CreateAsync(User user, CancellationToken ct);
    Task<User?> UpdateAsync(User user, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken ct);
}

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private readonly object _lock = new();
    
    public InMemoryUserRepository()
    {
        _users.AddRange(new[]
        {
            new User { Email = "alice@example.com", FirstName = "Alice", LastName = "Example" },
            new User { Email = "bob@example.com", FirstName = "Bob", LastName = "Example" }
        });
    }
    
    public Task<(IReadOnlyList<User> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0) page = 1; 
        if (pageSize <= 0) pageSize = 10;
        
        lock (_lock)
        {
            var q = _users.OrderBy(u => u.Email);
            var total = q.Count();
            var slice = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(((IReadOnlyList<User>)slice, total));
        }
    }
    
    public Task<User?> GetAsync(Guid id, CancellationToken ct)
    {
        lock (_lock)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }
    }

    public Task<User> CreateAsync(User user, CancellationToken ct)
    {
        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
        }
        user.CreatedUtc = DateTime.UtcNow;
        
        lock (_lock)
        {
            _users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task<User?> UpdateAsync(User user, CancellationToken ct)
    {
        lock (_lock)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null)
            {
                return Task.FromResult<User?>(null);
            }

            existing.Email = user.Email;
            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.IsActive = user.IsActive;
            
            return Task.FromResult<User?>(existing);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Task.FromResult(false);
            }
            _users.Remove(user);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken ct)
    {
        lock (_lock)
        {
            var exists = _users.Any(u => u.Email == email && (!excludeId.HasValue || u.Id != excludeId.Value));
            return Task.FromResult(exists);
        }
    }
}
