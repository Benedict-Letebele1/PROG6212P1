namespace RaceDayApi.Domain;

/// <summary>Role names stored in Users.Role. They match the CHECK constraint in the SQL script.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Event types stored in Events.EventType.</summary>
public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = [Run, Walk, Cycle];
}

/// <summary>Enrolment statuses stored in Enrolments.Status.</summary>
public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Confirmed, Cancelled];
}

/// <summary>Result statuses stored in Results.Status.</summary>
public static class ResultStatuses
{
    public const string Published = "Published";
    public const string Pending = "Pending";
    public const string Disqualified = "Disqualified";
}
