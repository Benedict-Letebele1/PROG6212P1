using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>Age bands inside an event. Anyone can read them; only the owning organiser can change them.</summary>
[ApiController]
[Produces("application/json")]
public class CategoriesController(RaceDayDbContext db, OrganiserAccess access) : ControllerBase
{
    /// <summary>Lists the categories of an event.</summary>
    /// <remarks>Public.</remarks>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType<List<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId, CancellationToken cancellationToken)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId, cancellationToken))
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryID)
            .Select(c => new CategoryResponse(c.CategoryID, c.EventID, c.Name, c.Description, c.MinAge, c.MaxAge))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    /// <summary>Adds a category to an event the logged-in organiser owns.</summary>
    /// <remarks>Organiser only, and only for the owner. Leave MaxAge empty for an open-ended band such as 50+.</remarks>
    [HttpPost("api/events/{eventId:int}/categories")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCategory(int eventId, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await access.FindEventAsync(eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || !OrganiserAccess.Owns(raceEvent, organiser))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only add categories to your own events."));
        }

        var error = EventRules.CategoryError(request);
        if (error is not null)
        {
            return BadRequest(new ErrorResponse(error));
        }

        var category = new RaceCategory
        {
            Event = raceEvent,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, Mappers.ToCategory(category));
    }

    /// <summary>Replaces a category the logged-in organiser owns.</summary>
    /// <remarks>Organiser only, and only for the owner of the event the category belongs to.</remarks>
    [HttpPut("api/categories/{id:int}")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryID == id, cancellationToken);
        if (category is null)
        {
            return NotFound(new ErrorResponse("Category not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || category.Event.OrganiserID != organiser.OrganiserID)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only change categories in your own events."));
        }

        var error = EventRules.CategoryError(request);
        if (error is not null)
        {
            return BadRequest(new ErrorResponse(error));
        }

        category.Name = request.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(Mappers.ToCategory(category));
    }

    /// <summary>Deletes a category the logged-in organiser owns.</summary>
    /// <remarks>
    /// Organiser only, and only for the owner. A category that already has enrolments cannot be deleted (409).
    /// </remarks>
    [HttpDelete("api/categories/{id:int}")]
    [RequireSession(Role = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryID == id, cancellationToken);
        if (category is null)
        {
            return NotFound(new ErrorResponse("Category not found."));
        }

        var organiser = await access.FindOrganiserAsync(HttpContext.CurrentUserId(), cancellationToken);
        if (organiser is null || category.Event.OrganiserID != organiser.OrganiserID)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("You can only delete categories in your own events."));
        }

        if (await db.Enrolments.AnyAsync(en => en.CategoryID == id, cancellationToken))
        {
            return Conflict(new ErrorResponse("This category has enrolments, so it cannot be deleted."));
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
