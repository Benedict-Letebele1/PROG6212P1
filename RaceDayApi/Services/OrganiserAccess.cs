using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Domain;

namespace RaceDayApi.Services;

/// <summary>Finds the organiser and event records that the ownership rules check against.</summary>
public class OrganiserAccess(RaceDayDbContext db)
{
    /// <summary>Returns the organiser profile for a user ID, or null if the user is not an organiser.</summary>
    public Task<OrganiserProfile?> FindOrganiserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Organisers.SingleOrDefaultAsync(o => o.UserID == userId, cancellationToken);

    /// <summary>Returns the event with its organiser and categories, or null if it does not exist.</summary>
    public Task<RaceEvent?> FindEventAsync(int eventId, CancellationToken cancellationToken = default) =>
        db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .SingleOrDefaultAsync(e => e.EventID == eventId, cancellationToken);

    /// <summary>True when the organiser created this event.</summary>
    public static bool Owns(RaceEvent raceEvent, OrganiserProfile organiser) =>
        raceEvent.OrganiserID == organiser.OrganiserID;
}
