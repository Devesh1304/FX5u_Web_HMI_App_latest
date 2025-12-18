using System;
using System.ComponentModel.DataAnnotations;

namespace FX5u_Web_HMI_App.Data
{
    // This class represents a single row in our log table
    // This class now represents a single "wide" row in the log table
    public class DataLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        // New columns for each piece of data
        public double Torque { get; set; }
        public double Position { get; set; }
        public double RPM { get; set; }
        public int BrakerNo { get; set; }
        public string BreakerDescription { get; set; } // Your new string field
    }
}
