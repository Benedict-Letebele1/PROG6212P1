using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Contracts;
using RaceDayApi.Domain;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Participants enrol with a category, and the enrolment is stored as the link between participant, event and category.</summary>
public class EnrolmentTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Participant_EnrolsWithACategory_Returns201_AndTheLinkIsRecorded()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId, "Open", 20, 39);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory, new DateOnly(1995, 1, 1));

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await ApiTestHelpers.ReadAsync<EnrolmentResponse>(response);
        Assert.Equal(EnrolmentStatuses.Pending, enrolment!.Status);
        Assert.Equal(categoryId, enrolment.CategoryId);

        var stored = await factory.WithDbAsync(db => db.Enrolments.SingleAsync(en => en.EnrolmentID == enrolment.EnrolmentId));
        Assert.Equal(eventId, stored.EventID);
        Assert.Equal(categoryId, stored.CategoryID);
    }

    [Fact]
    public async Task Participant_EnrolsTwiceInTheSameEvent_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId))).EnsureSuccessStatusCode();

        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("already enrolled", (await ApiTestHelpers.ReadErrorAsync(second))!.Message);
    }

    [Fact]
    public async Task Participant_OutsideTheAgeRangeOnTheEventDate_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser, date: new DateTime(2027, 6, 1, 7, 0, 0));
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId, "Open", 20, 39);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory, new DateOnly(1970, 1, 1));

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("outside the Open category", (await ApiTestHelpers.ReadErrorAsync(response))!.Message);
    }

    [Fact]
    public async Task Participant_UsingACategoryFromAnotherEvent_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventA = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Event A");
        var eventB = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Event B");
        var categoryOfB = await ApiTestHelpers.CreateCategoryAsync(organiser, eventB);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventA}/enrol", new EnrolRequest(categoryOfB));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewsEnrolmentsForTheirEvent_IncludesTheParticipantName()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId))).EnsureSuccessStatusCode();

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await ApiTestHelpers.ReadAsync<List<EnrolmentResponse>>(response);
        Assert.Single(list!);
        Assert.Equal("Jarred Participant", list![0].ParticipantName);
    }

    [Fact]
    public async Task Organiser_OfAnotherEvent_CannotViewTheEnrolments_Returns403()
    {
        var owner = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(owner);
        var intruder = await ApiTestHelpers.OrganiserClientAsync(factory);

        var response = await intruder.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ConfirmsAnEnrolment_Returns200_AndInvalidStatusReturns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        var enrolment = await ApiTestHelpers.ReadAsync<EnrolmentResponse>(
            await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId)));

        var confirmed = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentId}/status", new StatusRequest("confirmed"));
        var invalid = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status", new StatusRequest("Maybe"));

        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.Equal(EnrolmentStatuses.Confirmed, (await ApiTestHelpers.ReadAsync<EnrolmentResponse>(confirmed))!.Status);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Participant_ListsOnlyTheirOwnEnrolments()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var mine = await ApiTestHelpers.ParticipantClientAsync(factory);
        var theirs = await ApiTestHelpers.ParticipantClientAsync(factory);
        (await mine.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId))).EnsureSuccessStatusCode();
        (await theirs.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId))).EnsureSuccessStatusCode();

        var list = await ApiTestHelpers.ReadAsync<List<EnrolmentResponse>>(await mine.GetAsync("/api/enrolments"));

        Assert.Single(list!);
        Assert.Equal(eventId, list![0].EventId);
    }
}
