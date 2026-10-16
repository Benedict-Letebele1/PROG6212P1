namespace RaceDayApi.Domain;

/// <summary>A road running, walking or cycling event owned by one organiser.</summary>
public class RaceEvent
{
    public int EventID { get; set; }

    public int OrganiserID { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public decimal Distance { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? BannerImageURL { get; set; }

    public DateTime CreatedAt { get; set; }

    public OrganiserProfile Organiser { get; set; } = null!;

    public List<RaceCategory> Categories { get; set; } = [];

    public List<Enrolment> Enrolments { get; set; } = [];
}
