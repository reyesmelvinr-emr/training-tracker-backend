using Microsoft.AspNetCore.Mvc;
using TrainingTracker.Api.Contracts.Courses;
using TrainingTracker.Api.Contracts.Enrollments;
using TrainingTracker.Api.Domain.Services;

namespace TrainingTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _service;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(IEnrollmentService service, ILogger<EnrollmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EnrollmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EnrollmentSummaryDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => Ok(await _service.ListAsync(page, pageSize, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentDetailDto>> Get(Guid id, CancellationToken ct = default)
    {
        var e = await _service.GetAsync(id, ct);
        if (e == null) return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "Enrollment not found" });
        return Ok(e);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentDetailDto>> Create([FromBody] CreateEnrollmentRequest request, CancellationToken ct = default)
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
            _logger.LogWarning(ex, "Failed to create enrollment: {Message}", ex.Message);
            return Conflict(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(EnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentDetailDto>> UpdateStatus(Guid id, [FromBody] UpdateEnrollmentStatusRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _service.UpdateStatusAsync(id, request, ct);
        if (updated == null)
        {
            return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "Enrollment not found" });
        }
        
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "Enrollment not found" });
        }
        return NoContent();
    }
}
