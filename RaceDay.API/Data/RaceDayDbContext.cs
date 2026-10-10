using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    /*
     * The database context for RaceDayDb (EF Core Code-First).
     * The table, column and constraint setup below matches docs/RaceDay_DatabaseScript.sql
     * so the database created by the migrations is the same as the Part 1 ERD.
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
                entity.ToTable("Users", t =>
                    t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));

                entity.HasKey(u => u.UserId);
                entity.Property(u => u.FirstName).HasMaxLength(50);
                entity.Property(u => u.LastName).HasMaxLength(50);
                entity.Property(u => u.Email).HasMaxLength(255);
                entity.Property(u => u.PasswordHash).HasMaxLength(255);
                entity.Property(u => u.PhoneNumber).HasMaxLength(20);
                entity.Property(u => u.Role).HasMaxLength(20).IsUnicode(false);
                entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(u => u.Email).IsUnique();
            });

            // EventTypes (Run, Walk, Cycle)
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.HasKey(t => t.EventTypeId);
                entity.Property(t => t.Name).HasMaxLength(10).IsUnicode(false);
                entity.HasIndex(t => t.Name).IsUnique();
            });

            // Events
            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events", t =>
                    t.HasCheckConstraint("CK_Events_DistanceKm", "[DistanceKm] > 0"));

                entity.HasKey(e => e.EventId);
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.DistanceKm).HasColumnType("decimal(6,2)");
                entity.Property(e => e.BannerImageUrl).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Organiser)
                      .WithMany(u => u.OrganisedEvents)
                      .HasForeignKey(e => e.OrganiserId)
                      .HasConstraintName("FK_Events_Organiser")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.EventType)
                      .WithMany(t => t.Events)
                      .HasForeignKey(e => e.EventTypeId)
                      .HasConstraintName("FK_Events_EventType")
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Categories (deleted together with their event)
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories", t =>
                {
                    t.HasCheckConstraint("CK_Categories_EntryFee", "[EntryFee] >= 0");
                    t.HasCheckConstraint("CK_Categories_AgeRange", "[MaxAge] >= [MinAge]");
                });

                entity.HasKey(c => c.CategoryId);
                entity.Property(c => c.Name).HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(250);
                entity.Property(c => c.EntryFee).HasColumnType("decimal(10,2)");
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(c => c.Event)
                      .WithMany(e => e.Categories)
                      .HasForeignKey(c => c.EventId)
                      .HasConstraintName("FK_Categories_Event")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Enrolments (links a Participant, an Event and a Category)
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.ToTable("Enrolments", t =>
                    t.HasCheckConstraint("CK_Enrolments_Status",
                        "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled')"));

                entity.HasKey(en => en.EnrolmentId);
                entity.Property(en => en.EnrolmentStatus).HasMaxLength(20).IsUnicode(false);
                entity.Property(en => en.EnrolledAt).HasDefaultValueSql("GETDATE()");

                // A Participant can only enrol once per event
                entity.HasIndex(en => new { en.EventId, en.ParticipantId })
                      .IsUnique()
                      .HasDatabaseName("UQ_Enrolments_Event_Participant");

                entity.HasOne(en => en.Participant)
                      .WithMany(u => u.Enrolments)
                      .HasForeignKey(en => en.ParticipantId)
                      .HasConstraintName("FK_Enrolments_Participant")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(en => en.Event)
                      .WithMany(e => e.Enrolments)
                      .HasForeignKey(en => en.EventId)
                      .HasConstraintName("FK_Enrolments_Event")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(en => en.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(en => en.CategoryId)
                      .HasConstraintName("FK_Enrolments_Category")
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Results (one result per enrolment, deleted with the enrolment)
            modelBuilder.Entity<Result>(entity =>
            {
                entity.ToTable("Results", t =>
                    t.HasCheckConstraint("CK_Results_FinishingPosition", "[FinishingPosition] > 0"));

                entity.HasKey(r => r.ResultId);
                entity.Property(r => r.FinishTime).HasColumnType("time(0)");

                entity.HasOne(r => r.Enrolment)
                      .WithOne(en => en.Result)
                      .HasForeignKey<Result>(r => r.EnrolmentId)
                      .HasConstraintName("FK_Results_Enrolment")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Sample data (same rows as the SQL script)
            SeedData.Seed(modelBuilder);
        }
    }
}
