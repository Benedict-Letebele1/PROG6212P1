namespace RaceDayApi.Domain;

/// <summary>The finish time and position for one enrolment. Each enrolment has at most one result.</summary>
public class RaceResult
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    public TimeOnly FinishTime { get; set; }

    public int FinishPosition { get; set; }

    public string Status { get; set; } = ResultStatuses.Published;

    public Enrolment Enrolment { get; set; } = null!;
}
