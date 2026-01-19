using TrainingTracker.Api.Domain.Entities;

namespace TrainingTracker.Api.Domain.Repositories;

public interface ICourseRepository
{
    Task<(IReadOnlyList<Course> Items, int TotalCount)> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<Course?> GetAsync(Guid id, CancellationToken ct);
    Task<Course> CreateAsync(Course course, CancellationToken ct);
    Task<Course?> UpdateAsync(Course course, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByTitleAsync(string title, Guid? excludeId, CancellationToken ct);
}

public sealed class InMemoryCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = new();
    private readonly object _lock = new();

    public InMemoryCourseRepository()
    {
        // Seed deterministic sample data
        _courses.AddRange(new[]
        {
            new Course { Title = "Safety Orientation", IsRequired = true, ValidityMonths = 12, Category = "Safety", Description = "Mandatory safety intro." },
            new Course { Title = "Electrical Compliance 101", IsRequired = true, ValidityMonths = 24, Category = "Compliance", Description = "Electrical standards overview." },
            new Course { Title = "Leadership Essentials", IsRequired = false, Category = "Development", Description = "Soft skills for team leads." }
        });
    }

    public Task<(IReadOnlyList<Course> Items, int TotalCount)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        var skip = (page - 1) * pageSize;
        List<Course> slice;
        int total;
        lock (_lock)
        {
            total = _courses.Count;
            slice = _courses.Skip(skip).Take(pageSize).ToList();
        }
        return Task.FromResult(((IReadOnlyList<Course>)slice, total));
    }

    public Task<Course?> GetAsync(Guid id, CancellationToken ct)
    {
        Course? found;
        lock (_lock)
        {
            found = _courses.FirstOrDefault(c => c.Id == id);
        }
        return Task.FromResult(found);
    }

    public Task<Course> CreateAsync(Course course, CancellationToken ct)
    {
        if (course.Id == Guid.Empty)
        {
            course.Id = Guid.NewGuid();
        }
        course.CreatedUtc = DateTime.UtcNow;
        
        lock (_lock)
        {
            _courses.Add(course);
        }
        return Task.FromResult(course);
    }

    public Task<Course?> UpdateAsync(Course course, CancellationToken ct)
    {
        lock (_lock)
        {
            var existing = _courses.FirstOrDefault(c => c.Id == course.Id);
            if (existing == null)
            {
                return Task.FromResult<Course?>(null);
            }

            existing.Title = course.Title;
            existing.IsRequired = course.IsRequired;
            existing.IsActive = course.IsActive;
            existing.ValidityMonths = course.ValidityMonths;
            existing.Category = course.Category;
            existing.Description = course.Description;
            
            return Task.FromResult<Course?>(existing);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        lock (_lock)
        {
            var course = _courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
            {
                return Task.FromResult(false);
            }
            _courses.Remove(course);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsByTitleAsync(string title, Guid? excludeId, CancellationToken ct)
    {
        lock (_lock)
        {
            var exists = _courses.Any(c => c.Title == title && (!excludeId.HasValue || c.Id != excludeId.Value));
            return Task.FromResult(exists);
        }
    }
}