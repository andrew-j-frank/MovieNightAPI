using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Models
{
    public class DataAccessResult
    {
        public Object returnObject { get; set; }
        public bool error { get; set; }
        public int statusCode { get; set; }
        public string message { get; set; }
        public DataAccessResult()
        {
            error = false;
        }
    }
}
