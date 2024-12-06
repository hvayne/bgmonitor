using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace bgmonitor
{
    internal class Program
    {
        static void Main()
        {
            List<Trip> trips = new();
            List<string> routes = new()
            {
                "BKK_MOW",
                "BKK_LED",
                "BKK_VVO",
                "BKK_KHV",
                "BKK_IKT",
                "BKK_KJA",
                "BKK_OVB",
                "BKK_SVX",
                "HKT_MOW",
                "HKT_LED",
                "HKT_VVO",
                "HKT_KHV",
                "HKT_IKT",
                "HKT_KJA",
                "HKT_OVB",
                "HKT_SVX"
            };
            foreach (string route in routes)
            {
                DateTime date = DateTime.Now.AddDays(0);
                while (date < DateTime.Now.AddDays(14))
                {
                    trips.Add(new Trip { Date = date, Route = route });
                    date += TimeSpan.FromDays(1);
                }
            }
            trips = trips.OrderBy(t => Guid.NewGuid()).ToList();

            int maxConcurrentTasks = 9;
            var semaphore = new SemaphoreSlim(maxConcurrentTasks);
            var results = new ConcurrentBag<Trip>();

            List<Task> tasks = trips.Select(async trip =>
            {
                await semaphore.WaitAsync();
                try
                {
                    long price = await RequestFares(trip.Route, trip.Date);
                    trip.Price = price;
                    results.Add(trip);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }

            var orderedResults = results.OrderBy(t => t.Price);
            Console.WriteLine("==========ORDERED RESULTS BELOW==========");
            foreach (var trip in orderedResults)
            {
                Console.WriteLine($"{trip.Route} @ {trip.Date:ddMMM} {trip.Price}");
            }
            Console.WriteLine();

            List<Trip> secondAttempt = new List<Trip>();
            foreach (var trip in trips)
            {
                if (trip.Price == 0)
                {
                    secondAttempt.Add(trip);
                }
            }
            Console.WriteLine($"{secondAttempt.Count} tasks failed");
            var secondAttemptResults = new ConcurrentBag<Trip>();

            List<Task> secondAttemptTasks = secondAttempt.Select(async trip =>
            {
                await semaphore.WaitAsync();
                try
                {
                    long price = await RequestFares(trip.Route, trip.Date);
                    trip.Price = price;
                    secondAttemptResults.Add(trip);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            try
            {
                Task.WaitAll(secondAttemptTasks.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }

            var orderedTrips = trips.OrderBy(t => t.Price);
            Console.WriteLine($"==========ORDERED TRIPS BELOW==========");
            foreach (var trip in orderedTrips)
            {
                Console.WriteLine($"{trip.Route} @ {trip.Date:ddMMM} {trip.Price}");
            }
            //long price = RequestFares(route, date);
            //Console.WriteLine($"{route} {date:ddMMyyy} {price}");
            //date += TimeSpan.FromDays(1);
        }
        static async Task<long> RequestFares(string route, DateTime date)
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
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(20);
            string url = "https://www.bgoperator.ru/site?action=biletgraphql&task=biletjson";
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            string json = string.Empty;

            Console.WriteLine($"Requesting {route}@{date:ddMMM}...");

            // Send the POST request
            HttpResponseMessage response = await client.PostAsync(url, content);

            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                File.WriteAllText($"{dateAndRoute}{Guid.NewGuid()}.json", responseBody);
                json = responseBody;
            }
            else
            {
                Console.WriteLine($"{route}@{date:ddMMM} HTTPCode={response.StatusCode}");
            }

            BgClass bgDataObject = JsonConvert.DeserializeObject<BgClass>(json);
            if (bgDataObject.DataItself.MetaItself.SearchResultItself.Fares.Count == 0)
            {
                Console.WriteLine($"{route}@{date:ddMMM} request yielded zero results");
                return 1000000;
            }
            long lowestPrice = bgDataObject.DataItself.MetaItself.SearchResultItself.Fares.First().Price;
            Console.WriteLine($"{route}@{date:ddMMM} lowest price is {lowestPrice}");
            return lowestPrice;
        }
        static string ComputeMD5Hash(string input)
        {
            // Convert input string to byte array
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            // Create MD5 instance and compute the hash
            using MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // Format as a two-character hexadecimal string
            }

            return sb.ToString();
        }
        public class Trip
        {
            public string Route { get; set; }
            public DateTime Date { get; set; }
            public long Price { get; set; }
        }
    }

}