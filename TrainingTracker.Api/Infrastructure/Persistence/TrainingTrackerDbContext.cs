using Microsoft.EntityFrameworkCore;
using TrainingTracker.Api.Domain.Entities;

namespace TrainingTracker.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext used strictly for DML (no migrations-driven DDL). Schema is managed via SSDT/DACPAC.
/// </summary>
public class TrainingTrackerDbContext : DbContext
{
    public TrainingTrackerDbContext(DbContextOptions<TrainingTrackerDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Courses
        modelBuilder.Entity<Course>(b =>
        {
            b.ToTable("Courses", schema: "training");
            b.HasKey(c => c.Id);
            // SSDT uses column name 'Id' for the PK, so don't override the column name here.
            b.Property(c => c.Title).IsRequired().HasMaxLength(200);
            b.Property(c => c.Category).HasMaxLength(100);
            // DB defines Description as NVARCHAR(MAX) so avoid constraining it here.
            b.Property(c => c.Description);
            b.Property(c => c.IsRequired).IsRequired();
            b.Property(c => c.IsActive).IsRequired();
            b.Property(c => c.ValidityMonths);
            // CreatedUtc is not present in the current SSDT schema; do not require it here.
        });

        // Users
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users", schema: "training");
            b.HasKey(u => u.Id);
            // SSDT uses column name 'Id' for the PK; keep the default mapping.
            b.Property(u => u.FirstName).HasMaxLength(128).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(128).IsRequired();
            b.Property(u => u.Email).HasMaxLength(256).IsRequired();
            b.Property(u => u.IsActive).IsRequired();
            // CreatedUtc is not present in the current SSDT schema; do not require it here.
        });

        // Enrollments
        modelBuilder.Entity<Enrollment>(b =>
        {
            b.ToTable("Enrollments", schema: "training");
            b.HasKey(e => e.Id);
            // SSDT uses 'Id' as the PK column.
            // The SSDT Enrollments table currently contains CourseId, UserId and Status.
            b.Property(e => e.Status).HasMaxLength(50).IsRequired();
            b.HasIndex(e => new { e.CourseId, e.UserId }).IsUnique();

            b.HasOne<Course>()
             .WithMany()
             .HasForeignKey(e => e.CourseId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<User>()
             .WithMany()
             .HasForeignKey(e => e.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}