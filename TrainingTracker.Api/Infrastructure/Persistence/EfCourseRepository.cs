using Microsoft.EntityFrameworkCore;
using TrainingTracker.Api.Domain.Entities;
using TrainingTracker.Api.Domain.Repositories;

namespace TrainingTracker.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of ICourseRepository with full CRUD operations.
/// </summary>
public class EfCourseRepository : ICourseRepository
{
    private readonly TrainingTrackerDbContext _db;

    public EfCourseRepository(TrainingTrackerDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Course> Items, int TotalCount)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        var query = _db.Courses.AsNoTracking().OrderBy(c => c.Title);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Course?> GetAsync(Guid id, CancellationToken ct)
    {
        return _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Course> CreateAsync(Course course, CancellationToken ct)
    {
        // EF Core will generate the ID if it's not set
        if (course.Id == Guid.Empty)
        {
            course.Id = Guid.NewGuid();
        }

        // Set server-side timestamp
        course.CreatedUtc = DateTime.UtcNow;

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);
        return course;
    }

    public async Task<Course?> UpdateAsync(Course course, CancellationToken ct)
    {
        var existing = await _db.Courses.FindAsync(new object[] { course.Id }, ct);
        if (existing == null)
        {
            return null;
        }

        // Update properties
        existing.Title = course.Title;
        existing.IsRequired = course.IsRequired;
        existing.IsActive = course.IsActive;
        existing.ValidityMonths = course.ValidityMonths;
        existing.Category = course.Category;
        existing.Description = course.Description;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var course = await _db.Courses.FindAsync(new object[] { id }, ct);
        if (course == null)
        {
            return false;
        }

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsByTitleAsync(string title, Guid? excludeId, CancellationToken ct)
    {
        var query = _db.Courses.Where(c => c.Title == title);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }
}