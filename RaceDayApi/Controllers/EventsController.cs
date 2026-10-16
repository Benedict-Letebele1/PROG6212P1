using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>Browsing events is public. Only the organiser who owns an event can change or delete it.</summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController(
    RaceDayDbContext db,
    OrganiserAccess access,
    IBannerStorage banners,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] AllowedBannerExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    /// <summary>Lists events, optionally filtered by type, date and location.</summary>
    /// <remarks>
    /// Public. type is Run, Walk or Cycle. date is a single day (yyyy-MM-dd). location is a case-insensitive
    /// part of the location text. Results are ordered by event date.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<List<EventSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? type,
        [FromQuery] DateOnly? date,
        [FromQuery] string? location,
        CancellationToken cancellationToken)
    {
        var query = db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            var eventType = EventRules.EventTypeFrom(type);
            if (eventType is null)
            {
                return BadRequest(new ErrorResponse("Type must be Run, Walk or Cycle."));
            }

            query = query.Where(e => e.EventType == eventType);
        }

        if (date is not null)
        {
            // I compare against a half-open range so events late in the day still match.
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(e => e.EventDate >= start && e.EventDate < end);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var term = location.Trim();
            query = query.Where(e => e.Location.Contains(term));
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryResponse(
                e.EventID,
                e.Name,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.BannerImageURL,
                e.Organiser.OrganisationName))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    /// <summary>Returns one event with its categories.</summary>
    /// <remarks>Public.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(id, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        return Ok(Mappers.ToEventDetail(raceEvent));
    }

    /// <summary>Creates an event owned by the logged-in organiser.</summary>
    /// <remarks>Organiser only. The event starts with no categories. Add them with the categories endpoints.</remarks>
    [HttpPost]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        var error = EventRules.EventError(request);
        if (error is not null)
        {
            return BadRequest(new ErrorResponse(error));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("Only organisers can create events."));
        }

        var raceEvent = new RaceEvent
        {
            Organiser = organiser,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.Date,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = EventRules.EventTypeFrom(request.EventType)!,
            BannerImageURL = string.IsNullOrWhiteSpace(request.BannerImageUrl) ? null : request.BannerImageUrl.Trim()
        };

        db.Events.Add(raceEvent);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEvent), new { id = raceEvent.EventID }, Mappers.ToEventDetail(raceEvent));
    }

    /// <summary>Replaces the details of an event the logged-in organiser owns.</summary>
    /// <remarks>Organiser only, and only for the organiser who created the event. Send the full event, as for create.</remarks>
    [HttpPut("{id:int}")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(id, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only change your own events."));
        }

        var error = EventRules.EventError(request);
        if (error is not null)
        {
            return BadRequest(new ErrorResponse(error));
        }

        raceEvent.Name = request.Name.Trim();
        raceEvent.Description = request.Description.Trim();
        raceEvent.EventDate = request.Date;
        raceEvent.Location = request.Location.Trim();
        raceEvent.Distance = request.Distance;
        raceEvent.EventType = EventRules.EventTypeFrom(request.EventType)!;
        raceEvent.BannerImageURL = string.IsNullOrWhiteSpace(request.BannerImageUrl) ? null : request.BannerImageUrl.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return Ok(Mappers.ToEventDetail(raceEvent));
    }

    /// <summary>Deletes an event the logged-in organiser owns, along with its categories.</summary>
    /// <remarks>
    /// Organiser only, and only for the owner. An event that already has enrolments cannot be deleted (409),
    /// so participant records are never lost by accident.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(id, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only delete your own events."));
        }

        if (await db.Enrolments.AnyAsync(en => en.EventID == id, cancellationToken))
        {
            return Conflict(new ErrorResponse("This event has enrolments, so it cannot be deleted."));
        }

        db.Events.Remove(raceEvent);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>Uploads a banner image for an event the logged-in organiser owns.</summary>
    /// <remarks>
    /// Organiser only, and only for the owner. Accepts JPG, PNG or WebP files up to 5 MB by default. The saved
    /// URL is stored on the event and returned in the response.
    /// </remarks>
    [HttpPost("{eventId:int}/upload-banner")]
    [RequireSession(Role = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<BannerUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile file, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only change your own events."));
        }

        var maxBytes = configuration.GetValue("Uploads:MaxBannerBytes", 5 * 1024 * 1024L);
        if (file.Length == 0)
        {
            return BadRequest(new ErrorResponse("Choose an image to upload."));
        }

        if (file.Length > maxBytes)
        {
            return BadRequest(new ErrorResponse("Banner images must be 5 MB or smaller."));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedBannerExtensions.Contains(extension))
        {
            return BadRequest(new ErrorResponse("Banner must be a JPG, PNG or WebP image."));
        }

        var url = await banners.SaveAsync(file, cancellationToken);
        raceEvent.BannerImageURL = url;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new BannerUploadResponse(url));
    }
}
