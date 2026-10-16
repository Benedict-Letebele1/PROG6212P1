namespace RaceDayApi.Domain;

/// <summary>The organiser details for a user with the Organiser role.</summary>
public class OrganiserProfile
{
    public int OrganiserID { get; set; }

    public int UserID { get; set; }

    public string OrganisationName { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;

    public AppUser User { get; set; } = null!;

    public List<RaceEvent> Events { get; set; } = [];
}
