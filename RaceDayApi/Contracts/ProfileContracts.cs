namespace RaceDayApi.Contracts;

/// <summary>The profile of the logged-in user. Role-specific fields are null when they do not apply.</summary>
public record ProfileResponse(
    int UserId,
    string Email,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth,
    string? ProfilePictureUrl);

/// <summary>
/// Request body for PUT /api/users/profile. Organisers send organisationName and contactNumber.
/// Participants send dateOfBirth.
/// </summary>
public record UpdateProfileRequest(
    string FullName,
    string Email,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);
