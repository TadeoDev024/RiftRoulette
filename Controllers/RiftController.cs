using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
public class RiftController : ControllerBase
{
    private string connectionString = "Server=localhost;Database=RiftRoulette;Uid=root;Pwd=TU_CONTRASEÑA;";

    [HttpPost("login")]
    public IActionResult Login([FromBody] dynamic data)
    {
        string user = data.GetProperty("username").GetString();
        // Nota: En producción usa BCrypt para comparar hashes
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        var cmd = new MySqlCommand("SELECT id_usuario FROM Usuarios WHERE username = @u", conn);
        cmd.Parameters.AddWithValue("@u", user);
        var id = cmd.ExecuteScalar();
        
        if (id != null) return Ok(new { userId = id });
        return Unauthorized();
    }

    [HttpGet("skins/{userId}")]
    public IActionResult GetSkins(int userId)
    {
        var list = new List<object>();
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        // Trae todas las skins e indica si el usuario las tiene (LEFT JOIN)
        string query = @"
            SELECT s.id_skin, s.nombre_skin, t.nombre as tematica, 
            IF(us.id_usuario IS NULL, 0, 1) as poseida
            FROM Skins s
            JOIN Tematicas t ON s.id_tematica = t.id_tematica
            LEFT JOIN Usuario_Skins us ON s.id_skin = us.id_skin AND us.id_usuario = @uid";
        
        var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            list.Add(new { 
                id = reader["id_skin"], 
                nombre = reader["nombre_skin"], 
                tema = reader["tematica"],
                owned = reader.GetBoolean("poseida")
            });
        }
        return Ok(list);
    }

    [HttpPost("inventory/toggle")]
    public IActionResult ToggleSkin([FromBody] dynamic data)
    {
        int uid = data.GetProperty("userId").GetInt32();
        int sid = data.GetProperty("skinId").GetInt32();
        bool owned = data.GetProperty("owned").GetBoolean();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        string query = owned 
            ? "INSERT IGNORE INTO Usuario_Skins VALUES (@uid, @sid)" 
            : "DELETE FROM Usuario_Skins WHERE id_usuario = @uid AND id_skin = @sid";
        
        var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uid", uid);
        cmd.Parameters.AddWithValue("@sid", sid);
        cmd.ExecuteNonQuery();
        return Ok();
    }
}