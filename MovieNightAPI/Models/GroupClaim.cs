using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupClaim
    {
        public int group_id { get; set; }
        public bool is_admin { get; set; }
    }
}
