namespace RaceDayApi.Auth;

/// <summary>Helpers for reading the logged-in user from the session.</summary>
public static class SessionExtensions
{
    /// <summary>Returns the logged-in user's ID. Only call this from an action marked [RequireSession].</summary>
    public static int CurrentUserId(this HttpContext context) =>
        context.Session.GetInt32(SessionKeys.UserID)
        ?? throw new InvalidOperationException("No logged-in user. Add [RequireSession] to this action.");
}
