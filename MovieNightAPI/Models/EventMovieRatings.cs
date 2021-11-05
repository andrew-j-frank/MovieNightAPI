using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class EventMovieRatings
    {
        public int event_id { get; set; }
        public int user_id { get; set; }
        public int tmdb_movie_id { get; set; }
        public int rating { get; set; }
    }
}
