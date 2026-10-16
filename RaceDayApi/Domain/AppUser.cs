namespace RaceDayApi.Domain;

/// <summary>A login account. Every user is either an Organiser or a Participant.</summary>
public class AppUser
{
    public int UserID { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrganiserProfile? Organiser { get; set; }

    public ParticipantProfile? Participant { get; set; }
}
