CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO
-- 1. USERS TABLE
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 2. ORGANISERS TABLE
CREATE TABLE Organisers (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT UNIQUE NOT NULL,
    OrganisationName NVARCHAR(100) NOT NULL,
    ContactNumber NVARCHAR(20) NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
GO

-- 3. PARTICIPANTS TABLE
CREATE TABLE Participants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT UNIQUE NOT NULL,
    DateOfBirth DATE NOT NULL,
    ProfilePictureURL NVARCHAR(500) NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
GO

-- 4. EVENTS TABLE
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Distance DECIMAL(5,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    BannerImageURL NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrganiserID) REFERENCES Organisers(OrganiserID)
);
GO
-- 5. CATEGORIES TABLE
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    MinAge INT NULL,
    MaxAge INT NULL,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE
);
GO

-- 6. ENROLMENTS TABLE
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    RegistrationDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, EventID),
    FOREIGN KEY (ParticipantID) REFERENCES Participants(ParticipantID),
    FOREIGN KEY (EventID) REFERENCES Events(EventID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
GO

-- 7. RESULTS TABLE
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT UNIQUE NOT NULL,
    FinishTime TIME NOT NULL,
    FinishPosition INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Published' CHECK (Status IN ('Published', 'Pending', 'Disqualified')),
    FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);
GO

-- SEED DATA 
-- Users
INSERT INTO Users (Email, PasswordHash, FullName, Role) VALUES
('events@joburgrunning.co.za', '$2a$11$rsRYYKaDzIrDahYDXoQ/1OvABj8Nn1zvq8mWpmnhxSMPqYm694x8y', 'David Mokoena', 'Organiser'),
('info@capetowncycling.com', '$2a$11$Cd5Ae7KscN99szm8rlbaVuS04c8BFtNXBu5wdnXLYB7Rh2FMreTV2', 'Michelle Williams', 'Organiser'),
('thomas.n@email.com', '$2a$11$b7CYJhU99wRwgiSTKZDJgeK5e88yo7ezeJSwlK0YFCbyvSXhQj5SG', 'Thomas Nkosi', 'Participant'),
('priya.s@email.com', '$2a$11$Nx9nhL1/qCvEvPvoNhzKaOjAdE1a7hbiLmXTgsqQ1nrsri52lDII2', 'Priya Singh', 'Participant'),
('kyle.b@email.com', '$2a$11$oIobZJYvUyYYmCc78fXojuhvR1qPf1XI08TniQzprKbMI38ZCyQvG', 'Kyle Botha', 'Participant'),
('zanele.m@email.com', '$2a$11$bVwLMksJCge73nE7ZdcXX.V4THRstOOz7F49XCG6kHrutNmcYBBQm', 'Zanele Mthembu', 'Participant');

-- Organisers
INSERT INTO Organisers (UserID, OrganisationName, ContactNumber) VALUES
(1, 'Johannesburg Running Events', '+27 82 456 7890'),
(2, 'Cape Town Cycling Tours', '+27 83 321 6547');

-- Participants
INSERT INTO Participants (UserID, DateOfBirth, ProfilePictureURL) VALUES
(3, '1992-07-22', NULL),
(4, '1988-11-03', NULL),
(5, '1996-09-14', NULL),
(6, '1990-02-28', NULL);

-- Events
INSERT INTO Events (OrganiserID, Name, Description, EventDate, Location, Distance, EventType) VALUES
(1, 'Joburg City Marathon', 'Annual marathon through Johannesburg city centre', '2026-08-16 06:00:00', 'Johannesburg, Gauteng', 42.20, 'Run'),
(1, 'Soweto 10km Challenge', 'Community running event through Soweto', '2026-09-05 07:30:00', 'Soweto, Gauteng', 10.00, 'Run'),
(2, 'Cape Town Cycle Challenge', 'Coastal cycling event along Chapman''s Peak', '2026-10-10 07:00:00', 'Cape Town, Western Cape', 85.00, 'Cycle'),
(2, 'Winelands Walk', 'Scenic walk through Stellenbosch wine farms', '2026-11-07 08:00:00', 'Stellenbosch, Western Cape', 15.00, 'Walk');

-- Categories
INSERT INTO Categories (EventID, Name, Description, MinAge, MaxAge) VALUES
(1, 'Junior', 'Runners 16-19 years', 16, 19),
(1, 'Open', 'Runners 20-39 years', 20, 39),
(1, 'Masters', 'Runners 40-49 years', 40, 49),
(1, 'Grand Masters', 'Runners 50+ years', 50, NULL),
(2, 'Under 18', 'Runners under 18', 13, 17),
(2, 'Senior', 'Runners 18-39 years', 18, 39),
(2, 'Veteran', 'Runners 40+ years', 40, NULL),
(3, 'Under 20', 'Cyclists under 20', 15, 19),
(3, 'Elite', 'Cyclists 20-34 years', 20, 34),
(3, 'Master', 'Cyclists 35+ years', 35, NULL),
(4, 'Under 18', 'Walkers under 18', 12, 17),
(4, 'Adult', 'Walkers 18-59 years', 18, 59),
(4, 'Senior', 'Walkers 60+ years', 60, NULL);

-- Enrolments
INSERT INTO Enrolments (ParticipantID, EventID, CategoryID, Status) VALUES
(1, 1, 2, 'Confirmed'),
(1, 2, 6, 'Confirmed'),
(2, 3, 10, 'Pending'),
(2, 1, 2, 'Confirmed'),
(3, 1, 2, 'Confirmed'),
(3, 4, 12, 'Pending'),
(4, 2, 6, 'Confirmed'),
(4, 3, 10, 'Confirmed');

-- Results
INSERT INTO Results (EnrolmentID, FinishTime, FinishPosition) VALUES
(1, '03:28:45', 156),
(2, '00:42:30', 89),
(4, '04:15:20', 278),
(5, '03:55:10', 234),
(7, '00:38:15', 45),
(8, '03:10:45', 67);
GO
