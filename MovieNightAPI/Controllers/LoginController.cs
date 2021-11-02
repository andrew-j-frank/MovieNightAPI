using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieNightAPI.Models;
using MovieNightAPI.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SpikeExerciseAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public LoginController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        // POST /login
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] Login login)
        {
            var user = _dataAccess.Login(login);
            if (user != null)
            {
                return Ok(user);
            }
            else
            {
                return StatusCode(404, new { message = "username and password combination not found" });
            }
        }
    }
}
