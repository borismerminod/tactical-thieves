using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TacticalThievesServer.DTO;
using TacticalThievesServer.Services;

namespace TacticalThievesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {

        private readonly ThiefStateService thiefState;
        private readonly WebSocketHandler webSocketHandler;
        private readonly IHubContext<ClientHub> clientHub;

        public GameController(ThiefStateService thiefState, WebSocketHandler webSocketHandler, IHubContext<ClientHub> clientHub)
        {
            this.thiefState = thiefState;
            this.webSocketHandler = webSocketHandler;
            this.clientHub = clientHub;
        }

        [HttpPost("move")]
        public IActionResult Move()
        {
            this.thiefState.Move();
            webSocketHandler.Broadcast("move");
            return Ok(new { success = true });
        }

        [HttpPost("stealth")]
        public IActionResult Stealth()
        {
            this.thiefState.Stealth();
            webSocketHandler.Broadcast("stealth");
            return Ok(new { success = true });
        }

        [HttpPost("collect-treasure")]
        public IActionResult CollectTreasure([FromBody] TreasureCollectDTO dto)
        {
            if (dto == null || dto.Amount <= 0)
                return BadRequest(new { success = false, message = "Invalid treasure amount" });

            //clientHub.SendPlayerGoldUpdate(dto.Amount);
            clientHub.Clients.All.SendAsync("ScoreUpdated", dto.Amount);

            return Ok(new { success = true, gold = dto.Amount });
        }
    }
}
