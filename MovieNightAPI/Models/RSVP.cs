using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class RSVP
    {
        public int event_id { get; set; }
        public int user_id { get; set; }
        public Boolean is_coming { get; set; }
    }
}
