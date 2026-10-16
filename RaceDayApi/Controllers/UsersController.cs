using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>The logged-in user's own profile. Both roles can read and update their own details.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Returns the logged-in user's profile.</summary>
    /// <remarks>
    /// Any logged-in user. Organisers see organisation details; participants see their date of birth.
    /// </remarks>
    [HttpGet("profile")]
    [RequireSession]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new ErrorResponse("This account no longer exists."));
        }

        return Ok(ToProfile(user));
    }

    /// <summary>Updates the logged-in user's profile.</summary>
    /// <remarks>
    /// Any logged-in user. Everyone sends fullName and email. Organisers also send organisationName and
    /// contactNumber. Participants also send dateOfBirth. The email must not belong to another account.
    /// The role cannot be changed.
    /// </remarks>
    [HttpPut("profile")]
    [RequireSession]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new ErrorResponse("This account no longer exists."));
        }

        var fullNameError = ProfileRules.FullNameError(request.FullName);
        if (fullNameError is not null)
        {
            return BadRequest(new ErrorResponse(fullNameError));
        }

        var emailError = ProfileRules.EmailError(request.Email);
        if (emailError is not null)
        {
            return BadRequest(new ErrorResponse(emailError));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email && u.UserID != user.UserID, cancellationToken))
        {
            return BadRequest(new ErrorResponse("Another account already uses this email address."));
        }

        if (user.Role == Roles.Organiser)
        {
            var organiserError = ProfileRules.OrganiserDetailsError(request.OrganisationName, request.ContactNumber);
            if (organiserError is not null)
            {
                return BadRequest(new ErrorResponse(organiserError));
            }

            user.Organiser!.OrganisationName = request.OrganisationName!.Trim();
            user.Organiser.ContactNumber = request.ContactNumber!.Trim();
        }
        else
        {
            var participantError = ProfileRules.ParticipantDetailsError(request.DateOfBirth);
            if (participantError is not null)
            {
                return BadRequest(new ErrorResponse(participantError));
            }

            user.Participant!.DateOfBirth = request.DateOfBirth!.Value;
        }

        user.Email = email;
        user.FullName = request.FullName.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToProfile(user));
    }

    private Task<AppUser?> LoadCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = HttpContext.CurrentUserId();
        return db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .SingleOrDefaultAsync(u => u.UserID == userId, cancellationToken);
    }

    private static ProfileResponse ToProfile(AppUser user) =>
        new(
            user.UserID,
            user.Email,
            user.FullName,
            user.Role,
            user.Organiser?.OrganisationName,
            user.Organiser?.ContactNumber,
            user.Participant?.DateOfBirth,
            user.Participant?.ProfilePictureURL);
}
