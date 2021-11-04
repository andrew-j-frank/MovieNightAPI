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
    public class MovieController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public MovieController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        // POST /Movie
        [ProducesResponseType(typeof(GroupMovies), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] GroupMovies group_movies)
        {
            var result = _dataAccess.AddGroupMovie(group_movies);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // DELETE /Movie
        //[ProducesResponseType(typeof(GroupMovies), StatusCodes.Status200OK)]
        [HttpDelete]
        public IActionResult Delete([FromBody] int group_id, int tmdb_movie_id)
        {
            var result = _dataAccess.RemoveMovie(group_id, tmdb_movie_id);
            if (!result.error)
            {
                return Ok(200);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // Get /Movie
        [ProducesResponseType(typeof(IEnumerable<GroupMovies>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult Get([FromBody] int group_id)
        {
            var result = _dataAccess.GetMovies(group_id);
            if (!result.error)
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
