using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using bgmonitor.Models;

namespace bgmonitor.Services
{
    public class BgOperatorService
    {
        private readonly HttpClient _httpClient;
        private const int MaxConcurrentTasks = 20;
        private readonly SemaphoreSlim _semaphore;

        public BgOperatorService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _semaphore = new SemaphoreSlim(MaxConcurrentTasks);
        }

        public async Task<List<Trip>> GetPricesForRoutes(IEnumerable<string> routes, DateTime startDate, int daysAhead)
        {
            var trips = new List<Trip>();
            foreach (string route in routes)
            {
                DateTime date = startDate;
                while (date < startDate.AddDays(daysAhead))
                {
                    trips.Add(new Trip { Date = date, Route = route, CreatedAt = DateTime.Now });
                    date = date.AddDays(1);
                }
            }

            var results = new ConcurrentBag<Trip>();
            
            var options = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = MaxConcurrentTasks 
            };

            await Parallel.ForEachAsync(trips, options, async (trip, token) =>
            {
                await _semaphore.WaitAsync(token);
                try
                {
                    long price = await RequestFares(trip.Route, trip.Date);
                    trip.Price = price;
                    results.Add(trip);
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            var failedTrips = trips.Where(t => t.Price == 0).ToList();
            if (failedTrips.Any())
            {
                Console.WriteLine($"{failedTrips.Count} tasks failed, retrying with backoff...");
                
                for (int attempt = 1; attempt <= 3 && failedTrips.Any(); attempt++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                    
                    await Parallel.ForEachAsync(failedTrips.ToList(), options, async (trip, token) =>
                    {
                        await _semaphore.WaitAsync(token);
                        try
                        {
                            long price = await RequestFares(trip.Route, trip.Date);
                            if (price > 0)
                            {
                                trip.Price = price;
                                failedTrips.Remove(trip);
                            }
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                }
            }

            return trips.OrderBy(t => t.Price).ToList();
        }

        private async Task<long> RequestFares(string route, DateTime date)
        {
            string dateAndRoute = $"0{date:ddMMyyyy}_{route}.";
            string dataToHash = dateAndRoute + dateAndRoute.Substring(4, 6);
            string hash = ComputeMD5Hash(dataToHash);
            string somethingMine = dateAndRoute + $"/{hash}";

            string payload = "{\"query\":\"query Index {meta {...F0}} fragment F0 on Meta {PleaseGiveMeAPIAccessMyTelegramUsernameUnrandomizer:" +
                "_searchdata(searchflights:\\\"" + somethingMine + "\\\"," +
                "adt:1,chd:0,inf:0,airline:\\\"\\\",direct:\\\"0\\\",rand:0) {id,airlines {id,n},locations" +
                " {id,n},fares {id,h,p,bd,c,f},nodes {id,ac,acid,acn,ad,an,at,c,f,d,dc,dcid,dcn,dd,dn,dt,t," +
                "oa},routes {id,n},groups {id,n}}}\",\"variables\":{}}";

            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            string url = "https://www.bgoperator.ru/site?action=biletgraphql&task=biletjson";

            Console.WriteLine($"Requesting {route}@{date:ddMMM}...");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var bgDataObject = JsonConvert.DeserializeObject<BgClass>(responseBody);
                    if (bgDataObject?.DataItself?.MetaItself?.SearchResultItself?.Fares?.Count == 0)
                    {
                        Console.WriteLine($"{route}@{date:ddMMM} request yielded zero results");
                        return 1000000;
                    }
                    long lowestPrice = bgDataObject.DataItself.MetaItself.SearchResultItself.Fares.First().Price;
                    Console.WriteLine($"{route}@{date:ddMMM} lowest price is {lowestPrice}");
                    return lowestPrice;
                }
                
                Console.WriteLine($"{route}@{date:ddMMM} HTTPCode={response.StatusCode}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{route}@{date:ddMMM} Error: {ex.Message}");
                return 0;
            }
        }

        private static string ComputeMD5Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            using MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
