using System;

namespace FX5u_Web_HMI_App.Pages
{
    // This class defines the structure of one row in your new table
    public class HistoricalDataRow
    {
        public DateTime Timestamp { get; set; }

        public string LocalTime { get; set; }
        public double Torque { get; set; }
        public double Position { get; set; }
        public double RPM { get; set; }
        public double BrakerNo { get; set; }
        public string BreakerDescription { get; set; }
    }
}