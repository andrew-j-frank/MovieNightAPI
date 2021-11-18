using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class LoginSignUpUser
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string token { get; set; }
        public static LoginSignUpUser UserDBToUser(UserDB v, string token)
        {
            return new LoginSignUpUser()
            {
                user_id = v.user_id,
                username = v.username,
                email = v.email,
                token = token
            };
        }
    }
}
