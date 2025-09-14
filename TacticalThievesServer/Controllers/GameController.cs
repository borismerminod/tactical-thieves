using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TacticalThievesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        [HttpPost("move")]
        public IActionResult Move()
        {
            return Ok(new { success = true });
        }

        [HttpPost("stealth")]
        public IActionResult Stealth()
        {
            return Ok(new { success = true });
        }
    }
}
