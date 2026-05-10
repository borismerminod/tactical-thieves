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

    /// <summary>
    /// GameController is responsible for handling game-related API endpoints. 
    /// It acts as a bridge between the Unity client (via WebSocket) and the Angular client (via SignalR). 
    /// It also interacts with the database to save and load player progress. 
    /// Each endpoint corresponds to a specific game action or event, allowing for real-time communication 
    /// and synchronization between clients and the server.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {

        //This service is used to send messages to the Unity client via WebSocket
        private readonly WebSocketHandler webSocketHandler;

        // This service is used to send messages to the Angular client via SignalR
        private readonly IHubContext<ClientHub> clientHub;

        //This object is used to interact with the database
        private readonly ApplicationDbContext db;

        // This service is used to link Unity clients and Angular clients via sessionId
        private readonly WebSocketLinkerService linker;

        private readonly ILogger<GameController> logger;

        public GameController(WebSocketHandler webSocketHandler, IHubContext<ClientHub> clientHub, ApplicationDbContext db, WebSocketLinkerService linker, ILogger<GameController> logger)
        {
            this.webSocketHandler = webSocketHandler;
            this.clientHub = clientHub;
            this.db = db;
            this.linker = linker;
            this.logger = logger;
        }

        /// <summary>
        /// Sends a game message of the specified type to the connected client associated with the current session : 
        ///     - Retrieves the sessionId from the request headers and uses it to find the corresponding client link.
        ///     - Constructs a GameMessage object with the specified type and a default level of 0.
        ///     - Sends the message to the Unity client via WebSocket service.
        /// </summary>
        /// <param name="messageType">The type of game message to send.</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
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

                bool success = await webSocketHandler.SendToClient(link?.UnityGUID, json);

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

        /// <summary>
        /// The endpoint is called when player click on angular move button, it sends a "move" 
        /// message to the Unity client via WebSocket, which will trigger the corresponding action.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("move")]
        public Task<IActionResult> Move()
        {
            return SendGameMessageAsync("move");
        }

        /// <summary>
        /// The endpoint is called when player click on angular stealth button, it sends a "stealth" 
        /// message to the Unity client via WebSocket, which will trigger the corresponding action.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("stealth")]
        public Task<IActionResult> Stealth()
        {
            return SendGameMessageAsync("stealth");
        }

        /// <summary>
        /// The endpoint is called when player click on angular end turn button, it sends a "end-turn" 
        /// message to the Unity client via WebSocket, which will trigger player end turn.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("end-turn")]
        public Task<IActionResult> EndTurn()
        {
            return SendGameMessageAsync("end-turn");
        }

        /// <summary>
        /// The endpoint is called when player click on angular restart button, it sends a "restart" 
        /// message to the Unity client via WebSocket, which will restart the game level.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("restart")]
        public Task<IActionResult> Restart()
        {
            return SendGameMessageAsync("restart");
        }

        /// <summary>
        /// The endpoint is called for anonymous users. A random level is loaded when this endpoint is called :
        ///     - Retrieves the sessionId and connectionId from the request headers and uses them to find the corresponding client link.
        ///     - Constructs a GameMessage object with the type "load-random-level" and a default level of 0 (level value is not used here).
        ///     - Sends the message to the Unity client via WebSocket.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("load-random-level")]
        public async Task<IActionResult> LoadRandomLevel()
        {
            try
            {
                var connectionId = Request.Headers["X-Connection-Id"].ToString();
                if (string.IsNullOrEmpty(connectionId))
                    return BadRequest("Missing connectionId");

                var sessionId = Request.Headers["X-Session-Id"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                    return BadRequest("Missing sessionId");

                linker.AddOrUpdate(sessionId, angularGuid: connectionId);
                if (!linker.TryGet(sessionId, out var link))
                    return NotFound("Link not found for session");

                GameMessage gameMessage = new GameMessage
                {
                    Type = "load-random-level",
                    Level = 0
                };
                var json = JsonSerializer.Serialize(gameMessage);

                bool bSuccess = await webSocketHandler.SendToClient(link?.UnityGUID, json);

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

        /// <summary>
        /// This method is a generic handler for client events that follow a common pattern of validation, payload extraction, 
        /// and message sending using signalR : 
        ///     - First it retrieves the sessionId from the request headers and uses it to find the corresponding client link.
        ///     - Then it validates the incoming DTO using the provided validation function. 
        ///       If validation fails, it returns a BadRequest with the specified error message.
        ///     - If validation succeeds, it extracts the payload from the DTO using the provided payload selector function.
        ///     - Finally, it sends the payload to the client using SignalR and returns a success response.
        /// </summary>
        /// <typeparam name="T">The type of the DTO being processed.</typeparam>
        /// <param name="dto">The data transfer object containing the event data.</param>
        /// <param name="isValid">A function to validate the DTO.</param>
        /// <param name="validationErrorMessage">The error message to return if validation fails.</param>
        /// <param name="eventName">The name of the event to send to the client.</param>
        /// <param name="payloadSelector">A function to extract the payload from the DTO.</param>
        /// <param name="successResponse">A function to generate the success response.</param>
        /// <returns>An IActionResult representing the outcome of the operation.</returns>
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

                if (link == null ||link.AngularGUID == null)
                    return NotFound("Link not found for session");

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

        /// <summary>
        /// The endpoint is called from unity game when player collect treasure, it sends a "ScoreUpdated" message to the Angular client via SignalR
        /// </summary>
        /// <param name="dto">The data transfer object containing the treasure collection information.</param>
        /// <returns>An IActionResult representing the outcome of the operation.</returns>
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

        /// <summary>
        /// The endpoint is called from unity game when player reach the exit, it sends a "ExitReached" message to the Angular client via SignalR
        /// </summary>
        /// <param name="playerProgress">The data transfer object containing the player's progress information.</param>
        /// <returns>An IActionResult representing the outcome of the operation.</returns>
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

        /// <summary>
        /// The endpoint is called from unity game when player die, it sends a "ThievesDied" message to the Angular client via SignalR : 
        ///     - First it retrieves the sessionId from the request headers and uses it to find the corresponding client link.
        ///     - Then it sends a "ThievesDied" message to the Angular client using SignalR, which will trigger the corresponding action in the 
        ///     Angular application (e.g., display a game over screen).
        ///     - Finally, it returns a success response to indicate that the message was sent successfully.
        /// </summary>
        /// <returns>An IActionResult representing the outcome of the operation.</returns>
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

            if(link == null || link.AngularGUID == null)
                return NotFound("Link not found for session");

            clientHub.Clients.Client(link.AngularGUID).SendAsync("ThievesDied");
            return Ok(new { success = true });
        }

        /// <summary>
        /// This endpoint is called from unity game when player start a game, it sends a "GameStart" message to the Angular client via SignalR :
        ///     - It receives a GameStartDTO object from the request body, which contains the sessionId and unityGuid.
        ///     - It validates the DTO to ensure that both sessionId and unityGuid are present. 
        ///       If validation fails, it returns a BadRequest with an appropriate error message.
        ///     - If validation succeeds, it updates the WebSocketLinkerService with the new sessionId and unityGuid, 
        ///       allowing the server to link the Unity client with the Angular client for this session.
        ///     - Finally, it sends a "GameStart" message to the Angular client using SignalR, which will trigger 
        ///     the corresponding action in the Angular application (e.g., display the game UI).
        /// </summary>
        /// <param name="gameStartDTO">The DTO containing the sessionId and unityGuid.</param>
        /// <returns>An IActionResult representing the outcome of the operation.</returns>
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

        /// <summary>
        /// The endpoint is called from angular when player win a level. The next level is send by playerProgress DTO and is saved in database, 
        /// so when player will come back to the game, he will start from the next level.
        ///     - First it retrieves the username from the JWT token to identify the user making the request. User have to be authenticated to access this endpoint.
        ///     - Then it looks up the user in the database using the retrieved username. If the user is not found, it returns a NotFound response.
        ///     - If the user is found, it checks if the user already has a PlayerProgress record. If it does, it updates the current level.
        ///     - If it does not, it creates a new PlayerProgress record for the user with the current level from the DTO.
        ///     - Finally, it saves the changes to the database and returns a success response with the updated level information.
        /// </summary>
        /// <param name="playerProgress">The DTO containing the player's progress information.</param>
        /// <returns>An IActionResult representing the outcome of the operation.</returns>
        [Authorize]
        [HttpPost("save-level")]
        public async Task<IActionResult> SaveLevel([FromBody] PlayerProgressDTO playerProgress)
        {
            try
            {
                var username = User.FindFirst("username")?.Value;

                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { success = false, message = "Invalid token" });

                var existingUser = await db.Users.Include(u => u.CurrentLevel).FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                if (existingUser == null)
                    return NotFound(new { success = false, message = "User not found" });


                if (existingUser.CurrentLevel != null)
                {
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


        /// <summary>
        /// This endpoint is called from angular when player start the game, 
        /// it retrieves the current level of the player from database and send it to unity client via WebSocket, 
        /// so player can start from the last level he reached instead of starting from level 0.
        /// The user must be authenticated to access this endpoint, and the username is extracted from the JWT token to identify the user : 
        ///     - First it retrieves the username from the JWT token to identify the user making the request.
        ///     - Then it looks up the user in the database using the retrieved username. If the user is not found, it returns a NotFound response.
        ///     - If the user is found, it retrieves the current level from the user's PlayerProgress record.
        ///     - It also retrieves the connectionId and sessionId from the request headers to find the corresponding client link.
        ///     - Finally, it constructs a GameMessage with the current level and sends it to the Unity client via WebSocket, 
        ///       allowing the player to start from their last saved level.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
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

                bool bSuccess = await webSocketHandler.SendToClient(link?.UnityGUID, json);

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
