using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    /*
     * Seed data added by the InitialCreate migration.
     * The rows match the INSERT statements in docs/RaceDay_DatabaseScript.sql.
     * Every seeded user can log in with the password: RaceDay@2026
     * (the hashes below were generated once with PasswordHelper's PBKDF2 settings).
     */
    public static class SeedData
    {
        // Fixed dates so the migration does not change every time it is generated
        private static readonly DateTime SeedCreatedAt = new DateTime(2026, 1, 15, 9, 0, 0);
        private static readonly DateTime SeedEnrolledAt = new DateTime(2026, 2, 1, 9, 0, 0);

        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventType>().HasData(
                new EventType { EventTypeId = 1, Name = "Run" },
                new EventType { EventTypeId = 2, Name = "Walk" },
                new EventType { EventTypeId = 3, Name = "Cycle" });

            // 2 Organisers and 4 Participants
            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, FirstName = "Thabo", LastName = "Mokoena", Email = "thabo.mokoena@raceday.co.za", PhoneNumber = "0821234567", Role = "Organiser", CreatedAt = SeedCreatedAt,
                    PasswordHash = "phJIUg5kdMetJqiOOLuXeg==:cM/visUxQ7Fk85QIadyVmECBnFckBj9aJaK3pxtIM1I=" },
                new User { UserId = 2, FirstName = "Ayesha", LastName = "Patel", Email = "ayesha.patel@raceday.co.za", PhoneNumber = "0837654321", Role = "Organiser", CreatedAt = SeedCreatedAt,
                    PasswordHash = "lU1ZVyM42Gonr2+7wqoUog==:PnPxctHVy67EYkpAImK3Dg4uttnbMwflgUew+nNd6jM=" },
                new User { UserId = 3, FirstName = "Sipho", LastName = "Ndlovu", Email = "sipho.ndlovu@gmail.com", PhoneNumber = "0725558899", Role = "Participant", CreatedAt = SeedCreatedAt,
                    PasswordHash = "Xw11JNrUbh0bnTvT1encFw==:Oy3IR3eFdGdLM5urNrY0/0KyhRbCUR2nDPWtvYopKCI=" },
                new User { UserId = 4, FirstName = "Lerato", LastName = "Dlamini", Email = "lerato.dlamini@outlook.com", PhoneNumber = "0794443322", Role = "Participant", CreatedAt = SeedCreatedAt,
                    PasswordHash = "fSq5ZvYxJtXrlNDmK3XQeg==:RL7cKqQeSfLIPegWFEfPJoQMHLEabsn7MCpVCS1cl/U=" },
                new User { UserId = 5, FirstName = "Johan", LastName = "van Wyk", Email = "johan.vanwyk@webmail.co.za", PhoneNumber = "0836661177", Role = "Participant", CreatedAt = SeedCreatedAt,
                    PasswordHash = "EnjxIFJO6OcQNrT25KjbHg==:2oHPW913wqyLnSNnrUvUjr2rBUOwGBWdoqyiZE38h68=" },
                new User { UserId = 6, FirstName = "Nomvula", LastName = "Zulu", Email = "nomvula.zulu@gmail.com", PhoneNumber = "0713338844", Role = "Participant", CreatedAt = SeedCreatedAt,
                    PasswordHash = "UJtpXX988YAxVJIzVcqv1Q==:RUnx7r2W9rxWm01vJGHiCrLctxwSRBDLdD869DZrjL4=" });

            // 3 events, one of each event type
            modelBuilder.Entity<Event>().HasData(
                new Event { EventId = 1, OrganiserId = 1, EventTypeId = 1, Name = "Soweto Marathon 2026", CreatedAt = SeedCreatedAt,
                    Description = "A tough marathon through the streets of Soweto, passing the Orlando Towers and Vilakazi Street.",
                    EventDate = new DateTime(2026, 6, 7, 6, 0, 0), Location = "Soweto, Johannesburg", DistanceKm = 42.20m },
                new Event { EventId = 2, OrganiserId = 2, EventTypeId = 3, Name = "Cape Town Cycle Tour 2027", CreatedAt = SeedCreatedAt,
                    Description = "A 109 km cycle race around the Cape Peninsula, including Chapman's Peak and Suikerbossie.",
                    EventDate = new DateTime(2027, 3, 14, 5, 30, 0), Location = "Cape Town CBD", DistanceKm = 109.00m },
                new Event { EventId = 3, OrganiserId = 1, EventTypeId = 2, Name = "Durban Beachfront Charity Walk 2027", CreatedAt = SeedCreatedAt,
                    Description = "A family charity walk along the Golden Mile raising funds for school sports programmes.",
                    EventDate = new DateTime(2027, 5, 2, 7, 0, 0), Location = "uShaka Marine World, Durban", DistanceKm = 10.00m });

            // Categories for each event
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, EventId = 1, Name = "42.2km Marathon", Description = "Full marathon distance for experienced runners.", EntryFee = 450.00m, MinAge = 18, MaxAge = 59, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 2, EventId = 1, Name = "21.1km Half", Description = "Half marathon distance.", EntryFee = 350.00m, MinAge = 16, MaxAge = 59, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 3, EventId = 1, Name = "Under 20", Description = "Age-group category for young runners.", EntryFee = 250.00m, MinAge = 12, MaxAge = 19, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 4, EventId = 1, Name = "Senior", Description = "Age-group category for adult runners.", EntryFee = 400.00m, MinAge = 20, MaxAge = 39, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 5, EventId = 2, Name = "109km Full Route", Description = "Full Cape Town Cycle Tour route.", EntryFee = 780.00m, MinAge = 18, MaxAge = 59, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 6, EventId = 2, Name = "42km Fun Ride", Description = "Shorter scenic cycle route.", EntryFee = 450.00m, MinAge = 16, MaxAge = 59, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 7, EventId = 2, Name = "Veteran", Description = "Age-group category for veteran cyclists.", EntryFee = 600.00m, MinAge = 50, MaxAge = 100, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 8, EventId = 3, Name = "10km Walk", Description = "Full charity walk distance.", EntryFee = 120.00m, MinAge = 12, MaxAge = 59, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 9, EventId = 3, Name = "5km Family Walk", Description = "Shorter family-friendly walk.", EntryFee = 80.00m, MinAge = 6, MaxAge = 59, CreatedAt = SeedCreatedAt },
                new Category { CategoryId = 10, EventId = 3, Name = "Youth", Description = "Age-group category for young walkers.", EntryFee = 60.00m, MinAge = 6, MaxAge = 15, CreatedAt = SeedCreatedAt });

            // Enrolments covering all three statuses
            modelBuilder.Entity<Enrolment>().HasData(
                new Enrolment { EnrolmentId = 1, ParticipantId = 3, EventId = 1, CategoryId = 1, EnrolmentStatus = "Confirmed", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 2, ParticipantId = 4, EventId = 1, CategoryId = 2, EnrolmentStatus = "Confirmed", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 3, ParticipantId = 5, EventId = 1, CategoryId = 1, EnrolmentStatus = "Confirmed", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 4, ParticipantId = 5, EventId = 2, CategoryId = 5, EnrolmentStatus = "Confirmed", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 5, ParticipantId = 3, EventId = 2, CategoryId = 6, EnrolmentStatus = "Pending", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 6, ParticipantId = 6, EventId = 3, CategoryId = 10, EnrolmentStatus = "Confirmed", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 7, ParticipantId = 4, EventId = 3, CategoryId = 8, EnrolmentStatus = "Pending", EnrolledAt = SeedEnrolledAt },
                new Enrolment { EnrolmentId = 8, ParticipantId = 3, EventId = 3, CategoryId = 9, EnrolmentStatus = "Cancelled", EnrolledAt = SeedEnrolledAt });

            // Results for the event that has already taken place (Soweto Marathon)
            modelBuilder.Entity<Result>().HasData(
                new Result { ResultId = 1, EnrolmentId = 1, FinishTime = new TimeSpan(3, 41, 18), FinishingPosition = 47, PublishedAt = new DateTime(2026, 6, 7, 10, 22, 0) },
                new Result { ResultId = 2, EnrolmentId = 2, FinishTime = new TimeSpan(1, 58, 4), FinishingPosition = 129, PublishedAt = new DateTime(2026, 6, 7, 9, 58, 0) },
                new Result { ResultId = 3, EnrolmentId = 3, FinishTime = new TimeSpan(4, 52, 37), FinishingPosition = 208, PublishedAt = new DateTime(2026, 6, 7, 11, 45, 0) });
        }
    }
}
