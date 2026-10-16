using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Contracts;
using RaceDayApi.Domain;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Registration, login, session and profile behaviour.</summary>
public class AuthEndpointTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Register_Organiser_Returns201_AndStoresOnlyAHash()
    {
        var client = factory.CreateClient();
        var email = ApiTestHelpers.UniqueEmail("hash");

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, ApiTestHelpers.Password, "Jarred Hash", Roles.Organiser, "Jarred Events", "+27 11 000 0000", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ApiTestHelpers.ReadAsync<AuthResponse>(response);
        Assert.Equal(Roles.Organiser, body!.Role);

        var stored = await factory.WithDbAsync(db => db.Users.Include(u => u.Organiser).SingleAsync(u => u.Email == email));
        Assert.NotEqual(ApiTestHelpers.Password, stored.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify(ApiTestHelpers.Password, stored.PasswordHash));
        Assert.Equal("Jarred Events", stored.Organiser!.OrganisationName);
    }

    [Fact]
    public async Task Register_Participant_Returns201_AndCreatesParticipantProfile()
    {
        var client = factory.CreateClient();
        var email = ApiTestHelpers.UniqueEmail("participant-register");

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, ApiTestHelpers.Password, "Jarred Runner", Roles.Participant, null, null, new DateOnly(1990, 5, 15)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Users.Include(u => u.Participant).SingleAsync(u => u.Email == email));
        Assert.Equal(Roles.Participant, stored.Role);
        Assert.Equal(new DateOnly(1990, 5, 15), stored.Participant!.DateOfBirth);
    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns400()
    {
        var email = ApiTestHelpers.UniqueEmail("duplicate");
        var first = factory.CreateClient();
        (await first.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, ApiTestHelpers.Password, "Jarred Duplicate", Roles.Participant, null, null, new DateOnly(1995, 1, 1))))
            .EnsureSuccessStatusCode();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, ApiTestHelpers.Password, "Jarred Duplicate", Roles.Participant, null, null, new DateOnly(1995, 1, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already exists", (await ApiTestHelpers.ReadErrorAsync(response))!.Message);
    }

    [Fact]
    public async Task Register_WithUnknownRole_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            ApiTestHelpers.UniqueEmail("admin"), ApiTestHelpers.Password, "Jarred Admin", "Administrator", null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            ApiTestHelpers.UniqueEmail("no-dob"), ApiTestHelpers.Password, "Jarred NoDob", Roles.Participant, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Date of birth", (await ApiTestHelpers.ReadErrorAsync(response))!.Message);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            ApiTestHelpers.UniqueEmail("short"), "short", "Jarred Short", Roles.Participant, null, null, new DateOnly(1995, 1, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200_AndSetsSessionCookie()
    {
        var client = factory.CreateClient();
        var email = ApiTestHelpers.UniqueEmail("login-ok");
        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, ApiTestHelpers.Password, "Jarred Login", Roles.Participant, null, null, new DateOnly(1995, 1, 1))))
            .EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, ApiTestHelpers.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiTestHelpers.ReadAsync<AuthResponse>(response);
        Assert.Equal(Roles.Participant, body!.Role);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, cookie => cookie.StartsWith("RaceDayApi.Session=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var email = ApiTestHelpers.UniqueEmail("login-bad");
        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, ApiTestHelpers.Password, "Jarred Bad", Roles.Organiser, "Jarred Events", "+27 11 000 0000", null)))
            .EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401_WithTheSameMessageAsAWrongPassword()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(
            ApiTestHelpers.UniqueEmail("nobody"), ApiTestHelpers.Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid email or password.", (await ApiTestHelpers.ReadErrorAsync(response))!.Message);
    }

    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsTheOrganisersDetails()
    {
        var client = await ApiTestHelpers.OrganiserClientAsync(factory, "Jarred Profile Events");

        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await ApiTestHelpers.ReadAsync<ProfileResponse>(response);
        Assert.Equal(Roles.Organiser, profile!.Role);
        Assert.Equal("Jarred Profile Events", profile.OrganisationName);
    }

    [Fact]
    public async Task UpdateProfile_ChangesTheName_AndRejectsAnEmailOwnedByAnotherAccount()
    {
        var client = await ApiTestHelpers.ParticipantClientAsync(factory);
        var other = await ApiTestHelpers.ParticipantClientAsync(factory);
        var otherProfile = await ApiTestHelpers.ReadAsync<ProfileResponse>(await other.GetAsync("/api/users/profile"));
        var current = await ApiTestHelpers.ReadAsync<ProfileResponse>(await client.GetAsync("/api/users/profile"));

        var renamed = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest(
            "Jarred Renamed", current!.Email, null, null, new DateOnly(1995, 1, 1)));
        var takenEmail = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest(
            "Jarred Renamed", otherProfile!.Email, null, null, new DateOnly(1995, 1, 1)));

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("Jarred Renamed", (await ApiTestHelpers.ReadAsync<ProfileResponse>(renamed))!.FullName);
        Assert.Equal(HttpStatusCode.BadRequest, takenEmail.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession_SoProtectedEndpointsReturn401Again()
    {
        var client = await ApiTestHelpers.ParticipantClientAsync(factory);
        (await client.GetAsync("/api/users/profile")).EnsureSuccessStatusCode();

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
