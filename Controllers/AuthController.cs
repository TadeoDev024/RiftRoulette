using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MySql.Data.MySqlClient;
using RiftRoulette.Models;
using System.Threading.Tasks;
using System;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase 
{
    private readonly string _connectionString;

    public AuthController(IConfiguration configuration) {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequest req) {
        try {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Verificar si el usuario ya existe
            string checkQuery = "SELECT COUNT(*) FROM Usuarios WHERE username = @user";
            using (var cmdCheck = new MySqlCommand(checkQuery, conn)) {
                cmdCheck.Parameters.AddWithValue("@user", req.Username);
                int exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (exists > 0) return BadRequest("El nombre de usuario ya está en uso.");
            }

            // Crear el nuevo usuario
            string query = "INSERT INTO Usuarios (username, password) VALUES (@user, @pass); SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", req.Username);
            cmd.Parameters.AddWithValue("@pass", req.Password); // Nota: En un proyecto real esto iría encriptado (Hash)
            
            int userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            
            var token = GenerateJwtToken(req.Username);
            return Ok(new { token = token, username = req.Username, userId = userId });

        } catch (Exception ex) { return StatusCode(500, "Error en la base de datos: " + ex.Message); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req) {
        try {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string query = "SELECT id_usuario FROM Usuarios WHERE username = @user AND password = @pass";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", req.Username);
            cmd.Parameters.AddWithValue("@pass", req.Password);
            
            var result = await cmd.ExecuteScalarAsync();
            
            if (result != null) {
                int userId = Convert.ToInt32(result);
                return Ok(new { token = GenerateJwtToken(req.Username), username = req.Username, userId = userId });
            }

            return Unauthorized("Usuario o contraseña incorrectos.");
        } catch (Exception ex) { return StatusCode(500, "Error en la base de datos: " + ex.Message); }
    }

    private string GenerateJwtToken(string username) {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("ESTA_ES_UNA_LLAVE_SUPER_SECRETA_Y_LARGA_12345");
        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}