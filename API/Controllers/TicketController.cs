using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// IT Support Ticketing API
/// </summary>
/// <remarks>
/// Lets staff report IT issues and lets the IT team track them through to resolution
/// on a Kanban-style board.
///
/// ## Status Flow
/// `Open` -&gt; `Assigned` -&gt; `InProgress` -&gt; `Done` -&gt; `Closed`, with `Reopen` sending a
/// `Done`/`Closed` ticket back to `InProgress`.
///
/// - Only the assigned agent may move a ticket to `InProgress` or `Done`.
/// - Only a user with the `CanCloseTicket` permission may close a ticket.
/// - A ticket may be reopened by its original reporter or by a user with `CanCloseTicket`.
/// - Visibility: any user can see tickets they reported or are assigned to. Seeing every
///   ticket (the board) requires the `CanViewAllTickets` permission.
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/tickets")]
[Authorize]
[Tags("IT Support Tickets")]
public class TicketController(ITicketRepository repository) : ControllerBase
{
    /// <summary>
    /// Reports a new IT support ticket
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var departmentIdRaw = (string)HttpContext.Items["Department"];
        Guid? departmentId = Guid.TryParse(departmentIdRaw, out var parsedDepartmentId) ? parsedDepartmentId : null;

        var result = await repository.CreateTicket(request, Guid.Parse(userId), departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of tickets
    /// </summary>
    /// <remarks>
    /// Returns tickets the caller reported or is assigned to, unless they hold the
    /// `CanViewAllTickets` permission, in which case every ticket is returned. The board
    /// view uses this same endpoint with a large page size and groups the results by
    /// `status` client-side.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<TicketDto>>))]
    public async Task<IResult> GetTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] Guid? assignedToId = null,
        [FromQuery] Guid? reportedById = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetTickets(Guid.Parse(userId), page, pageSize, searchQuery, status,
            priority, category, assignedToId, reportedById);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a ticket by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TicketDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTicket([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetTicket(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Assigns (or reassigns) a ticket to an IT agent
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [Authorize(Policy = PermissionKeys.CanAssignTicket)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AssignTicket([FromRoute] Guid id, [FromBody] AssignTicketRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.AssignTicket(id, request.AssignedToId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Marks an assigned ticket as in progress. Only the assigned agent may call this.
    /// </summary>
    [HttpPut("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> StartProgress([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.StartProgress(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Marks a ticket as done. Only the assigned agent may call this.
    /// </summary>
    [HttpPut("{id:guid}/done")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkDone([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.MarkDone(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Closes a done ticket. Requires the `CanCloseTicket` permission.
    /// </summary>
    [HttpPut("{id:guid}/close")]
    [Authorize(Policy = PermissionKeys.CanCloseTicket)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CloseTicket([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CloseTicket(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Reopens a done or closed ticket. Callable by the original reporter or a user
    /// with the `CanCloseTicket` permission.
    /// </summary>
    [HttpPut("{id:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ReopenTicket([FromRoute] Guid id, [FromBody] ReopenTicketRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ReopenTicket(id, request.Reason, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Adds a comment to a ticket. Any user with access to the ticket may comment.
    /// </summary>
    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = PermissionKeys.CanCommentOnTicket)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddComment([FromRoute] Guid id, [FromBody] AddTicketCommentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.AddComment(id, request.Comment, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the comment thread and status-history timeline for a ticket
    /// </summary>
    [HttpGet("{id:guid}/activity")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TicketActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTicketActivity([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetTicketActivity(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
