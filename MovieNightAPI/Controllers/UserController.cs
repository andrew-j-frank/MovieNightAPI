using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieNightAPI.DataAccess;
using MovieNightAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MovieNightAPI.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public UserController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        #region POST

        // POST /user/{user_id}/join
        [ProducesResponseType(typeof(GroupJoinUserResult), StatusCodes.Status200OK)]
        [HttpPost("{user_id}/join")]
        public IActionResult JoinGroup(int user_id, [FromBody] GroupJoinUser groupUser)
        {
            var result = _dataAccess.JoinGroupUser(groupUser, user_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }
        /*
        // POST /user/rating
        [ProducesResponseType(typeof(MovieRatings), StatusCodes.Status200OK)]
        [HttpPost("rating")]
        public IActionResult UserRating([FromBody] MovieRatings rating)
        {
            var result = _dataAccess.RateMovie(rating);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }
        */
        #endregion

        #region GET

        // GET /user/{user_id}/groups
        [ProducesResponseType(typeof(IEnumerable<Group>), StatusCodes.Status200OK)]
        [HttpGet("{user_id}/groups")]
        public IActionResult Get(int user_id)
        {
            // Check if user has access
            if (!_dataAccess.CheckClaims(HttpContext.User.Identity as ClaimsIdentity, user_id, -1, false, false)) { return new UnauthorizedResult(); }

            var result = _dataAccess.GetGroups(user_id);
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

        // PATCH /user/{user_id}/{group_id}/alias
        [ProducesResponseType(typeof(GroupJoin), StatusCodes.Status200OK)]
        [HttpPatch("{user_id}/{group_id}/alias")]
        public IActionResult ChangeAlias(int user_id, int group_id, [FromBody] Alias alias)
        {
            var result = _dataAccess.ChangeAlias(group_id, user_id, alias.alias);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /user/{user_id}/{group_id}/admin
        [ProducesResponseType(typeof(GroupJoin), StatusCodes.Status200OK)]
        [HttpPatch("{user_id}/{group_id}/admin")]
        public IActionResult ChangeAdmin(int user_id, int group_id, [FromBody] IsAdmin admin)
        {
            var result = _dataAccess.ChangeAdmin(group_id, user_id, admin.is_admin);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /user/{user_id}/{group_id}/rating
        [ProducesResponseType(typeof(MovieRatings), StatusCodes.Status200OK)]
        [HttpPatch("{user_id}/{group_id}/rating")]
        public IActionResult PatchUserRating(int user_id, int group_id, [FromBody] MovieRatings rating)
        {
            var result = _dataAccess.UpdateRateMovie(user_id, group_id, rating);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /user/{user_id}/password
        [AllowAnonymous]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [HttpPatch("{user_id}/password")]
        public IActionResult ChangePassword(int user_id, [FromBody] Password password)
        {
            var result = _dataAccess.ChangePassword(user_id, password.password);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /user/forgot_password
        [AllowAnonymous]
        [ProducesResponseType(typeof(MovieRatings), StatusCodes.Status200OK)]
        [HttpPatch("forgot_password")]
        public IActionResult ForgotPassword([FromBody]Username username)
        {
            var result = _dataAccess.ForgotPassword(username.username);
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

        // Delete user/{user_id}/{group_id}
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [HttpDelete("{user_id}/{group_id}")]
        public IActionResult DeleteGroup(int user_id, int group_id)
        {
            var result = _dataAccess.DeleteUserGroup(user_id, group_id);
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
