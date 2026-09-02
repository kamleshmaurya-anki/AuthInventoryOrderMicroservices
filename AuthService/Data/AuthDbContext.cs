using Microsoft.EntityFrameworkCore;
using AuthService.Entities;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.UserId)
                  .HasColumnName("user_id")
                  .HasDefaultValueSql("NEWID()");

            entity.Property(u => u.Username)
                  .HasColumnName("username")
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(u => u.PasswordHash)
                  .HasColumnName("password_hash")
                  .IsRequired();

            entity.Property(u => u.Role)
                  .HasColumnName("role")
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(u => u.IsActive)
                  .HasColumnName("is_active")
                  .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(u => u.Username).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("chk_user_role", "[role] IN ('ADMIN','USER')"));
        });
    }
}
