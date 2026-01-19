using Microsoft.AspNetCore.Mvc;
using TrainingTracker.Api.Contracts.Courses;
using TrainingTracker.Api.Contracts.Users;
using TrainingTracker.Api.Domain.Services;

namespace TrainingTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IUserService service, ILogger<UsersController> logger) 
    { 
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserSummaryDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => Ok(await _service.ListAsync(page, pageSize, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> Get(Guid id, CancellationToken ct = default)
    {
        var u = await _service.GetAsync(id, ct);
        if (u == null) 
            return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "User not found" });
        return Ok(u);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct = default)
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
            _logger.LogWarning(ex, "Failed to create user: {Message}", ex.Message);
            return Conflict(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
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
                return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "User not found" });
            }
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update user: {Message}", ex.Message);
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
            return NotFound(new { traceId = HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName], message = "User not found" });
        }
        return NoContent();
    }
}
