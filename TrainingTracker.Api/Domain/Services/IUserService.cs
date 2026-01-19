using TrainingTracker.Api.Contracts.Courses; // reuse PagedResponse
using TrainingTracker.Api.Contracts.Users;
using TrainingTracker.Api.Domain.Repositories;
using TrainingTracker.Api.Domain.Entities;

namespace TrainingTracker.Api.Domain.Services;

public interface IUserService
{
    Task<PagedResponse<UserSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserDetailDto?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    
    public UserService(IUserRepository repo) 
    { 
        _repo = repo; 
    }
    
    public async Task<PagedResponse<UserSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListAsync(page, pageSize, ct);
        return new PagedResponse<UserSummaryDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
    
    public async Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var u = await _repo.GetAsync(id, ct);
        return u is null ? null : Map(u);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        // Validate email uniqueness
        if (await _repo.ExistsByEmailAsync(request.Email, null, ct))
        {
            throw new InvalidOperationException($"A user with the email '{request.Email}' already exists.");
        }

        // Parse full name into first and last name
        var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : request.FullName;
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var user = new User
        {
            Email = request.Email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = request.IsActive
        };

        var created = await _repo.CreateAsync(user, ct);
        return Map(created);
    }

    public async Task<UserDetailDto?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        // Check if user exists
        var existing = await _repo.GetAsync(id, ct);
        if (existing == null)
        {
            return null;
        }

        // Validate email uniqueness (excluding current user)
        if (await _repo.ExistsByEmailAsync(request.Email, id, ct))
        {
            throw new InvalidOperationException($"A user with the email '{request.Email}' already exists.");
        }

        // Parse full name into first and last name
        var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        existing.FirstName = nameParts.Length > 0 ? nameParts[0] : request.FullName;
        existing.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        existing.Email = request.Email;
        existing.IsActive = request.IsActive;

        var updated = await _repo.UpdateAsync(existing, ct);
        return updated != null ? Map(updated) : null;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        return await _repo.DeleteAsync(id, ct);
    }
    
    private static UserDetailDto Map(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = $"{u.FirstName} {u.LastName}".Trim(),
        IsActive = u.IsActive,
        CreatedUtc = u.CreatedUtc
    };
}
