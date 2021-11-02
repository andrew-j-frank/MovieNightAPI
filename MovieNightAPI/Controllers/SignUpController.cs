using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieNightAPI.DataAccess;
using MovieNightAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SignUpController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public SignUpController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        // POST /signup
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] SignUp signUp)
        {
            var result = _dataAccess.SignUp(signUp);
            if(!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }
    }
}
