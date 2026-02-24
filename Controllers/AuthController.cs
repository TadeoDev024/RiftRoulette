using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RiftRoulette.Models; // Esto vincula el archivo que creamos arriba
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req) {
        // Verificación en DB...
        var token = GenerateJwtToken(user);
        return Ok(new { token, username = user.Username, userId = user.Id });
    }
}