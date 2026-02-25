using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

[ApiController]
[Route("api/[controller]")]
public class RiftController : ControllerBase
{
    private readonly string _connectionString;

    public RiftController(IConfiguration configuration) {
        // Prioridad a la variable de entorno de Render
        _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                            ?? configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserDto model) {
        try {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = "SELECT id_usuario, password FROM Usuarios WHERE username = @u";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", model.Username);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                if (reader.GetString(reader.GetOrdinal("password")) == model.Password) {
                    return Ok(new { userId = reader.GetInt32(reader.GetOrdinal("id_usuario")), message = "OK" });
                }
            }
            return BadRequest(new { message = "Usuario o contraseña incorrectos" });
        } catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserDto model) {
        try {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = "INSERT INTO Usuarios (username, password) VALUES (@u, @p)";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", model.Username);
            cmd.Parameters.AddWithValue("@p", model.Password);
            await cmd.ExecuteNonQueryAsync();
            return await Login(model); 
        } catch (Exception ex) { return StatusCode(500, "Error: El usuario ya existe o error de conexión."); }
    }

    [HttpGet("skins/{userId}")]
    public async Task<IActionResult> GetSkins(int userId) {
        var list = new List<object>();
        try {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = @"
                SELECT s.id_skin_riot, s.nombre_skin, s.campeon, s.campeon_id, t.nombre as tematica, 
                IF(us.id_usuario IS NULL, 0, 1) as poseida
                FROM Skins s
                JOIN Tematicas t ON s.id_tematica = t.id_tematica
                LEFT JOIN Usuario_Skins us ON s.id_skin_riot = us.id_skin_riot AND us.id_usuario = @uid";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync()) {
                list.Add(new { 
                    id = reader["id_skin_riot"].ToString(), 
                    nombre = reader["nombre_skin"].ToString(), 
                    campeon = reader["campeon"].ToString(), 
                    campeonId = reader["campeon_id"].ToString(),
                    tema = reader["tematica"].ToString(),
                    owned = Convert.ToBoolean(reader["poseida"])
                });
            }
            return Ok(list);
        } catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("inventory/toggle")]
    public async Task<IActionResult> ToggleSkin([FromBody] JsonElement data) {
        try {
            int uid = data.GetProperty("userId").GetInt32();
            string sid = data.GetProperty("skinId").GetString() ?? "";
            bool owned = data.GetProperty("owned").GetBoolean();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = owned 
                ? "INSERT IGNORE INTO Usuario_Skins (id_usuario, id_skin_riot) VALUES (@uid, @sid)" 
                : "DELETE FROM Usuario_Skins WHERE id_usuario = @uid AND id_skin_riot = @sid";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@uid", uid);
            cmd.Parameters.AddWithValue("@sid", sid);
            await cmd.ExecuteNonQueryAsync();
            return Ok();
        } catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
}

public class UserDto {
    public string Username { get; set; }
    public string Password { get; set; }
}