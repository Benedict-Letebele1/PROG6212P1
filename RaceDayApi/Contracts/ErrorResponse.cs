namespace RaceDayApi.Contracts;

/// <summary>The body returned with every 400, 401, 403, 404 and 409 response from the API.</summary>
/// <param name="Message">A short, plain-language reason the request was refused.</param>
public record ErrorResponse(string Message);
