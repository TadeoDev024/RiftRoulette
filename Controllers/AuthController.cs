using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MySqlConnector;
using RiftRoulette.Models;
using RiftRoulette.Helpers;
using System.Threading.Tasks;
using System;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase 
{
    private readonly string _connectionString;
    private readonly string _jwtSecret;

    public AuthController(IConfiguration configuration) {
        _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                            ?? configuration.GetConnectionString("DefaultConnection") ?? "";
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "ESTA_ES_UNA_LLAVE_SUPER_SECRETA_Y_LARGA_12345";
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequest req) {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Usuario y contraseña son obligatorios.");

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

            // Hashear password y crear usuario
            string hash = AuthHelper.HashPassword(req.Password);
            string query = "INSERT INTO Usuarios (username, password) VALUES (@user, @pass); SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", req.Username);
            cmd.Parameters.AddWithValue("@pass", hash);
            
            int userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            
            var token = AuthHelper.GenerateJwtToken(req.Username, userId, _jwtSecret);
            return Ok(new { token = token, username = req.Username, userId = userId });

        } catch (Exception ex) { return StatusCode(500, "Error en la base de datos: " + ex.Message); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req) {
        try {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Buscar usuario y verificar hash
            string query = "SELECT id_usuario, password FROM Usuarios WHERE username = @user";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", req.Username);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                int userId = reader.GetInt32(reader.GetOrdinal("id_usuario"));
                string passwordHash = reader.GetString(reader.GetOrdinal("password"));
                if (AuthHelper.VerifyPassword(req.Password, passwordHash)) {
                    var token = AuthHelper.GenerateJwtToken(req.Username, userId, _jwtSecret);
                    return Ok(new { token = token, username = req.Username, userId = userId });
                }
            }

            return Unauthorized("Usuario o contraseña incorrectos.");
        } catch (Exception ex) { return StatusCode(500, "Error en la base de datos: " + ex.Message); }
    }
}