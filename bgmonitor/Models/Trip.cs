using System;

namespace bgmonitor.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public string Route { get; set; }
        public DateTime Date { get; set; }
        public long Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
