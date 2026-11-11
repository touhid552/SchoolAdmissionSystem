using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    public DbSet<Hold> Holds => Set<Hold>();

    // This method describes the tables. "dotnet ef migrations add" reads it
    // to create the migration, so the rules below end up in PostgreSQL itself.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.ToTable("Classes", table =>
            {
                table.HasCheckConstraint("CK_Classes_TotalSeats_Positive", DbRules.TotalSeatsPositive);
            });

            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasMaxLength(40);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.TotalSeats).IsRequired();
            entity.Property(c => c.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Hold>(entity =>
        {
            entity.ToTable("Holds", table =>
            {
                table.HasCheckConstraint("CK_Holds_StudentId_17Digits", DbRules.StudentIdHas17Digits);
                table.HasCheckConstraint("CK_Holds_Status_Allowed", DbRules.StatusIsAllowed);
            });

            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasMaxLength(40);
            entity.Property(h => h.ClassId).IsRequired().HasMaxLength(40);
            entity.Property(h => h.StudentId).IsRequired().HasMaxLength(17);
            entity.Property(h => h.Status).IsRequired().HasMaxLength(20);
            entity.Property(h => h.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(h => h.ExpiresAt).HasColumnType("timestamp with time zone");

            // Every hold must point to a class that exists.
            // Restrict: a class that still has holds cannot be deleted.
            entity.HasOne<SchoolClass>()
                  .WithMany()
                  .HasForeignKey(h => h.ClassId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Used to count the holds of one class.
            entity.HasIndex(h => h.ClassId);

            // Used to list one student's applications, newest first.
            entity.HasIndex(h => new { h.StudentId, h.CreatedAt });

            // A student can have only ONE active (HELD or CONFIRMED) hold per class.
            // "Partial" means the index only covers rows that match the filter,
            // so EXPIRED and CANCELLED holds do not block a new application.
            entity.HasIndex(h => new { h.ClassId, h.StudentId })
                  .IsUnique()
                  .HasDatabaseName("UX_Holds_ClassId_StudentId_Active")
                  .HasFilter(DbRules.ActiveHoldFilter);
        });
    }
}
