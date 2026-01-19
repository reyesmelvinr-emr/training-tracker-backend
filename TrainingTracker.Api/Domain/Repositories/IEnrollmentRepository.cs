using TrainingTracker.Api.Domain.Entities;

namespace TrainingTracker.Api.Domain.Repositories;

public interface IEnrollmentRepository
{
    Task<(IReadOnlyList<Enrollment> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<Enrollment?> GetAsync(Guid id, CancellationToken ct);
    Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken ct);
    Task<Enrollment> UpdateAsync(Enrollment enrollment, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    private readonly List<Enrollment> _enrollments = new();
    public InMemoryEnrollmentRepository()
    {
        // empty initial; could seed after linking with courses/users
    }
    public Task<(IReadOnlyList<Enrollment> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0) page = 1; if (pageSize <= 0) pageSize = 10;
        var q = _enrollments.OrderBy(e => e.EnrolledUtc);
        var total = q.Count();
        var slice = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<Enrollment>)slice, total));
    }
    public Task<Enrollment?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(_enrollments.FirstOrDefault(e => e.Id == id));
    
    public Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct) 
        => Task.FromResult(_enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId));
    
    public Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken ct)
    {
        _enrollments.Add(enrollment);
        return Task.FromResult(enrollment);
    }
    
    public Task<Enrollment> UpdateAsync(Enrollment enrollment, CancellationToken ct)
    {
        var existing = _enrollments.FirstOrDefault(e => e.Id == enrollment.Id);
        if (existing != null)
        {
            existing.Status = enrollment.Status;
            existing.CompletedUtc = enrollment.CompletedUtc;
        }
        return Task.FromResult(enrollment);
    }
    
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = _enrollments.FirstOrDefault(e => e.Id == id);
        if (existing != null)
        {
            _enrollments.Remove(existing);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
