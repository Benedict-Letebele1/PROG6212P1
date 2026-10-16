using System.Net;
using System.Net.Http.Json;
using RaceDayApi.Contracts;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Each role can reach only its own functions. Anonymous callers are sent to log in first.</summary>
public class RoleAccessTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Participant_CannotCreateAnEvent_Returns403()
    {
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.PostAsJsonAsync("/api/events", ApiTestHelpers.NewEventRequest("Jarred Sneak"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateAnEvent_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/events", ApiTestHelpers.NewEventRequest("Jarred Anon"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAddACategory_Returns403()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest("Jarred Open", null, 18, 39));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInAnEvent_Returns403()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewTheEnrolmentsOfAnEvent_Returns403()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureAResult_Returns403()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(1, "01:00:00", 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotReadMyResults_Returns403()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);

        var response = await organiser.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotChangeAnEnrolmentStatus_Returns403()
    {
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.PutAsJsonAsync("/api/enrolments/1/status", new StatusRequest("Confirmed"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotListEnrolments_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/enrolments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
