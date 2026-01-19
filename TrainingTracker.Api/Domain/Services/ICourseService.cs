using TrainingTracker.Api.Contracts.Courses;
using TrainingTracker.Api.Domain.Entities;
using TrainingTracker.Api.Domain.Repositories;

namespace TrainingTracker.Api.Domain.Services;

public interface ICourseService
{
    Task<PagedResponse<CourseSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<CourseDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<CourseDetailDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    Task<CourseDetailDto?> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class CourseService : ICourseService
{
    private readonly ICourseRepository _repo;

    public CourseService(ICourseRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<CourseSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListAsync(page, pageSize, ct);
        return new PagedResponse<CourseSummaryDto>
        {
            Items = items.Select(MapSummary).ToList(),
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 10 : pageSize,
            TotalCount = total
        };
    }

    public async Task<CourseDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var course = await _repo.GetAsync(id, ct);
        return course is null ? null : MapDetail(course);
    }

    public async Task<CourseDetailDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        // Validate title uniqueness
        if (await _repo.ExistsByTitleAsync(request.Title, null, ct))
        {
            throw new InvalidOperationException($"A course with the title '{request.Title}' already exists.");
        }

        var course = new Course
        {
            Title = request.Title,
            IsRequired = request.IsRequired,
            IsActive = request.IsActive,
            ValidityMonths = request.ValidityMonths,
            Category = request.Category,
            Description = request.Description
        };

        var created = await _repo.CreateAsync(course, ct);
        return MapDetail(created);
    }

    public async Task<CourseDetailDto?> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken ct)
    {
        // Check if course exists
        var existing = await _repo.GetAsync(id, ct);
        if (existing == null)
        {
            return null;
        }

        // Validate title uniqueness (excluding current course)
        if (await _repo.ExistsByTitleAsync(request.Title, id, ct))
        {
            throw new InvalidOperationException($"A course with the title '{request.Title}' already exists.");
        }

        existing.Title = request.Title;
        existing.IsRequired = request.IsRequired;
        existing.IsActive = request.IsActive;
        existing.ValidityMonths = request.ValidityMonths;
        existing.Category = request.Category;
        existing.Description = request.Description;

        var updated = await _repo.UpdateAsync(existing, ct);
        return updated != null ? MapDetail(updated) : null;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        return await _repo.DeleteAsync(id, ct);
    }

    private static CourseSummaryDto MapSummary(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        IsRequired = c.IsRequired,
        IsActive = c.IsActive,
        ValidityMonths = c.ValidityMonths,
        Category = c.Category
    };

    private static CourseDetailDto MapDetail(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        IsRequired = c.IsRequired,
        IsActive = c.IsActive,
        ValidityMonths = c.ValidityMonths,
        Category = c.Category,
        Description = c.Description,
        CreatedUtc = c.CreatedUtc
    };
}