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
    public class EventController : ControllerBase
    {
        private IDataAccess _dataAccess;

        public EventController(IDataAccess dataAccess)
        {
            this._dataAccess = dataAccess;
        }

        // POST /Event
        [ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Post([FromBody] GroupEvent group_event)
        {
            var result = _dataAccess.CreateEvent(group_event);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /Event group
        [ProducesResponseType(typeof(IEnumerable<GroupEvent>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetGroup([FromBody] int group_id)
        {
            var result = _dataAccess.GetGroupEvents(group_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /Event user
        [ProducesResponseType(typeof(IEnumerable<GroupEvent>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetUser([FromBody] int user_id)
        {
            var result = _dataAccess.GetUserEvents(user_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /Event userGroup
        [ProducesResponseType(typeof(IEnumerable<GroupEvent>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetUserGroupEvents([FromBody] int user_id, int group_id)
        {
            var result = _dataAccess.GetUserGroupEvents(user_id, group_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // POST /Event join
        //[ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult Join([FromBody] int user_id, int event_id)
        {
            var result = _dataAccess.JoinEvent(user_id, event_id);
            if (!result.error)
            {
                return Ok(200);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // PATCH /Event leave
        //[ProducesResponseType(typeof(GroupEvent), StatusCodes.Status200OK)]
        [HttpPatch]
        public IActionResult Leave([FromBody] int user_id, int event_id)
        {
            var result = _dataAccess.LeaveEvent(user_id, event_id);
            if (!result.error)
            {
                return Ok(200);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /Event rsvp
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetRSVP([FromBody] int event_id)
        {
            var result = _dataAccess.GetRSVP(event_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
            }
        }

        // GET /Event movies
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetMovies([FromBody] int event_id)
        {
            var result = _dataAccess.GetMoviesEvent(event_id);
            if (!result.error)
            {
                return Ok(result.returnObject);
            }
            else
            {
                return StatusCode(result.statusCode, new { message = result.message });
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

        //TODO(speters): leaveGroup(int group_id, int user_id)
    }
}
