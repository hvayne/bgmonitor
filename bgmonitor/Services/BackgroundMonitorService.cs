using bgmonitor.Data;
using bgmonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace bgmonitor.Services
{
    public class BackgroundMonitorService : BackgroundService
    {
        private readonly BgOperatorService _bgOperatorService;
        private readonly IServiceProvider _serviceProvider;
        private static readonly List<string> Routes = new()
        {
            "BKK_MOW", "BKK_LED", "BKK_VVO", "BKK_KHV", "BKK_IKT", "BKK_KJA", "BKK_OVB", "BKK_SVX",
            "HKT_MOW", "HKT_LED", "HKT_VVO", "HKT_KHV", "HKT_IKT", "HKT_KJA", "HKT_OVB", "HKT_SVX"
        };

        public BackgroundMonitorService(IServiceProvider serviceProvider)
        {
            _bgOperatorService = new BgOperatorService();
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorPrices();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in background service: {ex.Message}");
                }

                // Wait for 1 hour before the next check
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task MonitorPrices()
        {
            Console.WriteLine($"Starting price monitoring at {DateTime.Now}");

            var trips = await _bgOperatorService.GetPricesForRoutes(Routes, DateTime.Now, 14);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var trip in trips)
            {
                Console.WriteLine($"Saving {trip.Route}@{trip.Date:ddMMM} to database...");
                dbContext.Trips.Add(trip);
            }

            await dbContext.SaveChangesAsync();

            Console.WriteLine($"Saved {trips.Count} prices to database");

            // Print the lowest prices for each route
            var lowestPrices = trips
                .GroupBy(t => t.Route)
                .Select(g => new { Route = g.Key, LowestPrice = g.Min(t => t.Price) });

            Console.WriteLine("\nLowest prices per route:");
            foreach (var price in lowestPrices.OrderBy(p => p.LowestPrice))
            {
                Console.WriteLine($"{price.Route}: {price.LowestPrice}");
            }
        }
    }
}
