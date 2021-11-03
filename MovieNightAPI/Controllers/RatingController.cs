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
    public class RatingController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public RatingController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        // POST /Ratings
        [ProducesResponseType(typeof(GroupMovies), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] MovieRatings movie_ratings)
        {
            // DO we need group_id??
            var result = _dataAccess.RateMovie(movie_ratings);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // Get /Ratings
        [ProducesResponseType(typeof(float), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult Get([FromBody] int tmdb_movie_id, int group_id)
        {
            var result = _dataAccess.GetGroupRating(tmdb_movie_id, group_id);
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
