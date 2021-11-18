using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class MovieRatings
    {
        public int group_id { get; set; }
        public int user_id { get; set; }
        public int tmdb_movie_id { get; set; }
        public int user_rating { get; set; }
    }
}
