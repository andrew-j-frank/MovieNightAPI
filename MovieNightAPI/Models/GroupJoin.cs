using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupJoin
    {

        public string group_name { get; set; }
        public int created_by { get; set; }
        public int group_id { get; set; }
        public string group_code { get; set; }
        public string alias { get; set; }
    }
}