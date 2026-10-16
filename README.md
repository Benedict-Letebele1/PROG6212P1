# RaceDay - PROG6212 Programming 2B

| | |
|---|---|
| Module | PROG6212 - Programming 2B |
| Student | Benedict Letebele |
| Student Number | ST10483847 |
| Year | 2026 |

[![Planning document check](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/docs-check.yml/badge.svg)](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/docs-check.yml)

## Branches

| Branch | Contents |
|---|---|
| `main` | Part 1: planning documents (ERD, endpoint plan, SQL script) and the planning document check |
| `part-2` | Part 2: ASP.NET Core Web API, Entity Framework Core code-first, session authentication, Swagger, unit tests and the build workflow |

## System Overview

RaceDay is an event management platform for South Africa's road running, walking and cycling community. Event organisers create and manage events, categories and results. Participants browse events, enrol in a category and track their own results.

## Roles

**Organiser**
- Creates, edits and deletes their own events and uploads banner images
- Manages the categories of their events
- Views every enrolment for their events and changes enrolment status
- Captures finish times and finishing positions for enrolled participants

**Participant**
- Registers with a date of birth and browses events (public)
- Enrols in an event by choosing a category that fits their age on the event date
- Views their own enrolments and their own results

Both roles manage their own profile. Anonymous visitors can browse events and categories.

## Planning Documents

| Document | Location | What it shows |
|---|---|---|
| ERD | [docs/erd.png](docs/erd.png) | All seven entities with keys, constraints and cardinality. Drawn from the SQL script. |
| API endpoint plan | [docs/endpoint-plan.pdf](docs/endpoint-plan.pdf) | Every endpoint with method, route, access, request body and expected responses. |
| SQL script | [docs/RaceDay-Schema.sql](docs/RaceDay-Schema.sql) | Creates `RaceDayDB`, all tables and constraints, and the seed data. |

### ERD decisions

- **Users** holds login details for everyone. **Organisers** and **Participants** each hold one row per user (1:0..1), so role-specific fields stay out of the shared table.
- **Events** belong to one **Organiser** (1:N).
- **Categories** belong to one **Event** (1:N). Age bounds (MinAge, MaxAge) are optional so "50+" can be open-ended.
- **Enrolments** is the link table for the many-to-many relationship between Participants and Events. A unique constraint on (ParticipantID, EventID) stops a participant entering the same event twice.
- **Results** has at most one row per enrolment (1:0..1), enforced by a unique EnrolmentID.

### Endpoint plan decisions

- Access is split into three levels: public, logged-in user, and a specific role (Organiser or Participant). Organiser rights apply only to events the Organiser owns.
- Enrolment status (`Pending`, `Confirmed`, `Cancelled`) is changed by the Organiser that owns the event.
- Results are written against an enrolment, so a result always belongs to one participant in one event.

### Seed data

The script seeds six users (two organisers, four participants), two organisers, four events, 13 categories, eight enrolments and six results. Every enrolment falls inside its category's age range on the event date.

Corrections made while checking the plan against the database:

- The placeholder password hashes were replaced with real BCrypt hashes of `Password123!`, so the seeded accounts can log in once the API is built.
- Three enrolments were outside their category's age range and were moved into a matching category:
  - Priya Singh, Cape Town Cycle Challenge: Elite (20-34) changed to Master (35+).
  - Priya Singh, Joburg City Marathon: Masters (40-49) changed to Open (20-39).
  - Zanele Mthembu, Soweto 10km Challenge: Veteran (40+) changed to Senior (18-39).

## Setup Instructions

1. Clone the repository:
   ```
   git clone https://github.com/Benedict-Letebele1/PROG6212P1.git
   cd PROG6212P1
   ```
2. Open SQL Server Management Studio and connect to your local SQL Server instance (LocalDB works).
3. Open `docs/RaceDay-Schema.sql` and run it (F5). It creates `RaceDayDB` from scratch, so run it on an instance where that database does not exist yet, or drop it first.
4. Refresh the Databases node and check the seven tables under `RaceDayDB > Tables`. Run `SELECT COUNT(*) FROM Events;` and expect 4.
5. Check the **Actions** tab on GitHub for a green build.

To use the Part 2 API, switch to the `part-2` branch and follow the setup in its README. The API creates its own schema with migrations, so do not run the SQL script against the same database the API uses.

## Video Presentation

Link: [YouTube link - to be added before submission]

What the video covers:
- ERD walkthrough: each entity, its attributes, keys and relationships
- Endpoint plan explanation: every endpoint and its role-based access
- SQL script demonstration: running the script live in SSMS to show the tables and seed data

Duration: about 10 minutes. Audio is my own voice with no AI-generated voice.

## CI/CD

The workflow `.github/workflows/docs-check.yml` runs on every push to `main`. It checks that the three planning documents exist and that the SQL script defines all seven tables.

## AI Use Disclosure

AI tools were used to help check the SQL seed data against the endpoint plan and to draft the layout of this README. The ERD was drawn from the SQL script, and the endpoint plan is the approved plan submitted with this work.

## Student Declaration

I declare that this work is my own original work and all sources have been properly referenced.

Name: Benedict Letebele
Student Number: ST10483847
Date: 2026/09/03
