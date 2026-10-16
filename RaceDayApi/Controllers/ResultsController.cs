using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>Organisers capture finish times and positions. Participants read their own results.</summary>
[ApiController]
[Produces("application/json")]
public class ResultsController(RaceDayDbContext db, OrganiserAccess access) : ControllerBase
{
    /// <summary>Lists the logged-in participant's results, newest event first.</summary>
    /// <remarks>Participant only.</remarks>
    [HttpGet("api/results/me")]
    [RequireSession(Role = Roles.Participant)]
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults(CancellationToken cancellationToken)
    {
        var participant = await db.Participants.SingleOrDefaultAsync(p => p.UserID == HttpContext.CurrentUserId(), cancellationToken);
        if (participant is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("Only participants have results."));
        }

        var results = await WithDetails(db.Results.AsNoTracking())
            .Where(r => r.Enrolment.ParticipantID == participant.ParticipantID)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .ToListAsync(cancellationToken);

        return Ok(results.Select(Mappers.ToResult).ToList());
    }

    /// <summary>Lists the results for an event the logged-in organiser owns, by finishing position.</summary>
    /// <remarks>Organiser only, and only for the owner of the event.</remarks>
    [HttpGet("api/events/{eventId:int}/results")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only view results for your own events."));
        }

        var results = await WithDetails(db.Results.AsNoTracking())
            .Where(r => r.Enrolment.EventID == eventId)
            .OrderBy(r => r.FinishPosition)
            .ToListAsync(cancellationToken);

        return Ok(results.Select(Mappers.ToResult).ToList());
    }

    /// <summary>Records the finish time and position for one enrolment in an event the logged-in organiser owns.</summary>
    /// <remarks>
    /// Organiser only, and only for the owner. FinishTime is HH:mm:ss. FinishPosition starts at 1. The
    /// enrolment must belong to this event and have no result yet.
    /// </remarks>
    [HttpPost("api/events/{eventId:int}/results")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptureResult(int eventId, [FromBody] ResultRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only capture results for your own events."));
        }

        if (request.FinishPosition < 1)
        {
            return BadRequest(new ErrorResponse("Finish position must be 1 or more."));
        }

        if (!TryParseFinishTime(request.FinishTime, out var finishTime))
        {
            return BadRequest(new ErrorResponse("Finish time must look like 03:28:45."));
        }

        var enrolment = await db.Enrolments
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Include(en => en.Participant).ThenInclude(p => p.User)
            .Include(en => en.Result)
            .SingleOrDefaultAsync(en => en.EnrolmentID == request.EnrolmentId && en.EventID == eventId, cancellationToken);
        if (enrolment is null)
        {
            return BadRequest(new ErrorResponse("That enrolment does not belong to this event."));
        }

        if (enrolment.Result is not null)
        {
            return BadRequest(new ErrorResponse("This enrolment already has a result. Use PUT /api/results/{id} to change it."));
        }

        var result = new RaceResult
        {
            Enrolment = enrolment,
            FinishTime = finishTime,
            FinishPosition = request.FinishPosition
        };

        db.Results.Add(result);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, Mappers.ToResult(result));
    }

    /// <summary>Changes the finish time or position of a result in an event the logged-in organiser owns.</summary>
    /// <remarks>Organiser only, and only for the owner of the event. Send both FinishTime and FinishPosition.</remarks>
    [HttpPut("api/results/{id:int}")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateResultRequest request, CancellationToken cancellationToken)
    {
        var result = await WithDetails(db.Results).SingleOrDefaultAsync(r => r.ResultID == id, cancellationToken);
        if (result is null)
        {
            return NotFound(new ErrorResponse("Result not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || result.Enrolment.Event.OrganiserID != organiser.OrganiserID)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only change results in your own events."));
        }

        if (request.FinishPosition < 1)
        {
            return BadRequest(new ErrorResponse("Finish position must be 1 or more."));
        }

        if (!TryParseFinishTime(request.FinishTime, out var finishTime))
        {
            return BadRequest(new ErrorResponse("Finish time must look like 03:28:45."));
        }

        result.FinishTime = finishTime;
        result.FinishPosition = request.FinishPosition;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(Mappers.ToResult(result));
    }

    /// <summary>Accepts HH:mm:ss (or H:mm:ss). A finish time of 00:00:00 is rejected.</summary>
    private static bool TryParseFinishTime(string? value, out TimeOnly finishTime)
    {
        var parsed = TimeOnly.TryParseExact(
            value?.Trim(),
            ["HH:mm:ss", "H:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out finishTime);

        return parsed && finishTime != TimeOnly.MinValue;
    }

    /// <summary>Includes the event, category, participant user and enrolment, which the result response needs.</summary>
    private static IQueryable<RaceResult> WithDetails(IQueryable<RaceResult> query) =>
        query
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category)
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant).ThenInclude(p => p.User);
}
