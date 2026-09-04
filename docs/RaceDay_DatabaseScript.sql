-- RaceDay Database Script
-- This script creates the RaceDay database, all six tables and the sample data

-- Creating the database
USE master;
GO

IF DB_ID('RaceDayDb') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDb;
END
GO

CREATE DATABASE RaceDayDb;
GO

USE RaceDayDb;
GO

-- Creating the Users table
CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Role VARCHAR(20) NOT NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

-- Creating the EventTypes table
CREATE TABLE EventTypes
(
    EventTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(10) NOT NULL UNIQUE
);
GO

-- Creating the Events table
CREATE TABLE Events
(
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventTypeId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    BannerImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Events_EventType FOREIGN KEY (EventTypeId) REFERENCES EventTypes(EventTypeId),
    CONSTRAINT CK_Events_DistanceKm CHECK (DistanceKm > 0)
);
GO

-- Creating the Categories table
CREATE TABLE Categories
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(250) NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    MinAge INT NOT NULL,
    MaxAge INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT CK_Categories_AgeRange CHECK (MaxAge >= MinAge)
);
GO

-- Creating the Enrolments table, which links a Participant, an Event and a Category
CREATE TABLE Enrolments
(
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentStatus VARCHAR(20) NOT NULL,
    EnrolledAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (EnrolmentStatus IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT UQ_Enrolments_Event_Participant UNIQUE (EventId, ParticipantId)
);
GO

-- Creating the Results table
CREATE TABLE Results
(
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTime TIME(0) NOT NULL,
    FinishingPosition INT NOT NULL,
    PublishedAt DATETIME2 NULL,
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT CK_Results_FinishingPosition CHECK (FinishingPosition > 0)
);
GO

-- Seeding the lookup tables
INSERT INTO EventTypes (Name) VALUES
('Run'),
('Walk'),
('Cycle');
GO

-- Seeding 2 Organisers and 4 Participants
INSERT INTO Users (FirstName, LastName, Email, PasswordHash, PhoneNumber, Role) VALUES
    ('Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_password_sample_01', '0821234567', 'Organiser'),
    ('Ayesha', 'Patel', 'ayesha.patel@raceday.co.za', 'hashed_password_sample_02', '0837654321', 'Organiser'),
    ('Sipho', 'Ndlovu', 'sipho.ndlovu@gmail.com', 'hashed_password_sample_03', '0725558899', 'Participant'),
    ('Lerato', 'Dlamini', 'lerato.dlamini@outlook.com', 'hashed_password_sample_04', '0794443322', 'Participant'),
    ('Johan', 'van Wyk', 'johan.vanwyk@webmail.co.za', 'hashed_password_sample_05', '0836661177', 'Participant'),
    ('Nomvula', 'Zulu', 'nomvula.zulu@gmail.com', 'hashed_password_sample_06', '0713338844', 'Participant');
GO

-- Seeding 3 events, one of each event type
INSERT INTO Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm) VALUES
    (1, 1, 'Soweto Marathon 2026', 'A tough marathon through the streets of Soweto, passing the Orlando Towers and Vilakazi Street.', '2026-06-07 06:00', 'Soweto, Johannesburg', 42.20),
    (2, 3, 'Cape Town Cycle Tour 2027', 'A 109 km cycle race around the Cape Peninsula, including Chapman''s Peak and Suikerbossie.', '2027-03-14 05:30', 'Cape Town CBD', 109.00),
    (1, 2, 'Durban Beachfront Charity Walk 2027', 'A family charity walk along the Golden Mile raising funds for school sports programmes.', '2027-05-02 07:00', 'uShaka Marine World, Durban', 10.00);
GO

-- Seeding the categories for each event
INSERT INTO Categories (EventId, Name, Description, EntryFee, MinAge, MaxAge) VALUES
    (1, '42.2km Marathon', 'Full marathon distance for experienced runners.', 450.00, 18, 59),
    (1, '21.1km Half', 'Half marathon distance.', 350.00, 16, 59),
    (1, 'Under 20', 'Age-group category for young runners.', 250.00, 12, 19),
    (1, 'Senior', 'Age-group category for adult runners.', 400.00, 20, 39),
    (2, '109km Full Route', 'Full Cape Town Cycle Tour route.', 780.00, 18, 59),
    (2, '42km Fun Ride', 'Shorter scenic cycle route.', 450.00, 16, 59),
    (2, 'Veteran', 'Age-group category for veteran cyclists.', 600.00, 50, 100),
    (3, '10km Walk', 'Full charity walk distance.', 120.00, 12, 59),
    (3, '5km Family Walk', 'Shorter family-friendly walk.', 80.00, 6, 59),
    (3, 'Youth', 'Age-group category for young walkers.', 60.00, 6, 15);
GO

-- Seeding the enrolments, covering all three statuses
INSERT INTO Enrolments (ParticipantId, EventId, CategoryId, EnrolmentStatus) VALUES
    (3, 1, 1, 'Confirmed'),
    (4, 1, 2, 'Confirmed'),
    (5, 1, 1, 'Confirmed'),
    (5, 2, 5, 'Confirmed'),
    (3, 2, 6, 'Pending'),
    (6, 3, 10, 'Confirmed'),
    (4, 3, 8, 'Pending'),
    (3, 3, 9, 'Cancelled');
GO

-- Seeding the results for the event that has already taken place
INSERT INTO Results (EnrolmentId, FinishTime, FinishingPosition, PublishedAt) VALUES
    (1, '03:41:18', 47, '2026-06-07 10:22:00'),
    (2, '01:58:04', 129, '2026-06-07 09:58:00'),
    (3, '04:52:37', 208, '2026-06-07 11:45:00');
GO

-- Checking the number of rows in each table
SELECT 'Users' AS TableName, COUNT(*) AS TotalRows FROM Users
UNION ALL SELECT 'EventTypes', COUNT(*) FROM EventTypes
UNION ALL SELECT 'Events', COUNT(*) FROM Events
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Enrolments', COUNT(*) FROM Enrolments
UNION ALL SELECT 'Results', COUNT(*) FROM Results;
GO

-- Checking the enrolments for each event
SELECT e.Name AS EventName,
       u.FirstName + ' ' + u.LastName AS Participant,
       c.Name AS CategoryName,
       en.EnrolmentStatus
FROM Enrolments en
INNER JOIN Events e ON e.EventId = en.EventId
INNER JOIN Users u ON u.UserId = en.ParticipantId
INNER JOIN Categories c ON c.CategoryId = en.CategoryId
ORDER BY e.EventDate, u.LastName;
GO

-- Checking the captured results
SELECT u.FirstName + ' ' + u.LastName AS Participant,
       e.Name AS EventName,
       c.Name AS CategoryName,
       r.FinishTime,
       r.FinishingPosition,
       r.PublishedAt
FROM Results r
INNER JOIN Enrolments en ON en.EnrolmentId = r.EnrolmentId
INNER JOIN Users u ON u.UserId = en.ParticipantId
INNER JOIN Events e ON e.EventId = en.EventId
INNER JOIN Categories c ON c.CategoryId = en.CategoryId
ORDER BY r.FinishingPosition;
GO

PRINT 'RaceDayDb created and seeded successfully.';
GO
