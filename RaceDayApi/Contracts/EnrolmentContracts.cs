namespace RaceDayApi.Contracts;

/// <summary>Request body for POST /api/events/{eventId}/enrol. The category must belong to that event.</summary>
public record EnrolRequest(int CategoryId);

/// <summary>Request body for PUT /api/enrolments/{id}/status. Status is Pending, Confirmed or Cancelled.</summary>
public record StatusRequest(string Status);

/// <summary>One enrolment, with the event, category and participant names filled in.</summary>
public record EnrolmentResponse(
    int EnrolmentId,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    string ParticipantName,
    string Status,
    DateTime RegistrationDate);
