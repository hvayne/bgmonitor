using Microsoft.EntityFrameworkCore;
using bgmonitor.Models;

namespace bgmonitor.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Trip> Trips { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}
