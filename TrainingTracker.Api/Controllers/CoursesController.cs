using Microsoft.AspNetCore.Mvc;
using TrainingTracker.Api.Contracts.Courses;
using TrainingTracker.Api.Domain.Services;

namespace TrainingTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _service;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(ICourseService service, ILogger<CoursesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CourseSummaryDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _service.ListAsync(page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDetailDto>> Get(Guid id, CancellationToken ct = default)
    {
        var course = await _service.GetAsync(id, ct);
        if (course == null)
        {
            return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "Course not found" });
        }
        return Ok(course);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDetailDto>> Create([FromBody] CreateCourseRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create course: {Message}", ex.Message);
            return Conflict(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDetailDto>> Update(Guid id, [FromBody] UpdateCourseRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _service.UpdateAsync(id, request, ct);
            if (updated == null)
            {
                return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "Course not found" });
            }
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update course: {Message}", ex.Message);
            return Conflict(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "Course not found" });
        }
        return NoContent();
    }
}