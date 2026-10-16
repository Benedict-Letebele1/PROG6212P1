namespace RaceDayApi.Domain;

/// <summary>
/// Links one participant to one event through a chosen category. This is the table that resolves
/// the many-to-many relationship between participants and events.
/// </summary>
public class Enrolment
{
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public int EventID { get; set; }

    public int CategoryID { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string Status { get; set; } = EnrolmentStatuses.Pending;

    public ParticipantProfile Participant { get; set; } = null!;

    public RaceEvent Event { get; set; } = null!;

    public RaceCategory Category { get; set; } = null!;

    public RaceResult? Result { get; set; }
}
