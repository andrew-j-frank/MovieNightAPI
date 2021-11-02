using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class User
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public static User UserDBToUser(UserDB v)
        {
            return new User()
            {
                user_id = v.user_id,
                username = v.username,
                email = v.email
            };
        }
    }
}
