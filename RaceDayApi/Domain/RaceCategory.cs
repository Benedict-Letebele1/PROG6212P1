namespace RaceDayApi.Domain;

/// <summary>An age band inside an event, such as Open or Veteran. Null bounds mean the band is open-ended.</summary>
public class RaceCategory
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public RaceEvent Event { get; set; } = null!;

    public List<Enrolment> Enrolments { get; set; } = [];
}
