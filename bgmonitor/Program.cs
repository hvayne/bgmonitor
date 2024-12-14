using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using bgmonitor.Data;
using bgmonitor.Services;
using bgmonitor.Models;

namespace bgmonitor
{
    public class Program
    {
        private static readonly List<string> Routes = new()
        {
            "BKK_MOW", "BKK_LED", "BKK_VVO", "BKK_KHV", "BKK_IKT", "BKK_KJA", "BKK_OVB", "BKK_SVX",
            "HKT_MOW", "HKT_LED", "HKT_VVO", "HKT_KHV", "HKT_IKT", "HKT_KJA", "HKT_OVB", "HKT_SVX"
        };

        public static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            // Get the BgOperatorService from DI
            // using var scope = host.Services.CreateScope();
            // var bgOperatorService = scope.ServiceProvider.GetRequiredService<BgOperatorService>();
            //
            // try
            // {
            //     // Use the service to get prices
            //     var trips = await bgOperatorService.GetPricesForRoutes(Routes, DateTime.Now, 14);
            //
            //     // Print results
            //     Console.WriteLine("\n==========ORDERED RESULTS BELOW==========");
            //     foreach (var trip in trips.OrderBy(t => t.Price))
            //     {
            //         Console.WriteLine($"{trip.Route} @ {trip.Date:ddMMM} {trip.Price}");
            //     }
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"Error occurred: {ex.Message}");
            // }

            // If you want to run the background service instead, uncomment this line:
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Configure DbContext
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite("Data Source=/Users/neerd/Projects/dotnet/bgmonitor/bgmonitor/bgmonitor.db"));

                    // Register BgOperatorService
                    services.AddScoped<BgOperatorService>();

                    // Register BackgroundMonitorService if you want to use it
                    services.AddHostedService<BackgroundMonitorService>();

                    // Configure HttpClient
                    services.AddHttpClient("BgOperator", client =>
                    {
                        client.BaseAddress = new Uri("https://www.bgoperator.ru/");
                        client.Timeout = TimeSpan.FromSeconds(20);
                    });
                });
    }
}