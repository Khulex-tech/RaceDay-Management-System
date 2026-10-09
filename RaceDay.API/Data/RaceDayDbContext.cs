using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    /*
     * The database context for RaceDayDb.
     * The tables are created by docs/RaceDay_DatabaseScript.sql, so this class only
     * describes the existing tables to Entity Framework (no migrations are used).
     */
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // EventTypes
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.HasKey(t => t.EventTypeId);
            });

            // Events
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId);
                entity.Property(e => e.DistanceKm).HasColumnType("decimal(6,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Organiser)
                      .WithMany(u => u.OrganisedEvents)
                      .HasForeignKey(e => e.OrganiserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.EventType)
                      .WithMany(t => t.Events)
                      .HasForeignKey(e => e.EventTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Categories (deleted together with their event)
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.CategoryId);
                entity.Property(c => c.EntryFee).HasColumnType("decimal(10,2)");
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(c => c.Event)
                      .WithMany(e => e.Categories)
                      .HasForeignKey(c => c.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Enrolments
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.HasKey(en => en.EnrolmentId);
                entity.Property(en => en.EnrolledAt).HasDefaultValueSql("GETDATE()");

                // A Participant can only enrol once per event
                entity.HasIndex(en => new { en.EventId, en.ParticipantId }).IsUnique();

                entity.HasOne(en => en.Participant)
                      .WithMany(u => u.Enrolments)
                      .HasForeignKey(en => en.ParticipantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(en => en.Event)
                      .WithMany(e => e.Enrolments)
                      .HasForeignKey(en => en.EventId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(en => en.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(en => en.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Results (one result per enrolment, deleted with the enrolment)
            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasKey(r => r.ResultId);
                entity.Property(r => r.FinishTime).HasColumnType("time(0)");

                entity.HasOne(r => r.Enrolment)
                      .WithOne(en => en.Result)
                      .HasForeignKey<Result>(r => r.EnrolmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
