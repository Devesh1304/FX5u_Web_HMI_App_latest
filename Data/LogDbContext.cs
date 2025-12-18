using Microsoft.EntityFrameworkCore;

namespace FX5u_Web_HMI_App.Data
{
    // Manages the connection and tables in our database
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options) : base(options) { }

        // Existing logs table
        public DbSet<DataLog> DataLogs { get; set; }

        // NEW: localized breaker names (Gujarati stored here)
        public DbSet<LocaleBreakerName> LocaleBreakerNames => Set<LocaleBreakerName>();

        public DbSet<NameTranslation> NameTranslations => Set<NameTranslation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique per breaker Id (1..20) per language ("en","gu",...)
            modelBuilder.Entity<LocaleBreakerName>()
                .HasIndex(x => new { x.Id, x.Lang })
                .IsUnique();

            // (Optional) ensure max lengths if you want to enforce them at DB level too:
            modelBuilder.Entity<LocaleBreakerName>()
                .Property(x => x.Lang).HasMaxLength(5);
            modelBuilder.Entity<LocaleBreakerName>()
                .Property(x => x.Text).HasMaxLength(20);

            modelBuilder.Entity<NameTranslation>()
           .HasIndex(x => x.En)
           .IsUnique();
        }
    }
}
