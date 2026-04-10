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
        private readonly ApplicationDbContext db; // ajout du DbContext
        private readonly WebSocketLinkerService linker;

        public GameController(ThiefStateService thiefState, WebSocketHandler webSocketHandler, IHubContext<ClientHub> clientHub, ApplicationDbContext db, WebSocketLinkerService linker)
        {
            this.thiefState = thiefState;
            this.webSocketHandler = webSocketHandler;
            this.clientHub = clientHub;
            this.db = db;
            this.linker = linker;
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

        [HttpPost("end-turn")]
        public IActionResult EndTurn()
        {
            // récupérer sessionId depuis header
            var sessionId = Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest("Missing sessionId");

            // récupérer le lien
            if (!linker.TryGet(sessionId, out var link))
                return NotFound("Link not found for session");

            GameMessage gameMessage = new GameMessage
            {
                Type = "end-turn",
                Level = 0
            };

            var json = JsonSerializer.Serialize(gameMessage);

            webSocketHandler.SendToClient(link.UnityGUID, json);
            //webSocketHandler.Broadcast(json);
            return Ok(new { success = true });
        }

        [HttpPost("restart")]
        public IActionResult Restart()
        {

            // récupérer sessionId depuis header
            var sessionId = Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest("Missing sessionId");

            // récupérer le lien
            if (!linker.TryGet(sessionId, out var link))
                return NotFound("Link not found for session");


            GameMessage gameMessage = new GameMessage
            {
                Type = "restart",
                Level = 0
            };

            var json = JsonSerializer.Serialize(gameMessage);

            webSocketHandler.SendToClient(link.UnityGUID, json);

            //webSocketHandler.Broadcast(json);
            return Ok(new { success = true });

        }

        [HttpPost("collect-treasure")]
        public IActionResult CollectTreasure([FromBody] TreasureCollectDTO dto)
        {
            // récupérer sessionId depuis header
            var sessionId = Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest("Missing sessionId");

            if (dto == null || dto.Amount <= 0)
                return BadRequest(new { success = false, message = "Invalid treasure amount" });

            // récupérer le lien
            if (!linker.TryGet(sessionId, out var link))
                return NotFound("Link not found for session");


            clientHub.Clients.Client(link.AngularGUID).SendAsync("ScoreUpdated", dto.Amount);
            //clientHub.SendPlayerGoldUpdate(dto.Amount);
            //clientHub.Clients.All.SendAsync("ScoreUpdated", dto.Amount);

            return Ok(new { success = true, gold = dto.Amount });
        }

        [HttpPost("exit-reached")]
        public IActionResult ExitReached([FromBody] PlayerProgressDTO playerProgress)
        {
            // récupérer sessionId depuis header
            var sessionId = Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest("Missing sessionId");

            // récupérer le lien
            if (!linker.TryGet(sessionId, out var link))
                return NotFound("Link not found for session");

            clientHub.Clients.Client(link.AngularGUID).SendAsync("ExitReached", playerProgress.CurrentLevel);
            
            //clientHub.Clients.All.SendAsync("ExitReached", playerProgress.CurrentLevel);
            return Ok(new { success = true });
        }

        /*[HttpPost("game-start")]
        public IActionResult GameStart([FromBody] GameStartDTO gameStartDTO)
        {
            linker.AddOrUpdate(gameStartDTO.SessionID, unityGuid: gameStartDTO.UnityGUID);
            clientHub.Clients.All.SendAsync("GameStart", gameStartDTO.SessionID); 
            return Ok(new { success = true });
        }*/

        [HttpPost("game-start")]
        public async Task<IActionResult> GameStart([FromBody] GameStartDTO gameStartDTO)
        {
            try
            {
                //Vérification du body
                if (gameStartDTO == null)
                    return BadRequest("DTO is null");

                if (string.IsNullOrEmpty(gameStartDTO.SessionID))
                    return BadRequest("Missing SessionID");

                if (string.IsNullOrEmpty(gameStartDTO.UnityGUID))
                    return BadRequest("Missing UnityGUID");

                // Debug (très important)
                Console.WriteLine($"[GameStart] Session: {gameStartDTO.SessionID} | Unity: {gameStartDTO.UnityGUID}");

                // Enregistrement
                linker.AddOrUpdate(gameStartDTO.SessionID, unityGuid: gameStartDTO.UnityGUID);

                // Broadcast vers Angular
                await clientHub.Clients.All.SendAsync("GameStart", gameStartDTO.SessionID);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameStart ERROR] {ex.Message}");

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
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
            //clientHub.Clients.All.SendAsync("ThievesDied");
            return Ok(new { success = true });
        }

        [HttpPost("load-random-level")]
        public IActionResult LoadRandomLevel()
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

                webSocketHandler.SendToClient(link.UnityGUID, json);
                //webSocketHandler.Broadcast(json);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("save-level")]
        public async Task<IActionResult> SaveLevel([FromBody] PlayerProgressDTO playerProgress)
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
                //db.Users.Update(existingUser);
                //db.PlayerProgresses.Update(existing);
            }
            else
            {
                existingUser.CurrentLevel = new PlayerProgress
                {
                    UserId = existingUser.Id,
                    CurrentLevel = playerProgress.CurrentLevel
                };

                //db.Users.Update(existingUser);
                // Ajoute une nouvelle progression
                //db.PlayerProgresses.Add(playerProgress);
            }

            await db.SaveChangesAsync();

            // Notifie les clients (optionnel)
            //await clientHub.Clients.All.SendAsync("PlayerProgressSaved", new { pseudo = playerProgress.Pseudo, level = playerProgress.CurrentLevel });

            return Ok(new { Success = true, Pseudo = playerProgress.Pseudo, Level = playerProgress.CurrentLevel });
        }

        // Récupère le niveau courant d'un joueur par son pseudo (insensible à la casse côté SQL via LOWER)
       /* [HttpGet("load-level/{pseudo}")]
        public async Task<IActionResult> LoadLevel(string pseudo)
        {
            if (string.IsNullOrWhiteSpace(pseudo))
                return BadRequest(new { success = false, message = "Pseudo is required" });

            // Utilise LOWER() pour une recherche insensible à la casse au niveau SQL
            var player = await db.Users
                                 .FirstOrDefaultAsync(p => p.Username.ToLower() == pseudo.ToLower());

            if (player == null)
                return NotFound(new { success = false, message = "Player not found" });

            return Ok(new { Success = true, ID = player.Id, Pseudo = player.Username, Level = player.CurrentLevel.CurrentLevel });
        }

        // Récupère le niveau courant d'un joueur par son Id
        [HttpGet("load-level/{id:int}")]
        public async Task<IActionResult> GetLevelById(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid id" });

            var player = await db.Users.FindAsync(id);

            if (player == null)
                return NotFound(new { success = false, message = "Player not found" });

            return Ok(new { Success = true, ID = player.Id, Pseudo = player.Username, Level = player.CurrentLevel.CurrentLevel });
        }*/

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

                webSocketHandler.SendToClient(link.UnityGUID, json);

                //await webSocketHandler.BroadcastAsync(payload);
                //webSocketHandler.Broadcast(json);

                // Réponse API
                return Ok(new
                {
                    success = true,
                    level = player.CurrentLevel.CurrentLevel
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }

    public class  PlayerProgressDTO
    {
        public string Pseudo { get; set; }
        public int CurrentLevel { get; set; }
    }

    public class GameMessage
    {
        public string Type { get; set; }
        public int Level { get; set; }
    }

    public class GameStartDTO
    {
        public string SessionID { get; set; }
        public string UnityGUID { get; set; }
    }

}
