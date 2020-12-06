using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADSBSharp {
    public class Aircraft {
        public DateTime LastMessage { get; set; }
        public int CurrentAltitude { get; set; }
        public int CurrentSpeed { get; set; }
        public string FlightID { get; set; }
        public long ICAO { get; set; }
        public string idk { get; set; }
    }
}
