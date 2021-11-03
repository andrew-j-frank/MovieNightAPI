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

        // POST /group
        [ProducesResponseType(typeof(Group), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] string group_name, int creator_id, string alias)
        {
            Group group = new Group();
            group.group_name = group_name;
            group.created_by = creator_id;

            var result = _dataAccess.CreateGroup(group);
            if (result.error)
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
            else
            {
                var result2 = _dataAccess.JoinGroup(group.group_id, creator_id, alias, true);
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

        // POST /group join
        [ProducesResponseType(typeof(Group), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] int group_id, int user_id, string alias)
        {
            var result = _dataAccess.JoinGroup(group_id, user_id, alias);
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
        [ProducesResponseType(typeof(Group), StatusCodes.Status200OK)]
        [HttpPatch]
        public IActionResult Patch([FromBody] int group_id, int user_id, string alias)
        {
            var result = _dataAccess.ChangeAlias(group_id, user_id, alias);
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
        [HttpPost]
        public IActionResult Get([FromBody] int user_id)
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

        //TODO(speters): leaveGroup(int group_id, int user_id)
    }
}
