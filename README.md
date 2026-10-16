# RaceDay - PROG6212 Programming 2B

| | |
|---|---|
| Module | PROG6212 - Programming 2B |
| Student | Benedict Letebele |
| Student Number | ST10483847 |
| Year | 2026 |

[![Planning document check](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/docs-check.yml/badge.svg)](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/docs-check.yml)
[![RaceDay API - Build and Test](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/api-build-test.yml/badge.svg?branch=part-2)](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/api-build-test.yml)

## Branches

| Branch | Contents |
|---|---|
| `main` | Part 1: planning documents (ERD, endpoint plan, SQL script) and the planning document check |
| `part-2` | Part 2: the ASP.NET Core Web API, with EF Core code-first, session login, Swagger and unit tests. This branch also holds the Part 1 documents. |

## System Overview

RaceDay is an event management platform for South Africa's road running, walking and cycling community. Organisers create and manage events, their categories and results. Participants browse events, enrol in a category and track their own results.

## Roles

**Organiser**
- Creates, edits and deletes their own events, and uploads banner images
- Adds, edits and deletes the categories of their events
- Views every enrolment in their events and sets enrolment status
- Captures finish times and positions for enrolled participants

**Participant**
- Registers with a date of birth and enrols in events by choosing a category
- Views their own enrolments and their own results
- Updates their own profile

Anonymous visitors can browse events and categories. Each protected endpoint checks the role stored in the server-side session, and returns 401 when nobody is logged in and 403 when the wrong role calls it.

## Setup and Run (Visual Studio)

Requirements: Visual Studio with the ASP.NET and web development workload and .NET 10 support, the .NET 10 SDK, and SQL Server LocalDB (installed with Visual Studio).

1. Clone the repository and switch to the Part 2 branch:
   ```
   git clone https://github.com/Benedict-Letebele1/PROG6212P1.git
   cd PROG6212P1
   git checkout part-2
   ```
2. Open `RaceDay.sln` in Visual Studio.
3. Set `RaceDayApi` as the startup project and press **F5**. Swagger opens at `http://localhost:5088/swagger`.
4. The first run creates `RaceDayDB` in LocalDB, applies the migrations and loads the demo data.

The connection string is in `RaceDayApi/appsettings.json`. To use another SQL Server, set it without editing the file:

```
# PowerShell
$env:ConnectionStrings__RaceDayDb = "Server=.;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project RaceDayApi
```

```
# bash
export ConnectionStrings__RaceDayDb="Server=.;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project RaceDayApi
```

### Setup and run from the command line

```
dotnet tool restore
dotnet run --project RaceDayApi --urls http://localhost:5088
```

### Demo accounts

Every demo account uses the password `Password123!`.

| Role | Email |
|---|---|
| Organiser (Johannesburg Running Events) | events@joburgrunning.co.za |
| Organiser (Cape Town Cycling Tours) | info@capetowncycling.com |
| Participant | thomas.n@email.com, priya.s@email.com, kyle.b@email.com, zanele.m@email.com |

In Swagger, call `POST /api/auth/login` first. The session cookie is then sent with each request.

### Running the tests

```
dotnet test RaceDay.sln
```

The tests use an in-memory SQLite database, so they run without SQL Server or LocalDB on any machine.

## Endpoints

Every endpoint is listed in Swagger with its purpose, request body and responses. The table below is the implemented list, and it matches the approved plan in `docs/endpoint-plan.pdf` except for the documented deviations.

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | /api/auth/register | Public | Create an Organiser or Participant account |
| POST | /api/auth/login | Public | Log in and start a session |
| POST | /api/auth/logout | Logged in | End the session |
| GET | /api/users/profile | Logged in | View own profile |
| PUT | /api/users/profile | Logged in | Update own profile |
| GET | /api/events | Public | List events, filtered by type, date and location |
| GET | /api/events/{id} | Public | One event with its categories |
| POST | /api/events | Organiser | Create an event |
| PUT | /api/events/{id} | Organiser (owner) | Update an event |
| DELETE | /api/events/{id} | Organiser (owner) | Delete an event |
| POST | /api/events/{eventId}/upload-banner | Organiser (owner) | Upload a banner image |
| GET | /api/events/{eventId}/categories | Public | List the categories of an event |
| POST | /api/events/{eventId}/categories | Organiser (owner) | Add a category |
| PUT | /api/categories/{id} | Organiser (owner) | Update a category |
| DELETE | /api/categories/{id} | Organiser (owner) | Delete a category |
| GET | /api/enrolments | Logged in | Organiser: enrolments in own events. Participant: own enrolments |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) | Enrolments for one event |
| POST | /api/events/{eventId}/enrol | Participant | Enrol with a category of the event |
| PUT | /api/enrolments/{id}/status | Organiser (owner) | Set status to Pending, Confirmed or Cancelled |
| GET | /api/results/me | Participant | Own results |
| GET | /api/events/{eventId}/results | Organiser (owner) | Results for one event |
| POST | /api/events/{eventId}/results | Organiser (owner) | Record the finish time and position of an enrolment |
| PUT | /api/results/{id} | Organiser (owner) | Change a recorded result |

### Deviations from the Part 1 plan

These are deliberate. Each one is listed so the marker can see it.

1. **Organiser registration also takes `contactNumber`.** The plan lists only organisation name for organisers, but `Organisers.ContactNumber` is NOT NULL in the schema.
2. **Enrolment checks the age range.** `POST /api/events/{eventId}/enrol` returns 400 when the participant's age on the event date is outside the category's range. The plan does not state this rule, but the category ages are part of the plan's seed data.
3. **Deleting an event or a category that has enrolments returns 409 Conflict.** The plan lists only 204, 403 and 404 for delete. The 409 stops enrolment records from being lost.
4. **Duplicate enrolments and duplicate results return 400.** Both use 400 so the status codes stay within the ones the plan lists.
5. **`GET /api/events/{id}` includes the categories.** The plan lists event data only. Including them lets the Part 3 event page show its categories in one call.
6. **Login returns the user's identity and role in the body.** The plan says "User info". The session cookie is `RaceDayApi.Session`.
7. **Banner images are stored on the local disk** under `wwwroot/uploads/banners`. Part 3 moves this to Azure Blob Storage behind the same `IBannerStorage` interface.
8. **Profile pictures are not uploaded yet.** The plan has no endpoint for them. The `ProfilePictureURL` column exists, and the upload belongs to Part 3.
9. **The API builds its own schema from migrations.** Run `docs/RaceDay-Schema.sql` only against a separate database, not the one the API uses.

## Design Notes

- **Session login.** Login stores the user ID and role in ASP.NET Core server-side session state. The cookie holds only the session ID. `[RequireSession]` reads the role from the session, never from the request body.
- **Passwords.** BCrypt hashes with a work factor of 11. The plain password is never stored.
- **Schema.** Entity Framework Core code-first. The model maps onto the same table, column, key, unique, check and delete rules as `docs/RaceDay-Schema.sql`. `dotnet ef migrations has-pending-model-changes` runs in CI so the migration cannot drift from the model.
- **Layers.** Controllers handle HTTP. `EventRules` holds validation. `OrganiserAccess` holds the ownership lookup. `Mappers` builds the response bodies. `DemoDataSeeder` loads the demo data.
- **Ownership.** An organiser can change an event, its categories, its enrolments and its results only when they created that event. Any other organiser gets 403.
- **Time and dates.** Event dates are stored in `datetime`. Date of birth is `date`, and finish time is `time`.

## Migrations

`RaceDayApi/Data/Migrations/` holds the initial migration. To add a change:

```
dotnet ef migrations add <Name> --project RaceDayApi --startup-project RaceDayApi
```

## Video Presentation

Link: [YouTube link - to be added before submission]

What the video covers:
- The running API in Swagger
- Code structure, endpoints, role checks and sessions
- The unit tests and the GitHub Actions run

## CI/CD

The workflow `.github/workflows/api-build-test.yml` runs on every push. It restores the tools and packages, builds the solution, runs the unit tests and checks that the migrations match the model.

Screenshot of a successful build: to be added after the first green run on `part-2`.

## AI Use Disclosure

AI tools (Claude) were used to help draft the code, the tests and this README. The endpoint plan, the design choices and the seed-data corrections come from my own Part 1 work, and I checked and ran the code myself.

## Student Declaration

I declare that this work is my own original work and all sources have been properly referenced.

Name: Benedict Letebele
Student Number: ST10483847
Date: 2026/09/03
