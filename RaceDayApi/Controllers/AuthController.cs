using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>Registration, login and logout. Login starts a server-side session that holds the user ID and role.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Creates an Organiser or Participant account.</summary>
    /// <remarks>
    /// Public. Organisers must send organisationName and contactNumber. Participants must send dateOfBirth.
    /// Passwords are stored only as BCrypt hashes. This does not log the new user in; call /api/auth/login next.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var emailError = ProfileRules.EmailError(request.Email);
        if (emailError is not null)
        {
            return BadRequest(new ErrorResponse(emailError));
        }

        // I check the password here so a weak one never reaches the hasher.
        var passwordError = ProfileRules.PasswordError(request.Password);
        if (passwordError is not null)
        {
            return BadRequest(new ErrorResponse(passwordError));
        }

        var fullNameError = ProfileRules.FullNameError(request.FullName);
        if (fullNameError is not null)
        {
            return BadRequest(new ErrorResponse(fullNameError));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();

        var role = RoleFrom(request.Role);
        if (role is null)
        {
            return BadRequest(new ErrorResponse("Role must be Organiser or Participant."));
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return BadRequest(new ErrorResponse("An account with this email already exists."));
        }

        var user = new AppUser
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password, 11),
            FullName = fullName,
            Role = role
        };

        if (role == Roles.Organiser)
        {
            var organiserError = ProfileRules.OrganiserDetailsError(request.OrganisationName, request.ContactNumber);
            if (organiserError is not null)
            {
                return BadRequest(new ErrorResponse(organiserError));
            }

            user.Organiser = new OrganiserProfile
            {
                OrganisationName = request.OrganisationName!.Trim(),
                ContactNumber = request.ContactNumber!.Trim()
            };
        }
        else
        {
            var participantError = ProfileRules.ParticipantDetailsError(request.DateOfBirth);
            if (participantError is not null)
            {
                return BadRequest(new ErrorResponse(participantError));
            }

            user.Participant = new ParticipantProfile { DateOfBirth = request.DateOfBirth!.Value };
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }

    /// <summary>Logs in with email and password and starts a session.</summary>
    /// <remarks>
    /// Public. The session cookie (RaceDayApi.Session) must be sent with later requests. Swagger UI
    /// sends it automatically after a successful login. Wrong email and wrong password get the same message.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Organiser)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password ?? string.Empty, user.PasswordHash))
        {
            return Unauthorized(new ErrorResponse("Invalid email or password."));
        }

        // I clear any old session first so an earlier login cannot carry over.
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserID, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(ToResponse(user));
    }

    /// <summary>Ends the current session.</summary>
    /// <remarks>Any logged-in user. Later calls that need a session return 401.</remarks>
    [HttpPost("logout")]
    [RequireSession]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("You have been logged out."));
    }

    /// <summary>Maps the requested role to the stored role name, ignoring case. Returns null if it is not a valid role.</summary>
    private static string? RoleFrom(string? requested)
    {
        if (string.Equals(requested, Roles.Organiser, StringComparison.OrdinalIgnoreCase)) return Roles.Organiser;
        if (string.Equals(requested, Roles.Participant, StringComparison.OrdinalIgnoreCase)) return Roles.Participant;
        return null;
    }

    private static AuthResponse ToResponse(AppUser user) =>
        new(user.UserID, user.Email, user.FullName, user.Role, user.Organiser?.OrganisationName);
}
