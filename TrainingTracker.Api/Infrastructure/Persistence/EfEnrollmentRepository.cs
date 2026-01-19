using Microsoft.EntityFrameworkCore;
using TrainingTracker.Api.Domain.Entities;
using TrainingTracker.Api.Domain.Repositories;

namespace TrainingTracker.Api.Infrastructure.Persistence;

public class EfEnrollmentRepository : IEnrollmentRepository
{
    private readonly TrainingTrackerDbContext _db;
    public EfEnrollmentRepository(TrainingTrackerDbContext db) { _db = db; }
    
    public async Task<(IReadOnlyList<Enrollment> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0) page = 1; if (pageSize <= 0) pageSize = 10;
        var q = _db.Enrollments.AsNoTracking().OrderBy(e => e.Status).ThenBy(e => e.Id); // Order by Status+Id (exist in DB) instead of EnrolledUtc (NotMapped)
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
    
    public Task<Enrollment?> GetAsync(Guid id, CancellationToken ct) 
        => _db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    
    public Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
        => _db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);
    
    public async Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken ct)
    {
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);
        return enrollment;
    }
    
    public async Task<Enrollment> UpdateAsync(Enrollment enrollment, CancellationToken ct)
    {
        _db.Enrollments.Update(enrollment);
        await _db.SaveChangesAsync(ct);
        return enrollment;
    }
    
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FindAsync(new object[] { id }, ct);
        if (enrollment == null) return false;
        
        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
