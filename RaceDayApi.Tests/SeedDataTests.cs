using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Contracts;
using RaceDayApi.Data;
using RaceDayApi.Domain;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>The demo data must match docs/RaceDay-Schema.sql, and every seeded enrolment must fit its age range.</summary>
public class SeedDataTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    /// <summary>Loads the demo data. The seeder does nothing when data already exists, so every test can call this.</summary>
    private Task SeedAsync() => factory.WithDbAsync(async db =>
    {
        await DemoDataSeeder.SeedAsync(db);
        return 0;
    });

    [Fact]
    public async Task DemoData_LoadsTheRowCountsFromTheSqlScript()
    {
        await SeedAsync();

        var counts = await factory.WithDbAsync(async db => new
        {
            Users = await db.Users.CountAsync(),
            Organisers = await db.Organisers.CountAsync(),
            Participants = await db.Participants.CountAsync(),
            Events = await db.Events.CountAsync(),
            Categories = await db.Categories.CountAsync(),
            Enrolments = await db.Enrolments.CountAsync(),
            Results = await db.Results.CountAsync()
        });

        Assert.Equal(6, counts.Users);
        Assert.Equal(2, counts.Organisers);
        Assert.Equal(4, counts.Participants);
        Assert.Equal(4, counts.Events);
        Assert.Equal(13, counts.Categories);
        Assert.Equal(8, counts.Enrolments);
        Assert.Equal(6, counts.Results);
    }

    [Fact]
    public async Task DemoData_EveryEnrolment_FitsItsCategoryAgeRangeOnTheEventDate()
    {
        await SeedAsync();

        var enrolments = await factory.WithDbAsync(db => db.Enrolments
            .Include(en => en.Category)
            .Include(en => en.Event)
            .Include(en => en.Participant)
            .ToListAsync());

        foreach (var enrolment in enrolments)
        {
            var onDay = DateOnly.FromDateTime(enrolment.Event.EventDate);
            var age = onDay.Year - enrolment.Participant.DateOfBirth.Year;
            if (onDay < enrolment.Participant.DateOfBirth.AddYears(age))
            {
                age--;
            }

            if (enrolment.Category.MinAge is { } min)
            {
                Assert.True(age >= min, $"Enrolment {enrolment.EnrolmentID} is below the {enrolment.Category.Name} minimum.");
            }

            if (enrolment.Category.MaxAge is { } max)
            {
                Assert.True(age <= max, $"Enrolment {enrolment.EnrolmentID} is above the {enrolment.Category.Name} maximum.");
            }
        }
    }

    [Fact]
    public async Task DemoAccounts_CanLogIn_WithTheDocumentedPassword()
    {
        await SeedAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("events@joburgrunning.co.za", DemoDataSeeder.DemoPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Roles.Organiser, (await ApiTestHelpers.ReadAsync<AuthResponse>(response))!.Role);
    }
}
