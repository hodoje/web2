﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.DomainModels
{
    // Checked
    public class Station
    {
        public int Id { get; set; }
        public string Name { get; set; }
        // x coord
        public double Longitude { get; set; }
        // y coord
        public double Latitude { get; set; }
        public ICollection<TransportationLineRoutePoint> TransportationLineRoutePoints { get; set; }
        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}