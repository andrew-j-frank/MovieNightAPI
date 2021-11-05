using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class GroupUser
    {
        public int group_id { get; set; }
        public int user_id { get; set; }
        public string display_name { get; set; }
        public bool is_admin { get; set; }

        public static GroupUser GroupUserDBToGroupUser(GroupUserDB v)
        {
            return new GroupUser()
            {
                display_name = v.alias,
                group_id = v.group_id,
                is_admin = v.is_admin,
                user_id = v.user_id
            };
        }
    }
}
