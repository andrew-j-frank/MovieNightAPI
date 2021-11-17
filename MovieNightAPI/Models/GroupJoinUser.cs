using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupJoinUser
    {
        public string group_code { get; set; }
        public string alias { get; set; }
        public Boolean is_admin { get; set; }
    }
}
