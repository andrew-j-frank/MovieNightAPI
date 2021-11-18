using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupMovies
    {
        public int group_id { get; set; }
        public int tmdb_movie_id { get; set; }
        public int added_by { get; set; }
    }
}