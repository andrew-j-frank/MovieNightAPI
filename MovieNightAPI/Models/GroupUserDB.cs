using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupUserDB
    {
        public int group_id { get; set; }
        public int user_id { get; set; }
        public string alias { get; set; }
        public bool is_admin { get; set; }
    }
}
