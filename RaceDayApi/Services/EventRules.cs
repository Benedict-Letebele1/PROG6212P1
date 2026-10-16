using RaceDayApi.Contracts;
using RaceDayApi.Domain;

namespace RaceDayApi.Services;

/// <summary>Validation for events and categories. Each check returns the reason to show, or null when the input is valid.</summary>
public static class EventRules
{
    /// <summary>Returns the stored event type for a value such as "run" or "CYCLE", or null if it is not a valid type.</summary>
    public static string? EventTypeFrom(string? value) =>
        EventTypes.All.FirstOrDefault(type => string.Equals(type, value?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the stored enrolment status for a value, ignoring case, or null if it is not valid.</summary>
    public static string? EnrolmentStatusFrom(string? value) =>
        EnrolmentStatuses.All.FirstOrDefault(status => string.Equals(status, value?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string? EventError(EventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return "Name is required and must be 100 characters or fewer.";
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 500)
        {
            return "Description is required and must be 500 characters or fewer.";
        }

        if (request.Date == default)
        {
            return "Date is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Location) || request.Location.Trim().Length > 200)
        {
            return "Location is required and must be 200 characters or fewer.";
        }

        // The Distance column is DECIMAL(5,2), so the largest value it can hold is 999.99 km.
        if (request.Distance <= 0 || request.Distance > 999.99m)
        {
            return "Distance must be more than 0 km and no more than 999.99 km.";
        }

        if (EventTypeFrom(request.EventType) is null)
        {
            return "Event type must be Run, Walk or Cycle.";
        }

        if (request.BannerImageUrl is { Length: > 500 })
        {
            return "Banner image URL must be 500 characters or fewer.";
        }

        return null;
    }

    public static string? CategoryError(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 50)
        {
            return "Category name is required and must be 50 characters or fewer.";
        }

        if (request.Description is { Length: > 200 })
        {
            return "Category description must be 200 characters or fewer.";
        }

        if (request.MinAge is < 0 or > 120 || request.MaxAge is < 0 or > 120)
        {
            return "Ages must be between 0 and 120.";
        }

        if (request.MinAge is { } min && request.MaxAge is { } max && max < min)
        {
            return "The maximum age cannot be less than the minimum age.";
        }

        return null;
    }
}
