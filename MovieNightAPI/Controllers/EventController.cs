using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieNightAPI.Models;
using MovieNightAPI.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SpikeExerciseAPI.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public EventController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        #region POST

        // POST /event
        [ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] GroupEvent group_event)
        {
            DataAccessResult result = _dataAccess.CreateEvent(group_event);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // POST /event/rsvp
        [ProducesResponseType(typeof(IEnumerable<RSVP>), StatusCodes.Status200OK)]
        [HttpPost("rsvp")]
        public IActionResult Join([FromBody] RSVP rsvp)
        {
            DataAccessResult result = _dataAccess.JoinEvent(rsvp);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // POST /event/{event_id}/movies
        [ProducesResponseType(typeof(IEnumerable<EventMovies>), StatusCodes.Status200OK)]
        [HttpPost("{event_id}/movies")]
        public IActionResult AddMovieEvent(int event_id, [FromBody] MovieIDList movie_ids)
        {
            DataAccessResult result = _dataAccess.AddMovieEvent(event_id, movie_ids);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        #endregion

        #region GET

        // GET /event/{event_id}
        [ProducesResponseType(typeof(IEnumerable<GroupEvent>), StatusCodes.Status200OK)]
        [HttpGet("{event_id}")]
        public IActionResult Get(int event_id)
        {
            DataAccessResult result = _dataAccess.GetEvent(event_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /event/{event_id}/rsvp
        [ProducesResponseType(typeof(IEnumerable<RSVP>), StatusCodes.Status200OK)]
        [HttpGet("{event_id}/rsvp")]
        public IActionResult GetGroup(int event_id)
        {
            DataAccessResult result = _dataAccess.GetRSVPs(event_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /event/{event_id}/movies/{user_id}
        [ProducesResponseType(typeof(IEnumerable<EventMovieRatings>), StatusCodes.Status200OK)]
        [HttpGet("{event_id}/movies/{user_id}")]
        public IActionResult GetMoviesEvent(int event_id, int user_id)
        {
            DataAccessResult result = _dataAccess.GetMoviesEventUser(event_id, user_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // Get /event/{event_id}/rating
        [ProducesResponseType(typeof(IEnumerable<EventMovieRatings>), StatusCodes.Status200OK)]
        [HttpGet("{event_id}/rating")]
        public IActionResult GetEventRating(int event_id)
        {
            DataAccessResult result = _dataAccess.GetEventRating(event_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        #endregion

        #region PATCH

        // PATCH /event/{event_id}/rsvp/{user_id}
        [ProducesResponseType(typeof(IEnumerable<RSVP>), StatusCodes.Status200OK)]
        [HttpPatch("{event_id}/rsvp/{user_id}")]
        public IActionResult ChangeRSVP(int event_id, int user_id, [FromBody] IsComing is_coming)
        {
            DataAccessResult result = _dataAccess.ChangeRSVP(event_id, user_id, is_coming);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /event/rating
        [ProducesResponseType(typeof(IEnumerable<GroupMovies>), StatusCodes.Status200OK)]
        [HttpPatch("rating")]
        public IActionResult RateMovieEvent([FromBody] EventMovieRatings event_movie_ratings)
        {
            DataAccessResult result = _dataAccess.UpdateEventMovieRating(event_movie_ratings);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /event/{event_id}
        [ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpPatch("{event_id}")]
        public IActionResult ChangeVotingMode(int event_id, [FromBody] VotingMode voting_mode)
        {
            DataAccessResult result = _dataAccess.ChangeEventVotingMode(event_id, voting_mode);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /event/{event_id}/movie
        [ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpPatch("{event_id}/movie")]
        public IActionResult ChangeEventMovie(int event_id, [FromBody] MovieID tmdb_movie_id)
        {
            DataAccessResult result = _dataAccess.ChangeEventMovie(event_id, tmdb_movie_id.tmdb_movie_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        #endregion

        #region DELETE

        // Delete /event/{event_id}
        [ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpDelete("{event_id}")]
        public IActionResult ChangeVotingMode(int event_id)
        {
            DataAccessResult result = _dataAccess.RemoveEvent(event_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        #endregion
    }
}
