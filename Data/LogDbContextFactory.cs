using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration; // <-- This was missing
using System.IO; // <-- This was missing

namespace FX5u_Web_HMI_App.Data
{
    public class LogDbContextFactory : IDesignTimeDbContextFactory<LogDbContext>
    {
        public LogDbContext CreateDbContext(string[] args)
        {
            // This builds the configuration to find your appsettings.json file
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // This creates the options builder for the DbContext
            var builder = new DbContextOptionsBuilder<LogDbContext>();

            // This reads your connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // This tells the builder to use SQLite with that connection string
            builder.UseSqlite(connectionString);

            // This successfully creates and returns the DbContext
            return new LogDbContext(builder.Options);
        }
    }
}