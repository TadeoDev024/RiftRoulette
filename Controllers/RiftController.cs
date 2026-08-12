using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using RiftRoulette.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiftRoulette.Controllers
{
    [ApiController]
    [Route("api/Rift")]
    public class RiftController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _jwtSecret;

        public RiftController(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                ?? configuration.GetConnectionString("DefaultConnection") ?? "";
            _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "ESTA_ES_UNA_LLAVE_SUPER_SECRETA_Y_LARGA_12345";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { message = "Datos vacíos" });

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                string query = "SELECT id_usuario, password FROM Usuarios WHERE username = @u";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", model.Username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int userId = reader.GetInt32(reader.GetOrdinal("id_usuario"));
                    string passwordHash = reader.GetString(reader.GetOrdinal("password"));
                    if (AuthHelper.VerifyPassword(model.Password, passwordHash))
                    {
                        var token = AuthHelper.GenerateJwtToken(model.Username, userId, _jwtSecret);
                        return Ok(new { token = token, userId = userId, message = "OK" });
                    }
                }
                return BadRequest(new { message = "Usuario o contraseña incorrectos" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error de conexión: " + ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { message = "Datos vacíos" });

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM Usuarios WHERE username = @u";
                using var cmdCheck = new MySqlCommand(checkQuery, conn);
                cmdCheck.Parameters.AddWithValue("@u", model.Username);
                int exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (exists > 0)
                    return BadRequest(new { message = "El usuario ya existe" });

                string hash = AuthHelper.HashPassword(model.Password);
                string query = "INSERT INTO Usuarios (username, password) VALUES (@u, @p); SELECT LAST_INSERT_ID();";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", model.Username);
                cmd.Parameters.AddWithValue("@p", hash);
                int userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                var token = AuthHelper.GenerateJwtToken(model.Username, userId, _jwtSecret);
                return Ok(new { token = token, userId = userId, message = "OK" });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "El usuario ya existe" });
            }
        }

        [Authorize]
        [HttpGet("skins/{userId}")]
        public async Task<IActionResult> GetSkins(int userId)
        {
            var list = new List<object>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                string query = @"
                    SELECT s.id_skin_riot, s.nombre_skin, s.campeon, s.campeon_id, t.nombre as tematica, 
                    IF(us.id_usuario IS NULL, 0, 1) as poseida
                    FROM Skins s
                    JOIN Tematicas t ON s.id_tematica = t.id_tematica
                    LEFT JOIN Usuario_Skins us ON us.id_skin_riot = s.id_skin_riot AND us.id_usuario = @uid";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id_skin_riot"].ToString(),
                        nombre = reader["nombre_skin"].ToString(),
                        campeon = reader["campeon"].ToString(),
                        campeonId = reader["campeon_id"].ToString(),
                        tema = reader["tematica"].ToString(),
                        owned = Convert.ToBoolean(reader["poseida"])
                    });
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [Authorize]
        [HttpPost("inventory/toggle")]
        public async Task<IActionResult> ToggleSkin([FromBody] JsonElement data)
        {
            try
            {
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
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }
    }

    public class UserDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}