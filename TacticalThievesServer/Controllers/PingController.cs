using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TacticalThievesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpPost("ping")]
        public IActionResult Ping([FromBody] PingRequest request)
        {
            Console.WriteLine($"Ping received: {request.Message ?? "<null>"}");
            return Ok(new { response = "Pong from ASP.NET", time = DateTime.Now });
        }
    }

    public class PingRequest
    {
        public string Message { get; set; } = "";
    }
}
