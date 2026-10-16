using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Contracts;
using RaceDayApi.Domain;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Organisers manage their own events. Anyone can browse them.</summary>
public class EventManagementTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Organiser_CreatesEvent_Returns201_WithTheSubmittedDetails()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);

        var response = await organiser.PostAsJsonAsync("/api/events", ApiTestHelpers.NewEventRequest("Table Mountain Trail", EventTypes.Walk));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ApiTestHelpers.ReadAsync<EventDetailResponse>(response);
        Assert.Equal("Table Mountain Trail", created!.Name);
        Assert.Equal(EventTypes.Walk, created.EventType);
        Assert.Equal(21.10m, created.Distance);
        Assert.Empty(created.Categories);
    }

    [Fact]
    public async Task Organiser_UpdatesOwnEvent_Returns200()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Original Run");

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", ApiTestHelpers.NewEventRequest("Renamed Run", EventTypes.Cycle));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ApiTestHelpers.ReadAsync<EventDetailResponse>(response);
        Assert.Equal("Renamed Run", updated!.Name);
        Assert.Equal(EventTypes.Cycle, updated.EventType);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(owner);
        var intruder = await ApiTestHelpers.OrganiserClientAsync(factory);

        var response = await intruder.PutAsJsonAsync($"/api/events/{eventId}", ApiTestHelpers.NewEventRequest("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeletesEventWithoutEnrolments_Returns204_AndTheCategoriesGoToo()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        var fetch = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
        var categoriesLeft = await factory.WithDbAsync(db => db.Categories.CountAsync(c => c.EventID == eventId));
        Assert.Equal(0, categoriesLeft);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventWithEnrolments_Returns409()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId))).EnsureSuccessStatusCode();

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CanBrowseEvents_AndSeeTheirCategories()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser, "Public Browse Run");
        await ApiTestHelpers.CreateCategoryAsync(organiser, eventId, "Under 20", 15, 19);
        var anonymous = factory.CreateClient();

        var list = await anonymous.GetAsync("/api/events");
        var detail = await anonymous.GetAsync($"/api/events/{eventId}");
        var categories = await anonymous.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var events = await ApiTestHelpers.ReadAsync<List<EventSummaryResponse>>(list);
        Assert.Contains(events!, e => e.EventId == eventId);

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await ApiTestHelpers.ReadAsync<EventDetailResponse>(detail);
        Assert.Single(body!.Categories);
        Assert.Equal("Under 20", body.Categories[0].Name);

        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
    }

    [Fact]
    public async Task GetEvents_FilteredByType_OnlyReturnsMatchingEvents()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var cycleId = await ApiTestHelpers.CreateEventAsync(organiser, "Filter Cycle", EventTypes.Cycle);
        var runId = await ApiTestHelpers.CreateEventAsync(organiser, "Filter Run", EventTypes.Run);

        var response = await factory.CreateClient().GetAsync("/api/events?type=cycle");

        var events = await ApiTestHelpers.ReadAsync<List<EventSummaryResponse>>(response);
        Assert.Contains(events!, e => e.EventId == cycleId);
        Assert.DoesNotContain(events!, e => e.EventId == runId);
        Assert.All(events!, e => Assert.Equal(EventTypes.Cycle, e.EventType));
    }

    [Fact]
    public async Task GetEvents_FilteredByLocationAndDate_ReturnsOnlyThoseEvents()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var durban = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Durban", date: new DateTime(2027, 9, 12, 8, 0, 0));
        var other = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Elsewhere", date: new DateTime(2027, 9, 13, 8, 0, 0));
        await organiser.PutAsJsonAsync($"/api/events/{durban}", ApiTestHelpers.NewEventRequest("Jarred Durban", EventTypes.Run, new DateTime(2027, 9, 12, 8, 0, 0)) with { Location = "Durban, KwaZulu-Natal" });

        var response = await factory.CreateClient().GetAsync("/api/events?location=durban&date=2027-09-12");

        var events = await ApiTestHelpers.ReadAsync<List<EventSummaryResponse>>(response);
        Assert.Contains(events!, e => e.EventId == durban);
        Assert.DoesNotContain(events!, e => e.EventId == other);
    }

    [Fact]
    public async Task GetEvents_WithUnknownType_Returns400()
    {
        var response = await factory.CreateClient().GetAsync("/api/events?type=Swim");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ForUnknownId_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownEventType_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);

        var response = await organiser.PostAsJsonAsync("/api/events", ApiTestHelpers.NewEventRequest("Bad Type", "Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithDistanceOverTheColumnLimit_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var request = ApiTestHelpers.NewEventRequest("Too Long") with { Distance = 1000m };

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithANonImageFile_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([0x54, 0x65, 0x78, 0x74]);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "notes.txt");

        var response = await organiser.PostAsync($"/api/events/{eventId}/upload-banner", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
