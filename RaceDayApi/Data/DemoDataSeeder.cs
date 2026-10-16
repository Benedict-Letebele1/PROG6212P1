using Microsoft.EntityFrameworkCore;
using RaceDayApi.Domain;

namespace RaceDayApi.Data;

/// <summary>
/// Loads the same demo data as docs/RaceDay-Schema.sql into an empty database. Passwords are hashed
/// with BCrypt at load time, so every demo account signs in with <see cref="DemoPassword"/>.
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(RaceDayDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var hash = BCrypt.Net.BCrypt.EnhancedHashPassword(DemoPassword, 11);

        var david = new AppUser { Email = "events@joburgrunning.co.za", PasswordHash = hash, FullName = "David Mokoena", Role = Roles.Organiser };
        var michelle = new AppUser { Email = "info@capetowncycling.com", PasswordHash = hash, FullName = "Michelle Williams", Role = Roles.Organiser };
        var thomas = new AppUser { Email = "thomas.n@email.com", PasswordHash = hash, FullName = "Thomas Nkosi", Role = Roles.Participant };
        var priya = new AppUser { Email = "priya.s@email.com", PasswordHash = hash, FullName = "Priya Singh", Role = Roles.Participant };
        var kyle = new AppUser { Email = "kyle.b@email.com", PasswordHash = hash, FullName = "Kyle Botha", Role = Roles.Participant };
        var zanele = new AppUser { Email = "zanele.m@email.com", PasswordHash = hash, FullName = "Zanele Mthembu", Role = Roles.Participant };

        var joburg = new OrganiserProfile { User = david, OrganisationName = "Johannesburg Running Events", ContactNumber = "+27 82 456 7890" };
        var capeTown = new OrganiserProfile { User = michelle, OrganisationName = "Cape Town Cycling Tours", ContactNumber = "+27 83 321 6547" };

        var thomasProfile = new ParticipantProfile { User = thomas, DateOfBirth = new DateOnly(1992, 7, 22) };
        var priyaProfile = new ParticipantProfile { User = priya, DateOfBirth = new DateOnly(1988, 11, 3) };
        var kyleProfile = new ParticipantProfile { User = kyle, DateOfBirth = new DateOnly(1996, 9, 14) };
        var zaneleProfile = new ParticipantProfile { User = zanele, DateOfBirth = new DateOnly(1990, 2, 28) };

        var marathon = NewEvent(joburg, "Joburg City Marathon", "Annual marathon through Johannesburg city centre", new DateTime(2026, 8, 16, 6, 0, 0), "Johannesburg, Gauteng", 42.20m, EventTypes.Run);
        var tenK = NewEvent(joburg, "Soweto 10km Challenge", "Community running event through Soweto", new DateTime(2026, 9, 5, 7, 30, 0), "Soweto, Gauteng", 10.00m, EventTypes.Run);
        var cycle = NewEvent(capeTown, "Cape Town Cycle Challenge", "Coastal cycling event along Chapman's Peak", new DateTime(2026, 10, 10, 7, 0, 0), "Cape Town, Western Cape", 85.00m, EventTypes.Cycle);
        var walk = NewEvent(capeTown, "Winelands Walk", "Scenic walk through Stellenbosch wine farms", new DateTime(2026, 11, 7, 8, 0, 0), "Stellenbosch, Western Cape", 15.00m, EventTypes.Walk);

        var junior = NewCategory(marathon, "Junior", "Runners 16-19 years", 16, 19);
        var open = NewCategory(marathon, "Open", "Runners 20-39 years", 20, 39);
        var masters = NewCategory(marathon, "Masters", "Runners 40-49 years", 40, 49);
        NewCategory(marathon, "Grand Masters", "Runners 50+ years", 50, null);
        NewCategory(tenK, "Under 18", "Runners under 18", 13, 17);
        var senior = NewCategory(tenK, "Senior", "Runners 18-39 years", 18, 39);
        var veteran = NewCategory(tenK, "Veteran", "Runners 40+ years", 40, null);
        NewCategory(cycle, "Under 20", "Cyclists under 20", 15, 19);
        var elite = NewCategory(cycle, "Elite", "Cyclists 20-34 years", 20, 34);
        var cycleMaster = NewCategory(cycle, "Master", "Cyclists 35+ years", 35, null);
        NewCategory(walk, "Under 18", "Walkers under 18", 12, 17);
        var adult = NewCategory(walk, "Adult", "Walkers 18-59 years", 18, 59);
        NewCategory(walk, "Senior", "Walkers 60+ years", 60, null);

        var thomasMarathon = NewEnrolment(thomasProfile, marathon, open, EnrolmentStatuses.Confirmed);
        var thomasTenK = NewEnrolment(thomasProfile, tenK, senior, EnrolmentStatuses.Confirmed);
        var priyaCycle = NewEnrolment(priyaProfile, cycle, cycleMaster, EnrolmentStatuses.Pending);
        var priyaMarathon = NewEnrolment(priyaProfile, marathon, open, EnrolmentStatuses.Confirmed);
        var kyleMarathon = NewEnrolment(kyleProfile, marathon, open, EnrolmentStatuses.Confirmed);
        var kyleWalk = NewEnrolment(kyleProfile, walk, adult, EnrolmentStatuses.Pending);
        var zaneleTenK = NewEnrolment(zaneleProfile, tenK, senior, EnrolmentStatuses.Confirmed);
        var zaneleCycle = NewEnrolment(zaneleProfile, cycle, cycleMaster, EnrolmentStatuses.Confirmed);

        db.Users.AddRange(david, michelle, thomas, priya, kyle, zanele);
        db.Organisers.AddRange(joburg, capeTown);
        db.Participants.AddRange(thomasProfile, priyaProfile, kyleProfile, zaneleProfile);
        db.Events.AddRange(marathon, tenK, cycle, walk);
        db.Enrolments.AddRange(thomasMarathon, thomasTenK, priyaCycle, priyaMarathon, kyleMarathon, kyleWalk, zaneleTenK, zaneleCycle);

        db.Results.AddRange(
            NewResult(thomasMarathon, "03:28:45", 156),
            NewResult(thomasTenK, "00:42:30", 89),
            NewResult(priyaMarathon, "04:15:20", 278),
            NewResult(kyleMarathon, "03:55:10", 234),
            NewResult(zaneleTenK, "00:38:15", 45),
            NewResult(zaneleCycle, "03:10:45", 67));

        await db.SaveChangesAsync();
    }

    private static RaceEvent NewEvent(OrganiserProfile organiser, string name, string description, DateTime date, string location, decimal distance, string type) =>
        new()
        {
            Organiser = organiser,
            Name = name,
            Description = description,
            EventDate = date,
            Location = location,
            Distance = distance,
            EventType = type
        };

    private static RaceCategory NewCategory(RaceEvent raceEvent, string name, string description, int minAge, int? maxAge)
    {
        var category = new RaceCategory { Event = raceEvent, Name = name, Description = description, MinAge = minAge, MaxAge = maxAge };
        raceEvent.Categories.Add(category);
        return category;
    }

    private static Enrolment NewEnrolment(ParticipantProfile participant, RaceEvent raceEvent, RaceCategory category, string status) =>
        new()
        {
            Participant = participant,
            Event = raceEvent,
            Category = category,
            Status = status,
            RegistrationDate = DateTime.Now
        };

    private static RaceResult NewResult(Enrolment enrolment, string finishTime, int position) =>
        new()
        {
            Enrolment = enrolment,
            FinishTime = TimeOnly.Parse(finishTime),
            FinishPosition = position
        };
}
