namespace RaceDayApi.Contracts;

/// <summary>
/// Request body for adding or replacing a category. Ages are optional, and a missing MaxAge means the band is open-ended.
/// </summary>
public record CategoryRequest(string Name, string? Description, int? MinAge, int? MaxAge);

/// <summary>A category inside an event.</summary>
public record CategoryResponse(int CategoryId, int EventId, string Name, string? Description, int? MinAge, int? MaxAge);
