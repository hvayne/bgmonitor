using Newtonsoft.Json;

namespace bgmonitor
{
    public class BgClass
    {
        [JsonProperty("data")]
        public Data DataItself { get; set; }

        public class Data
        {
            [JsonProperty("meta")]
            public Meta MetaItself { get; set; }
        }

        public class Meta
        {
            [JsonProperty("PleaseGiveMeAPIAccessMyTelegramUsernameUnrandomizer")]
            public SearchResult SearchResultItself { get; set; }
        }

        public class SearchResult
        {
            public string Id { get; set; }
            public List<Node> Nodes { get; set; }
            public List<Location> Locations { get; set; }
            public List<Fare> Fares { get; set; }
            public List<Airline> Airlines { get; set; }
            public List<Route> Routes { get; set; }
            public List<Group> Groups { get; set; }
        }

        public class Airline
        {
            public string Id { get; set; }
            public string N { get; set; }
        }
        public class Location
        {
            public int Id { get; set; }
            public string N { get; set; }
        }

        public class Fare
        {
            public long Id { get; set; }
            [JsonProperty("c")]
            public string Class { get; set; }
            public string F { get; set; }
            [JsonProperty("p")]
            public long Price { get; set; }
            public string Bd { get; set; }
            public string H { get; set; }
        }

        public class Group
        {
            public long Id { get; set; }
            public List<List<List<long>>> N { get; set; }
        }

        public class Node
        {
            public long Id { get; set; }
            [JsonProperty("c")]
            public string Company { get; set; }
            [JsonProperty("f")]
            public string FlightNumber { get; set; }
            [JsonProperty("d")]
            public long DurationMinutes { get; set; }
            [JsonProperty("dd")]
            public string DepartureDate { get; set; }
            [JsonProperty("dcn")]
            public long Dcn { get; set; }
            [JsonProperty("dt")]
            public string DepartureTime { get; set; }
            [JsonProperty("dn")]
            public long Dn { get; set; }
            public long Dcid { get; set; }
            public long Dc { get; set; }
            [JsonProperty("ad")]
            public string ArrivalDate { get; set; }
            public long Acn { get; set; }
            [JsonProperty("at")]
            public string ArrivalTime { get; set; }
            public long An { get; set; }
            public long Acid { get; set; }
            public long Ac { get; set; }
            public string T { get; set; }
            public string Oa { get; set; }
        }

        public class Route
        {
            public string Id { get; set; }
            public List<long> N { get; set; }
        }
    }
}
