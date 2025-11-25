using Microsoft.EntityFrameworkCore;
using VaderData.Core.Models;

namespace VaderData.DataAccess.Context
{
    public class WeatherContext : DbContext
    {
        public DbSet<WeatherData> WeatherData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=vaderdata.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeatherData>()
                .HasIndex(w => new { w.DateTime, w.Location });
        }
    }
}
