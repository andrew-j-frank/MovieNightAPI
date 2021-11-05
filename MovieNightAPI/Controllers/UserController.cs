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
    public class UserController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public UserController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        // POST /group join
        [ProducesResponseType(typeof(GroupJoin), StatusCodes.Status200OK)]
        [HttpPost("{user_id}/join/{group_id}")]
        public IActionResult JoinGroup(int user_id, int group_id, [FromBody] GroupUserDB groupUser)
        {
            var result = _dataAccess.JoinGroup(group_id, user_id, groupUser.alias, groupUser.is_admin);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /group alias
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

        // PATCH /group alias
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

        // GET /group
        [ProducesResponseType(typeof(IEnumerable<Group>), StatusCodes.Status200OK)]
        [HttpGet("{user_id}/groups")]
        public IActionResult Get(int user_id)
        {
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

        // Delete 
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
    }
}
