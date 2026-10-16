namespace RaceDayApi.Contracts;

/// <summary>
/// Request body for POST /api/auth/register. Organisers send organisationName and contactNumber.
/// Participants send dateOfBirth. Role is Organiser or Participant.
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

/// <summary>Request body for POST /api/auth/login.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>The logged-in user's identity and role, returned after register and login.</summary>
public record AuthResponse(int UserId, string Email, string FullName, string Role, string? OrganisationName);

/// <summary>A short confirmation message.</summary>
public record MessageResponse(string Message);
