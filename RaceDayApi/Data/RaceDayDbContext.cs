using Microsoft.EntityFrameworkCore;
using RaceDayApi.Domain;

namespace RaceDayApi.Data;

/// <summary>
/// Maps the RaceDay entities onto the tables in docs/RaceDay-Schema.sql. Table, column, key, unique,
/// check and delete rules follow the script so the migration produces the same schema.
/// </summary>
public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<OrganiserProfile> Organisers => Set<OrganiserProfile>();

    public DbSet<ParticipantProfile> Participants => Set<ParticipantProfile>();

    public DbSet<RaceEvent> Events => Set<RaceEvent>();

    public DbSet<RaceCategory> Categories => Set<RaceCategory>();

    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            entity.HasKey(u => u.UserID);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
            entity.Property(u => u.FullName).HasMaxLength(100);
            entity.Property(u => u.Role).HasMaxLength(20);
            entity.Property(u => u.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<OrganiserProfile>(entity =>
        {
            entity.ToTable("Organisers");
            entity.HasKey(o => o.OrganiserID);
            entity.HasIndex(o => o.UserID).IsUnique();
            entity.Property(o => o.OrganisationName).HasMaxLength(100);
            entity.Property(o => o.ContactNumber).HasMaxLength(20);
            entity.HasOne(o => o.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<OrganiserProfile>(o => o.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParticipantProfile>(entity =>
        {
            entity.ToTable("Participants");
            entity.HasKey(p => p.ParticipantID);
            entity.HasIndex(p => p.UserID).IsUnique();
            entity.Property(p => p.DateOfBirth).HasColumnType("date");
            entity.Property(p => p.ProfilePictureURL).HasMaxLength(500);
            entity.HasOne(p => p.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<ParticipantProfile>(p => p.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RaceEvent>(entity =>
        {
            entity.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
            entity.HasKey(e => e.EventID);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EventDate).HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Distance).HasPrecision(5, 2);
            entity.Property(e => e.EventType).HasMaxLength(20);
            entity.Property(e => e.BannerImageURL).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RaceCategory>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryID);
            entity.Property(c => c.Name).HasMaxLength(50);
            entity.Property(c => c.Description).HasMaxLength(200);
            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));
            entity.HasKey(en => en.EnrolmentID);
            entity.HasIndex(en => new { en.ParticipantID, en.EventID }).IsUnique().HasDatabaseName("UQ_Enrolment");
            entity.Property(en => en.RegistrationDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(en => en.Status).HasMaxLength(20).HasDefaultValue(EnrolmentStatuses.Pending);
            entity.HasOne(en => en.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(en => en.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(en => en.Event)
                .WithMany(e => e.Enrolments)
                .HasForeignKey(en => en.EventID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Published', 'Pending', 'Disqualified')"));
            entity.HasKey(r => r.ResultID);
            entity.HasIndex(r => r.EnrolmentID).IsUnique();
            entity.Property(r => r.FinishTime).HasColumnType("time");
            entity.Property(r => r.Status).HasMaxLength(20).HasDefaultValue(ResultStatuses.Published);
            entity.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<RaceResult>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
