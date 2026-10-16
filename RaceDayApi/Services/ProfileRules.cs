namespace RaceDayApi.Services;

/// <summary>The role-specific checks used by both registration and profile updates.</summary>
public static class ProfileRules
{
    /// <summary>Returns the reason the email is invalid, or null when it is valid.</summary>
    public static string? EmailError(string? email)
    {
        var trimmed = (email ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > 255 || !System.Net.Mail.MailAddress.TryCreate(trimmed, out _))
        {
            return "Enter a valid email address.";
        }

        return null;
    }

    /// <summary>Returns the reason the full name is invalid, or null when it is valid.</summary>
    public static string? FullNameError(string? fullName)
    {
        var trimmed = (fullName ?? string.Empty).Trim();
        return trimmed.Length is 0 or > 100 ? "Full name is required and must be 100 characters or fewer." : null;
    }

    /// <summary>Returns the reason the password is invalid, or null when it is valid.</summary>
    public static string? PasswordError(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 100
            ? "Password must be between 8 and 100 characters."
            : null;

    /// <summary>Returns the reason the organiser details are invalid, or null when they are valid.</summary>
    public static string? OrganiserDetailsError(string? organisationName, string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(organisationName) || organisationName.Trim().Length > 100)
        {
            return "Organisation name is required and must be 100 characters or fewer.";
        }

        if (string.IsNullOrWhiteSpace(contactNumber) || contactNumber.Trim().Length > 20)
        {
            return "Contact number is required and must be 20 characters or fewer.";
        }

        return null;
    }

    /// <summary>Returns the reason the participant details are invalid, or null when they are valid.</summary>
    public static string? ParticipantDetailsError(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is null)
        {
            return "Date of birth is required for participants.";
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dateOfBirth.Value.Year < 1900 || dateOfBirth.Value > today)
        {
            return "Date of birth must be in the past and after 1900.";
        }

        return null;
    }
}
