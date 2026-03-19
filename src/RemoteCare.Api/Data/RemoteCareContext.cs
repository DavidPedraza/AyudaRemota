using Microsoft.EntityFrameworkCore;
using RemoteCare.Api.Models;

namespace RemoteCare.Api.Data
{
    public class RemoteCareContext : DbContext
    {
        public RemoteCareContext(DbContextOptions<RemoteCareContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Role);
                entity.HasIndex(e => e.CreatedAt);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Device configuration
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DeviceId).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.IsActive });
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Devices)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Session configuration
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.SeniorDeviceId, e.SupportDeviceId, e.Status });

                entity.HasOne(e => e.SeniorDevice)
                    .WithMany(d => d.SessionsAsSenior)
                    .HasForeignKey(e => e.SeniorDeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SupportDevice)
                    .WithMany(d => d.SessionsAsSupport)
                    .HasForeignKey(e => e.SupportDeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.Action, e.Timestamp });
                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
