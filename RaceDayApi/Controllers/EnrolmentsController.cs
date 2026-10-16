using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>Participants enrol in events by choosing a category. Organisers see and manage the enrolments of their own events.</summary>
[ApiController]
[Produces("application/json")]
public class EnrolmentsController(RaceDayDbContext db, OrganiserAccess access) : ControllerBase
{
    /// <summary>Lists enrolments. Organisers see every enrolment in their events; participants see their own.</summary>
    /// <remarks>Any logged-in user. The response depends on the role stored in the session.</remarks>
    [HttpGet("api/enrolments")]
    [RequireSession]
    [ProducesResponseType<List<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEnrolments(CancellationToken cancellationToken)
    {
        var userId = HttpContext.CurrentUserId();
        var query = WithDetails(db.Enrolments.AsNoTracking());

        if (HttpContext.Session.GetString(SessionKeys.Role) == Roles.Organiser)
        {
            var organiser = await access.FindOrganiserAsync(userId, cancellationToken);
            if (organiser is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("No organiser profile was found for this account."));
            }

            query = query.Where(en => en.Event.OrganiserID == organiser.OrganiserID);
        }
        else
        {
            var participant = await db.Participants.SingleOrDefaultAsync(p => p.UserID == userId, cancellationToken);
            if (participant is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("No participant profile was found for this account."));
            }

            query = query.Where(en => en.ParticipantID == participant.ParticipantID);
        }

        var enrolments = await query.OrderBy(en => en.EnrolmentID).ToListAsync(cancellationToken);
        return Ok(enrolments.Select(Mappers.ToEnrolment).ToList());
    }

    /// <summary>Lists every enrolment for an event the logged-in organiser owns.</summary>
    /// <remarks>Organiser only, and only for the owner of the event.</remarks>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<List<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only view enrolments for your own events."));
        }

        var enrolments = await WithDetails(db.Enrolments.AsNoTracking())
            .Where(en => en.EventID == eventId)
            .OrderBy(en => en.EnrolmentID)
            .ToListAsync(cancellationToken);

        return Ok(enrolments.Select(Mappers.ToEnrolment).ToList());
    }

    /// <summary>Enrols the logged-in participant in an event, using a category that belongs to it.</summary>
    /// <remarks>
    /// Participant only. The participant's age on the event date must fit the category's age range, and a
    /// participant can enrol only once per event. New enrolments start as Pending.
    /// </remarks>
    [HttpPost("api/events/{eventId:int}/enrol")]
    [RequireSession(Role = Roles.Participant)]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventID == eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var participant = await db.Participants
            .Include(p => p.User)
            .SingleOrDefaultAsync(p => p.UserID == HttpContext.CurrentUserId(), cancellationToken);
        if (participant is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("Only participants can enrol in events."));
        }

        var category = await db.Categories
            .SingleOrDefaultAsync(c => c.CategoryID == request.CategoryId && c.EventID == eventId, cancellationToken);
        if (category is null)
        {
            return BadRequest(new ErrorResponse("Choose a category that belongs to this event."));
        }

        if (await db.Enrolments.AnyAsync(en => en.ParticipantID == participant.ParticipantID && en.EventID == eventId, cancellationToken))
        {
            return BadRequest(new ErrorResponse("You are already enrolled in this event."));
        }

        var age = AgeOn(participant.DateOfBirth, DateOnly.FromDateTime(raceEvent.EventDate));
        if ((category.MinAge is { } min && age < min) || (category.MaxAge is { } max && age > max))
        {
            return BadRequest(new ErrorResponse($"You will be {age} on the event date, which is outside the {category.Name} category."));
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participant.ParticipantID,
            EventID = eventId,
            CategoryID = category.CategoryID,
            Status = EnrolmentStatuses.Pending,
            RegistrationDate = DateTime.Now,
            Participant = participant,
            Event = raceEvent,
            Category = category
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, Mappers.ToEnrolment(enrolment));
    }

    /// <summary>Changes the status of an enrolment in an event the logged-in organiser owns.</summary>
    /// <remarks>Organiser only, and only for the owner of the event. Status is Pending, Confirmed or Cancelled.</remarks>
    [HttpPut("api/enrolments/{id:int}/status")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusRequest request, CancellationToken cancellationToken)
    {
        var enrolment = await WithDetails(db.Enrolments).SingleOrDefaultAsync(en => en.EnrolmentID == id, cancellationToken);
        if (enrolment is null)
        {
            return NotFound(new ErrorResponse("Enrolment not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || enrolment.Event.OrganiserID != organiser.OrganiserID)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only change enrolments in your own events."));
        }

        var status = EventRules.EnrolmentStatusFrom(request.Status);
        if (status is null)
        {
            return BadRequest(new ErrorResponse("Status must be Pending, Confirmed or Cancelled."));
        }

        enrolment.Status = status;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(Mappers.ToEnrolment(enrolment));
    }

    /// <summary>Includes the event, category and participant user, which the enrolment response needs.</summary>
    private static IQueryable<Enrolment> WithDetails(IQueryable<Enrolment> query) =>
        query
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Include(en => en.Participant).ThenInclude(p => p.User);

    /// <summary>The age a person has reached on a given day.</summary>
    private static int AgeOn(DateOnly dateOfBirth, DateOnly onDay)
    {
        var age = onDay.Year - dateOfBirth.Year;
        if (onDay < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
