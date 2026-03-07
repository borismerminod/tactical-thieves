using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TacticalThievesServer.DTO;
using TacticalThievesServer.Models;
using TacticalThievesServer.Services;
using TacticalThievesServer.Data;

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

        public GameController(ThiefStateService thiefState, WebSocketHandler webSocketHandler, IHubContext<ClientHub> clientHub, ApplicationDbContext db)
        {
            this.thiefState = thiefState;
            this.webSocketHandler = webSocketHandler;
            this.clientHub = clientHub;
            this.db = db;
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
            webSocketHandler.Broadcast("end-turn");
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

        [HttpPost("exit-reached")]
        public IActionResult ExitReached()
        {
            clientHub.Clients.All.SendAsync("ExitReached");
            return Ok(new { success = true });
        }

        [HttpPost("game-start")]
        public IActionResult GameStart()
        {
            clientHub.Clients.All.SendAsync("GameStart");
            return Ok(new { success = true });
        }

        [HttpPost("thieves-died")]
        public IActionResult ThievesDied()
        {
            clientHub.Clients.All.SendAsync("ThievesDied");
            return Ok(new { success = true });
        }

        [HttpPost("save-level")]
        public async Task<IActionResult> SaveLevel([FromBody] PlayerProgress playerProgress)
        {
            if (playerProgress == null || string.IsNullOrWhiteSpace(playerProgress.Pseudo))
                return BadRequest(new { success = false, message = "Invalid player data" });

            // Cherche une progression existante par pseudo
            var existing = await db.PlayerProgresses
                                   .FirstOrDefaultAsync(p => p.Pseudo == playerProgress.Pseudo);

            if (existing != null)
            {
                // Met à jour le niveau si nécessaire
                existing.CurrentLevel = playerProgress.CurrentLevel;
                db.PlayerProgresses.Update(existing);
            }
            else
            {
                // Ajoute une nouvelle progression
                db.PlayerProgresses.Add(playerProgress);
            }

            await db.SaveChangesAsync();

            // Notifie les clients (optionnel)
            //await clientHub.Clients.All.SendAsync("PlayerProgressSaved", new { pseudo = playerProgress.Pseudo, level = playerProgress.CurrentLevel });

            return Ok(new { Success = true, Pseudo = playerProgress.Pseudo, Level = playerProgress.CurrentLevel });
        }

        // Récupère le niveau courant d'un joueur par son pseudo (insensible à la casse côté SQL via LOWER)
        [HttpGet("load-level/{pseudo}")]
        public async Task<IActionResult> LoadLevel(string pseudo)
        {
            if (string.IsNullOrWhiteSpace(pseudo))
                return BadRequest(new { success = false, message = "Pseudo is required" });

            // Utilise LOWER() pour une recherche insensible à la casse au niveau SQL
            var player = await db.PlayerProgresses
                                 .FirstOrDefaultAsync(p => p.Pseudo.ToLower() == pseudo.ToLower());

            if (player == null)
                return NotFound(new { success = false, message = "Player not found" });

            return Ok(new { Success = true, ID = player.Id, Pseudo = player.Pseudo, Level = player.CurrentLevel });
        }

        // Récupère le niveau courant d'un joueur par son Id
        [HttpGet("load-level/{id:int}")]
        public async Task<IActionResult> GetLevelById(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid id" });

            var player = await db.PlayerProgresses.FindAsync(id);

            if (player == null)
                return NotFound(new { success = false, message = "Player not found" });

            return Ok(new { Success = true, ID = player.Id, Pseudo = player.Pseudo, Level = player.CurrentLevel });
        }
    }
}
