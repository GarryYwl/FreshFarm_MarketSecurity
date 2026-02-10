using FreshFarmMarketSecurity.Models;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Data
{
    public class FreshFarmDbContext : DbContext
    {
        public FreshFarmDbContext(DbContextOptions<FreshFarmDbContext> options)
            : base(options) { }

        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<PasswordHistory> PasswordHistories { get; set; }

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure Email is unique
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
