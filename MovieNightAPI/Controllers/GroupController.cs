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
    public class GroupController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public GroupController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        #region POST

        // POST /group
        [ProducesResponseType(typeof(GroupJoin), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult CreateGroup([FromBody] GroupJoin group)
        {
            var result = _dataAccess.CreateGroup(group);
            if (result.error)
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
            else
            {
                if (group.alias == "") group.alias = null;
                var result2 = _dataAccess.JoinGroupCreator(group);
                if (result2.error)
                {
                    return StatusCode(result2.statusCode, new { message = result2.message });
                }
                else
                {
                    return Ok(result2.returnObject);
                }

            }
        }

        // POST /group/{group_id}/movie
        [ProducesResponseType(typeof(GroupMovies), StatusCodes.Status200OK)]
        [HttpPost("{group_id}/movie")]
        public IActionResult AddMovie(int group_id, [FromBody] GroupMoviesNoId movie)
        {
            GroupMovies groupMovie = new GroupMovies();
            groupMovie.added_by = movie.added_by;
            groupMovie.tmdb_movie_id = movie.tmdb_movie_id;
            groupMovie.group_id = group_id;

            var result = _dataAccess.AddGroupMovie(groupMovie);
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

        // Get /group/{group_id}/users
        [ProducesResponseType(typeof(IEnumerable<GroupUser>), StatusCodes.Status200OK)]
        [HttpGet("{group_id}/users")]
        public IActionResult GetUsers(int group_id)
        {
            var result = _dataAccess.GetUsers(group_id);
            if (result.error)
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
            else
            {
                return Ok(result.returnObject);
            }
        }

        // Get /group/{group_id}
        [ProducesResponseType(typeof(IEnumerable<GroupUser>), StatusCodes.Status200OK)]
        [HttpGet("{group_id}")]
        public IActionResult GetGroup(int group_id)
        {
            var result = _dataAccess.GetGroup(group_id);
            if (result.error)
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
            else
            {
                return Ok(result.returnObject);
            }
        }

        // Get /group/{group_id}/movies/{user_id}
        [ProducesResponseType(typeof(IEnumerable<GroupMovieRating>), StatusCodes.Status200OK)]
        [HttpGet("{group_id}/movies/{user_id}")]
        public IActionResult GetMovies(int group_id, int user_id)
        {
            var result = _dataAccess.GetMovies(group_id, user_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // Get /group/{group_id}/events
        [ProducesResponseType(typeof(IEnumerable<GroupEvent>), StatusCodes.Status200OK)]
        [HttpGet("{group_id}/events")]
        public IActionResult GetGroupEvents(int group_id)
        {
            var result = _dataAccess.GetEvents(group_id);
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

        // PATCH /group/{group_id}/max_user_movies
        [ProducesResponseType(typeof(GroupJoin), StatusCodes.Status200OK)]
        [HttpPatch("{group_id}/max_user_movies")]
        public IActionResult ChangeMaxMovies(int group_id, [FromBody] MaxUserMovies max_user_movies)
        {
            var result = _dataAccess.ChangeMaxMovies(group_id, max_user_movies.max_user_movies);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /group/{group_id}/group_name
        [ProducesResponseType(typeof(GroupJoin), StatusCodes.Status200OK)]
        [HttpPatch("{group_id}/group_name")]
        public IActionResult ChangeGroupName(int group_id, [FromBody] GroupName groupName)
        {
            var result = _dataAccess.ChangeGroupName(group_id, groupName.group_name);
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

        // DELETE /group/{group_id}/movie/{tmdb_movie_id}
        [ProducesResponseType(typeof(GroupMovies), StatusCodes.Status200OK)]
        [HttpDelete("{group_id}/movie/{tmdb_movie_id}")]
        public IActionResult DeleteMovie(int group_id, int tmdb_movie_id)
        {
            var result = _dataAccess.RemoveMovie(group_id, tmdb_movie_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // Delete /group
        [ProducesResponseType(typeof(Group), StatusCodes.Status200OK)]
        [HttpDelete("{group_id}")]
        public IActionResult DeleteGroup(int group_id)
        {
            var result = _dataAccess.DeleteGroup(group_id);
            if (result.error)
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
            else
            {
                return Ok(result.returnObject);
            }
        }

        #endregion
    }
}
