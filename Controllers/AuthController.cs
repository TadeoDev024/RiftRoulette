using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RiftRoulette.Models;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase 
{
    // 1. Simulación de Login (Para que el build pase y puedas probar)
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req) 
    {
        // NOTA: Aquí es donde buscarías en MySQL. 
        // Por ahora, creamos un objeto 'user' para que el compilador no de error.
        var user = new { 
            Id = 1, 
            Username = req.Username, 
            Password = req.Password 
        };

        if (user.Username == "admin") // Validación simple de prueba
        {
            var token = GenerateJwtToken(user.Username);
            return Ok(new { 
                token = token, 
                username = user.Username, 
                userId = user.Id 
            });
        }

        return Unauthorized("Usuario o contraseña incorrectos");
    }

    // 2. Definición del método que genera el Token (ESTO FALTABA)
    private string GenerateJwtToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        // IMPORTANTE: Esta clave debe tener al menos 32 caracteres
        var key = Encoding.ASCII.GetBytes("ESTA_ES_UNA_LLAVE_SUPER_SECRETA_Y_LARGA_12345");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature
            )
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}