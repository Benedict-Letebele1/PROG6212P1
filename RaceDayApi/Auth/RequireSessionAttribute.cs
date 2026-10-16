using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayApi.Contracts;

namespace RaceDayApi.Auth;

/// <summary>
/// Stops a request with 401 when there is no logged-in session. If a role is given, it also stops
/// users with any other role with 403. The role is read from the server-side session, not from the request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSessionAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>The role allowed to call the action. Leave it empty to allow any logged-in user.</summary>
    public string? Role { get; init; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync();

        if (session.GetInt32(SessionKeys.UserID) is null)
        {
            context.Result = new ObjectResult(new ErrorResponse("You need to log in first."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (Role is not null && session.GetString(SessionKeys.Role) != Role)
        {
            context.Result = new ObjectResult(new ErrorResponse($"Only {Role}s can use this endpoint."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
