using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TacticalThievesServer.Services;

namespace TacticalThievesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {

        private readonly ThiefStateService thiefState;

        public GameController(ThiefStateService thiefState)
        {
            this.thiefState = thiefState;
        }

        [HttpPost("move")]
        public IActionResult Move()
        {
            this.thiefState.Move();
            return Ok(new { success = true });
        }

        [HttpPost("stealth")]
        public IActionResult Stealth()
        {
            this.thiefState.Stealth();
            return Ok(new { success = true });
        }
    }
}
