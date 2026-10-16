namespace RaceDayApi.Domain;

/// <summary>The participant details for a user with the Participant role.</summary>
public class ParticipantProfile
{
    public int ParticipantID { get; set; }

    public int UserID { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string? ProfilePictureURL { get; set; }

    public AppUser User { get; set; } = null!;

    public List<Enrolment> Enrolments { get; set; } = [];
}
