using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupEvent
    {
        public int event_id { get; set; }
        public DateTime start_time { get; set; }
        public string location { get; set; }
        public int genre { get; set; }
        public int tmdb_movie_id { get; set; }
        public int organized_by { get; set; }
        public int voting_mode { get; set; }
    }
}
