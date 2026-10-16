using System.Net;
using System.Net.Http.Json;
using RaceDayApi.Contracts;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Organisers capture finish times and positions. Participants read back their own results.</summary>
public class ResultTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Organiser_CapturesAResult_AndTheParticipantSeesItInTheirResults()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        var enrolment = await ApiTestHelpers.ReadAsync<EnrolmentResponse>(
            await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId)));

        var captured = await organiser.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(enrolment!.EnrolmentId, "03:28:45", 47));
        var mine = await participant.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Created, captured.StatusCode);
        var result = await ApiTestHelpers.ReadAsync<ResultResponse>(captured);
        Assert.Equal("03:28:45", result!.FinishTime);
        Assert.Equal(47, result.FinishPosition);

        var list = await ApiTestHelpers.ReadAsync<List<ResultResponse>>(mine);
        Assert.Contains(list!, r => r.ResultId == result.ResultId);
    }

    [Fact]
    public async Task Organiser_CapturesASecondResultForTheSameEnrolment_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        var enrolment = await ApiTestHelpers.ReadAsync<EnrolmentResponse>(
            await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId)));
        (await organiser.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(enrolment!.EnrolmentId, "01:00:00", 3))).EnsureSuccessStatusCode();

        var second = await organiser.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(enrolment.EnrolmentId, "01:01:00", 4));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureAResult_ForAnotherOrganisersEvent_Returns403()
    {
        var owner = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(owner);
        var intruder = await ApiTestHelpers.OrganiserClientAsync(factory);

        var response = await intruder.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(1, "01:00:00", 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesAResult_Returns200()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        var enrolment = await ApiTestHelpers.ReadAsync<EnrolmentResponse>(
            await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new EnrolRequest(categoryId)));
        var captured = await ApiTestHelpers.ReadAsync<ResultResponse>(
            await organiser.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(enrolment!.EnrolmentId, "02:00:00", 9)));

        var response = await organiser.PutAsJsonAsync($"/api/results/{captured!.ResultId}", new UpdateResultRequest("01:58:30", 7));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ApiTestHelpers.ReadAsync<ResultResponse>(response);
        Assert.Equal("01:58:30", updated!.FinishTime);
        Assert.Equal(7, updated.FinishPosition);
    }

    [Fact]
    public async Task Result_WithAnInvalidFinishTime_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventId = await ApiTestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/results", new ResultRequest(1, "soon", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Result_ForAnEnrolmentFromAnotherEvent_Returns400()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var eventA = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Result A");
        var eventB = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Result B");
        var categoryB = await ApiTestHelpers.CreateCategoryAsync(organiser, eventB);
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        var enrolmentInB = await ApiTestHelpers.ReadAsync<EnrolmentResponse>(
            await participant.PostAsJsonAsync($"/api/events/{eventB}/enrol", new EnrolRequest(categoryB)));

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventA}/results", new ResultRequest(enrolmentInB!.EnrolmentId, "01:00:00", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
