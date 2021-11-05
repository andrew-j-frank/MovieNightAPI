using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupJoin
    {
        public int group_id { get; set; }
        public string group_name { get; set; }
        public int created_by { get; set; }
        public string alias { get; set; }
        public bool is_admin { get; set; }
        public int group_code { get; set; }
    }
}
