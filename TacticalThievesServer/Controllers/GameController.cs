using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TacticalThievesServer.Data;
using TacticalThievesServer.DTO;
using TacticalThievesServer.Models;
using TacticalThievesServer.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TacticalThievesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {

        private readonly ThiefStateService thiefState;
        private readonly WebSocketHandler webSocketHandler;
        private readonly IHubContext<ClientHub> clientHub;
        private readonly ApplicationDbContext db;
        private readonly WebSocketLinkerService linker;
        private readonly ILogger<GameController> logger;

        public GameController(ThiefStateService thiefState, WebSocketHandler webSocketHandler, IHubContext<ClientHub> clientHub, ApplicationDbContext db, WebSocketLinkerService linker, ILogger<GameController> logger)
        {
            this.thiefState = thiefState;
            this.webSocketHandler = webSocketHandler;
            this.clientHub = clientHub;
            this.db = db;
            this.linker = linker;
            this.logger = logger;
        }

        private async Task<IActionResult> SendGameMessageAsync(string messageType)
        {
            try
            {
                var sessionId = Request.Headers["X-Session-Id"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                    return BadRequest("Missing sessionId");

                if (!linker.TryGet(sessionId, out var link))
                    return NotFound("Link not found for session");

                var gameMessage = new GameMessage
                {
                    Type = messageType,
                    Level = 0
                };

                var json = JsonSerializer.Serialize(gameMessage);

                // Supposons que SendToClient retourne un bool ou Task<bool>
                bool success = await webSocketHandler.SendToClient(link.UnityGUID, json);

                if (!success)
                    return StatusCode(500, "Failed to send message to client");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SendGameMessageAsync. SessionId: {SessionId}, Type: {MessageType}",Request.Headers["X-Session-Id"].ToString(), messageType);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occured"
                });
            }
        }

        [HttpPost("move")]
        public Task<IActionResult> Move()
        {
            return SendGameMessageAsync("move");
        }

        [HttpPost("stealth")]
        public Task<IActionResult> Stealth()
        {
            return SendGameMessageAsync("stealth");
        }

        [HttpPost("end-turn")]
        public Task<IActionResult> EndTurn()
        {
            return SendGameMessageAsync("end-turn");
        }

        [HttpPost("restart")]
        public Task<IActionResult> Restart()
        {
            return SendGameMessageAsync("restart");
        }

        [HttpPost("load-random-level")]
        public async Task<IActionResult> LoadRandomLevel()
        {
            try
            {
                // récupérer connectionId depuis header
                var connectionId = Request.Headers["X-Connection-Id"].ToString();
                if (string.IsNullOrEmpty(connectionId))
                    return BadRequest("Missing connectionId");

                // récupérer sessionId depuis header
                var sessionId = Request.Headers["X-Session-Id"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                    return BadRequest("Missing sessionId");

                // récupérer le lien
                linker.AddOrUpdate(sessionId, angularGuid: connectionId);
                if (!linker.TryGet(sessionId, out var link))
                    return NotFound("Link not found for session");

                GameMessage gameMessage = new GameMessage
                {
                    Type = "load-random-level",
                    Level = 0
                };
                var json = JsonSerializer.Serialize(gameMessage);

                bool bSuccess = await webSocketHandler.SendToClient(link.UnityGUID, json);

                if(bSuccess == false)
                    return StatusCode(500, "Failed to send message to client");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoadRandomLevel. SessionId: {SessionId}, ConnectionId: {ConnectionId}", Request.Headers["X-Session-Id"].ToString(), Request.Headers["X-Connection-Id"].ToString());
                return BadRequest(new
                {
                    success = false,
                    message = "An error occured"
                });
            }
        }

        private async Task<IActionResult> HandleClientEventAsync<T>(
            T dto, 
            Func<T, bool> isValid, 
            string validationErrorMessage, 
            string eventName, 
            Func<T, object> payloadSelector, 
            Func<object> successResponse
         )
        {
            try
            {
                var sessionId = Request.Headers["X-Session-Id"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                    return BadRequest("Missing sessionId");

                if (!linker.TryGet(sessionId, out var link))
                    return NotFound("Link not found for session");

                if (dto == null || !isValid(dto))
                    return BadRequest(new { success = false, message = validationErrorMessage });

                var payload = payloadSelector(dto);

                await clientHub
                    .Clients
                    .Client(link.AngularGUID)
                    .SendAsync(eventName, payload);

                return Ok(successResponse());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in HandleClientEventAsync. Event: {EventName}", eventName);

                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occured"
                });
            }
        }

        [HttpPost("collect-treasure")]
       public Task<IActionResult> CollectTreasure([FromBody] TreasureCollectDTO dto)
       {
            return HandleClientEventAsync(
                 dto,
                 dto => dto.Amount >= 0,
                 "Invalid treasure amount",
                 "ScoreUpdated",
                 dto => dto.Amount,
                  () => new { success = true, gold = dto.Amount }
            );
          
       }

       [HttpPost("exit-reached")]
       public Task<IActionResult> ExitReached([FromBody] PlayerProgressDTO playerProgress)
       {
            return HandleClientEventAsync(
                playerProgress,
                playerProgress => playerProgress.CurrentLevel >= 0,
                "Invalid level value",
                "ExitReached",
                playerProgress => playerProgress.CurrentLevel,
                () => new { success = true }
             );
          
       }

        [HttpPost("thieves-died")]
        public IActionResult ThievesDied()
        {
            // récupérer sessionId depuis header
            var sessionId = Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest("Missing sessionId");

            // récupérer le lien
            if (!linker.TryGet(sessionId, out var link))
                return NotFound("Link not found for session");

            clientHub.Clients.Client(link.AngularGUID).SendAsync("ThievesDied");
            return Ok(new { success = true });
        }


        [HttpPost("game-start")]
        public async Task<IActionResult> GameStart([FromBody] GameStartDTO gameStartDTO)
        {
            try
            {
                if (gameStartDTO == null)
                {
                    logger.LogWarning("GameStart called with null DTO");
                    return BadRequest("DTO is null");
                }

                if (string.IsNullOrEmpty(gameStartDTO.SessionID))
                {
                    logger.LogWarning("GameStart missing SessionID");
                    return BadRequest("Missing SessionID");
                }

                if (string.IsNullOrEmpty(gameStartDTO.UnityGUID))
                {
                    logger.LogWarning("GameStart missing UnityGUID");
                    return BadRequest("Missing UnityGUID");
                }

                logger.LogInformation(
                    "GameStart received. SessionId: {SessionId}, UnityGUID: {UnityGUID}",
                    gameStartDTO.SessionID,
                    gameStartDTO.UnityGUID
                );

                linker.AddOrUpdate(gameStartDTO.SessionID, unityGuid: gameStartDTO.UnityGUID);

                await clientHub.Clients.All.SendAsync("GameStart", gameStartDTO.SessionID);

                logger.LogInformation(
                    "GameStart broadcast sent for SessionId: {SessionId}",
                    gameStartDTO.SessionID
                );

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error in GameStart. SessionId: {SessionId}",
                    gameStartDTO?.SessionID);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        [Authorize]
        [HttpPost("save-level")]
        public async Task<IActionResult> SaveLevel([FromBody] PlayerProgressDTO playerProgress)
        {
            try
            {
                // Récupérer username depuis le JWT
                var username = User.FindFirst("username")?.Value;

                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { success = false, message = "Invalid token" });

                var existingUser = await db.Users.Include(u => u.CurrentLevel).FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                if (existingUser == null)
                    return NotFound(new { success = false, message = "User not found" });


                if (existingUser.CurrentLevel != null)
                {
                    // Met à jour le niveau si nécessaire
                    existingUser.CurrentLevel.CurrentLevel = playerProgress.CurrentLevel;
                }
                else
                {
                    existingUser.CurrentLevel = new PlayerProgress
                    {
                        UserId = existingUser.Id,
                        CurrentLevel = playerProgress.CurrentLevel
                    };
                }

                await db.SaveChangesAsync();

                return Ok(new { Success = true, Pseudo = playerProgress.Pseudo, Level = playerProgress.CurrentLevel });
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error in SaveLevel for {Username}",
                    User.FindFirst("username")?.Value);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Database error while saving level"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in SaveLevel");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }


        [Authorize]
        [HttpPost("load-level")]
        public async Task<IActionResult> LoadLevel()
        {
            try
            {
                // Récupérer username depuis le JWT
                var username = User.FindFirst("username")?.Value;

                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { success = false, message = "Invalid token" });

                // Récupérer le joueur
                var player = await db.Users
                    .Include(p => p.CurrentLevel)
                    .FirstOrDefaultAsync(p => p.Username.ToLower() == username.ToLower());

                if (player == null)
                    return NotFound(new { success = false, message = "Player not found" });

                // récupérer connectionId
                var connectionId = Request.Headers["X-Connection-Id"].ToString();
                if (string.IsNullOrEmpty(connectionId))
                    return BadRequest("Missing connectionId");

                // récupérer sessionId
                var sessionId = Request.Headers["X-Session-Id"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                    return BadRequest("Missing sessionId");

                linker.AddOrUpdate(sessionId, angularGuid: connectionId);
                if (!linker.TryGet(sessionId, out var link))
                    return NotFound("Link not found for session");

                // Envoi au jeu via WebSocket
                var payload = new GameMessage();
                payload.Type = "load-level";
                payload.Level = player.CurrentLevel.CurrentLevel;


                var json = JsonSerializer.Serialize(payload);

                bool bSuccess = await webSocketHandler.SendToClient(link.UnityGUID, json);

                if (bSuccess == false)
                {
                    return StatusCode(500, "Failed to send message to client");
                }


                // Réponse API
                return Ok(new
                {
                    success = true,
                    level = player.CurrentLevel.CurrentLevel
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoadLevel for user {Username}", User.FindFirst("username")?.Value);

                return BadRequest(new
                {
                    success = false,
                    message = "An error occured while loading level"
                });
            }
        }

    }

}
