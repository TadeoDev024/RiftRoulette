using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RiftRoulette.Models; // Esto vincula el archivo que creamos arriba
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LobbyController : ControllerBase
{
    // Diccionario estático para mantener las salas en memoria (Singleton)
    private static readonly Dictionary<string, List<PlayerDto>> Lobbies = new();

    [HttpPost("create")]
    public IActionResult CreateLobby()
    {
        string code = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        Lobbies[code] = new List<PlayerDto>();
        return Ok(new { lobbyCode = code });
    }

    [HttpPost("join/{code}")]
    public IActionResult JoinLobby(string code, [FromBody] PlayerDto player)
    {
        if (!Lobbies.ContainsKey(code)) return NotFound("Sala no encontrada");
        if (Lobbies[code].Count >= 5) return BadRequest("Sala llena");
        
        if (!Lobbies[code].Any(p => p.UserId == player.UserId))
            Lobbies[code].Add(player);

        return Ok(new { players = Lobbies[code] });
    }

    [HttpGet("status/{code}")]
    public IActionResult GetStatus(string code)
    {
        if (!Lobbies.ContainsKey(code)) return NotFound();
        return Ok(new { players = Lobbies[code] });
    }
}

public class PlayerDto {
    public int UserId { get; set; }
    public string Username { get; set; }
}