using RaceDayApi.Contracts;
using RaceDayApi.Domain;

namespace RaceDayApi.Services;

/// <summary>
/// Converts entities into response bodies. The callers load the navigation properties these need
/// (organiser, categories, event, category, participant and user) before mapping.
/// </summary>
public static class Mappers
{
    public static EventDetailResponse ToEventDetail(RaceEvent raceEvent) =>
        new(
            raceEvent.EventID,
            raceEvent.Name,
            raceEvent.Description,
            raceEvent.EventDate,
            raceEvent.Location,
            raceEvent.Distance,
            raceEvent.EventType,
            raceEvent.BannerImageURL,
            raceEvent.Organiser.OrganisationName,
            raceEvent.Categories.OrderBy(c => c.CategoryID).Select(ToCategory).ToList());

    public static CategoryResponse ToCategory(RaceCategory category) =>
        new(category.CategoryID, category.EventID, category.Name, category.Description, category.MinAge, category.MaxAge);

    public static EnrolmentResponse ToEnrolment(Enrolment enrolment) =>
        new(
            enrolment.EnrolmentID,
            enrolment.EventID,
            enrolment.Event.Name,
            enrolment.Event.EventDate,
            enrolment.CategoryID,
            enrolment.Category.Name,
            enrolment.Participant.User.FullName,
            enrolment.Status,
            enrolment.RegistrationDate);

    public static ResultResponse ToResult(RaceResult result) =>
        new(
            result.ResultID,
            result.EnrolmentID,
            result.Enrolment.EventID,
            result.Enrolment.Event.Name,
            result.Enrolment.Event.EventDate,
            result.Enrolment.Category.Name,
            result.Enrolment.Participant.User.FullName,
            result.FinishTime.ToString("HH:mm:ss"),
            result.FinishPosition,
            result.Status);
}
