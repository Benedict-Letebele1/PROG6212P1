using System.Net.Http.Json;
using System.Text.Json;
using RaceDayApi.Contracts;
using RaceDayApi.Domain;

namespace RaceDayApi.Tests.Infrastructure;

/// <summary>Creates accounts, events and requests for the tests. Each test makes its own data, so tests never depend on each other.</summary>
public static class ApiTestHelpers
{
    public const string Password = "Password123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>An email address nobody else in the test run uses.</summary>
    public static string UniqueEmail(string prefix) => $"jarred.{prefix}.{Guid.NewGuid():N}@test.raceday";

    /// <summary>Registers an organiser, logs in, and returns a client that carries the session cookie.</summary>
    public static async Task<HttpClient> OrganiserClientAsync(RaceDayApiFactory factory, string organisationName = "Jarred Events")
    {
        var client = factory.CreateClient();
        var email = UniqueEmail("organiser");
        var registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, Password, "Jarred Organiser", Roles.Organiser, organisationName, "+27 11 000 0000", null));
        registered.EnsureSuccessStatusCode();

        await LoginAsync(client, email);
        return client;
    }

    /// <summary>Registers a participant, logs in, and returns a client that carries the session cookie.</summary>
    public static async Task<HttpClient> ParticipantClientAsync(RaceDayApiFactory factory, DateOnly? dateOfBirth = null)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail("participant");
        var registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, Password, "Jarred Participant", Roles.Participant, null, null, dateOfBirth ?? new DateOnly(1995, 1, 1)));
        registered.EnsureSuccessStatusCode();

        await LoginAsync(client, email);
        return client;
    }

    /// <summary>Logs the client in. The session cookie is kept for later requests on the same client.</summary>
    public static async Task LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
    }

    /// <summary>A valid event request. Override the parts a test cares about.</summary>
    public static EventRequest NewEventRequest(string name = "Jarred Run", string eventType = EventTypes.Run, DateTime? date = null) =>
        new(name, "Created by the automated tests", date ?? new DateTime(2027, 6, 1, 7, 0, 0), "Cape Town, Western Cape", 21.10m, eventType, null);

    /// <summary>Creates an event as the logged-in organiser and returns its ID.</summary>
    public static async Task<int> CreateEventAsync(HttpClient organiser, string name = "Jarred Run", string eventType = EventTypes.Run, DateTime? date = null)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(name, eventType, date));
        response.EnsureSuccessStatusCode();
        var detail = await ReadAsync<EventDetailResponse>(response);
        return detail!.EventId;
    }

    /// <summary>Adds a category to an event as the logged-in organiser and returns its ID.</summary>
    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "Open", int? minAge = 20, int? maxAge = 39)
    {
        var response = await organiser.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new CategoryRequest(name, "Created by the automated tests", minAge, maxAge));
        response.EnsureSuccessStatusCode();
        var category = await ReadAsync<CategoryResponse>(response);
        return category!.CategoryId;
    }

    public static async Task<T?> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions);

    public static async Task<ErrorResponse?> ReadErrorAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
}
