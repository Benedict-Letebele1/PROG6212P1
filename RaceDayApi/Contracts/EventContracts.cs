namespace RaceDayApi.Contracts;

/// <summary>
/// Request body for creating or replacing an event. Date is the start date and time, Distance is in km,
/// and EventType is Run, Walk or Cycle (any letter case). BannerImageUrl is optional.
/// </summary>
public record EventRequest(
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl);

/// <summary>One row in the event list.</summary>
public record EventSummaryResponse(
    int EventId,
    string Name,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    string OrganisationName);

/// <summary>One event with its categories.</summary>
public record EventDetailResponse(
    int EventId,
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    string OrganisationName,
    List<CategoryResponse> Categories);

/// <summary>The URL of a banner image that was just saved.</summary>
public record BannerUploadResponse(string ImageUrl);
