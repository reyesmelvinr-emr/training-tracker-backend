using Microsoft.AspNetCore.Mvc;
using TrainingTracker.Api.Contracts.Admin;
using TrainingTracker.Api.Contracts.Users;
using TrainingTracker.Api.Domain.Services;

namespace TrainingTracker.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserService userService,
        ICourseService courseService,
        IEnrollmentService enrollmentService,
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get system statistics for admin dashboard
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        _logger.LogInformation("Fetching system statistics");

        // Fetch all data with large page size to get everything
        var usersData = await _userService.ListAsync(1, 1000, CancellationToken.None);
        var coursesData = await _courseService.ListAsync(1, 1000, CancellationToken.None);
        var enrollmentsData = await _enrollmentService.ListAsync(1, 1000, CancellationToken.None);

        var users = usersData.Items;
        var courses = coursesData.Items;
        var enrollments = enrollmentsData.Items;

        var stats = new StatisticsResponse
        {
            TotalUsers = usersData.TotalCount,
            ActiveUsers = users.Count(u => u.IsActive),
            InactiveUsers = users.Count(u => !u.IsActive),
            
            TotalCourses = coursesData.TotalCount,
            RequiredCourses = courses.Count(c => c.IsRequired),
            OptionalCourses = courses.Count(c => !c.IsRequired),
            
            TotalEnrollments = enrollmentsData.TotalCount,
            PendingEnrollments = enrollments.Count(e => e.Status == "PENDING"),
            ActiveEnrollments = enrollments.Count(e => e.Status == "ACTIVE"),
            CompletedEnrollments = enrollments.Count(e => e.Status == "COMPLETED"),
            CancelledEnrollments = enrollments.Count(e => e.Status == "CANCELLED"),
            
            CompletionRate = enrollmentsData.TotalCount > 0 
                ? Math.Round((double)enrollments.Count(e => e.Status == "COMPLETED") / enrollmentsData.TotalCount * 100, 1)
                : 0
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth()
    {
        _logger.LogInformation("Checking system health");

        var health = new HealthResponse
        {
            ApiStatus = "Healthy",
            DatabaseStatus = "Healthy",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Test database connectivity by querying users
            await _userService.ListAsync(1, 1, CancellationToken.None);
            health.DatabaseStatus = "Healthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            health.DatabaseStatus = "Unhealthy";
            health.DatabaseError = ex.Message;
        }

        return Ok(health);
    }

    /// <summary>
    /// Bulk activate/deactivate users
    /// </summary>
    [HttpPatch("users/bulk-status")]
    [ProducesResponseType(typeof(BulkUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUpdateUserStatus([FromBody] BulkUpdateUserStatusRequest request)
    {
        _logger.LogInformation("Bulk updating {Count} users to IsActive={IsActive}", 
            request.UserIds.Count, request.IsActive);

        var result = new BulkUpdateResponse
        {
            TotalRequested = request.UserIds.Count,
            SuccessCount = 0,
            FailedCount = 0,
            Errors = new List<string>()
        };

        foreach (var userId in request.UserIds)
        {
            try
            {
                var user = await _userService.GetAsync(userId, CancellationToken.None);
                if (user == null)
                {
                    result.FailedCount++;
                    result.Errors.Add($"User {userId} not found");
                    continue;
                }

                // Update user status by creating an update request
                var updateRequest = new UpdateUserRequest
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    IsActive = request.IsActive
                };
                
                await _userService.UpdateAsync(userId, updateRequest, CancellationToken.None);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user {UserId}", userId);
                result.FailedCount++;
                result.Errors.Add($"User {userId}: {ex.Message}");
            }
        }

        return Ok(result);
    }
}
