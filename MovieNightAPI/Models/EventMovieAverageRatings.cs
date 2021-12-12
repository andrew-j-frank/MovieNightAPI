using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class EventMovieAverageRatings
    {
        public int tmdb_movie_id { get; set; }
        public double avg_rating { get; set; }
    }
}
