using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Data;

/// <summary>
/// One conversation with the database. Registered Scoped (one per HTTP request)
/// by AddDbContext in Program.cs — which is why TaskService must also be Scoped.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(t => t.Description)
                  .HasMaxLength(1000);

            // Stored as strings so the table is readable when inspected directly,
            // and so filtering translates to WHERE Status = 'Todo' rather than = 1.
            entity.Property(t => t.Priority)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(t => t.Status)
                  .HasConversion<string>()
                  .HasMaxLength(12)
                  .IsRequired();

            // Every list request filters by Status and orders by CreatedAt.
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.CreatedAt).IsDescending();
        });
    }
}
