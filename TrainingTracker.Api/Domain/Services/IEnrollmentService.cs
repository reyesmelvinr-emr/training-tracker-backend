using TrainingTracker.Api.Contracts.Courses; // reuse PagedResponse
using TrainingTracker.Api.Contracts.Enrollments;
using TrainingTracker.Api.Domain.Repositories;

namespace TrainingTracker.Api.Domain.Services;

public interface IEnrollmentService
{
    Task<PagedResponse<EnrollmentSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<EnrollmentDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<EnrollmentDetailDto> CreateAsync(CreateEnrollmentRequest request, CancellationToken ct);
    Task<EnrollmentDetailDto?> UpdateStatusAsync(Guid id, UpdateEnrollmentStatusRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly ICourseRepository _courseRepo;

    public EnrollmentService(IEnrollmentRepository repo, IUserRepository userRepo, ICourseRepository courseRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
        _courseRepo = courseRepo;
    }

    public async Task<PagedResponse<EnrollmentSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListAsync(page, pageSize, ct);
        return new PagedResponse<EnrollmentSummaryDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<EnrollmentDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var e = await _repo.GetAsync(id, ct);
        return e is null ? null : Map(e);
    }

    public async Task<EnrollmentDetailDto> CreateAsync(CreateEnrollmentRequest request, CancellationToken ct)
    {
        // Validate user exists
        var user = await _userRepo.GetAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{request.UserId}' does not exist");
        }

        // Validate course exists
        var course = await _courseRepo.GetAsync(request.CourseId, ct);
        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID '{request.CourseId}' does not exist");
        }

        // Check for duplicate enrollment
        var existing = await _repo.GetByUserAndCourseAsync(request.UserId, request.CourseId, ct);
        if (existing != null)
        {
            throw new InvalidOperationException($"User is already enrolled in this course");
        }

        var enrollment = new Entities.Enrollment
        {
            CourseId = request.CourseId,
            UserId = request.UserId,
            Status = "PENDING",
            EnrolledUtc = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(enrollment, ct);
        return Map(created);
    }

    public async Task<EnrollmentDetailDto?> UpdateStatusAsync(Guid id, UpdateEnrollmentStatusRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetAsync(id, ct);
        if (existing == null)
        {
            return null;
        }

        existing.Status = request.Status;
        
        // Set CompletedUtc when status changes to COMPLETED
        if (request.Status.ToUpper() == "COMPLETED" && existing.CompletedUtc == null)
        {
            existing.CompletedUtc = DateTime.UtcNow;
        }

        var updated = await _repo.UpdateAsync(existing, ct);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        return await _repo.DeleteAsync(id, ct);
    }

    private static EnrollmentDetailDto Map(Entities.Enrollment e) => new()
    {
        Id = e.Id,
        CourseId = e.CourseId,
        UserId = e.UserId,
        Status = e.Status,
        EnrolledUtc = e.EnrolledUtc,
        CompletedUtc = e.CompletedUtc
    };
}
