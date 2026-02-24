using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using RiftRoulette.Models;

[ApiController]
[Route("api/[controller]")]
public class RiftController : ControllerBase
{
    private readonly string _connectionString;

    public RiftController(IConfiguration configuration)
    {
        // Lee la conexión configurada para Railway
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet("sync-data")]
    public async Task<IActionResult> Sync()
    {
        var service = new RiotDataService(); // Asegúrate de que el método sea público en dataimporter.cs
        await service.SyncRiotData();
        return Ok("Sincronización con Riot completada.");
    }

    [HttpGet("skins/{userId}")]
    public IActionResult GetSkins(int userId)
    {
        var list = new List<object>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();
        string query = @"
            SELECT s.id_skin_riot, s.nombre_skin, t.nombre as tematica, 
            IF(us.id_usuario IS NULL, 0, 1) as poseida
            FROM Skins s
            JOIN Tematicas t ON s.id_tematica = t.id_tematica
            LEFT JOIN Usuario_Skins us ON s.id_skin_riot = us.id_skin_riot AND us.id_usuario = @uid";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            list.Add(new { 
                id = reader["id_skin_riot"], 
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
        long sid = data.GetProperty("skinId").GetInt64();
        bool owned = data.GetProperty("owned").GetBoolean();

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();
        string query = owned 
            ? "INSERT IGNORE INTO Usuario_Skins (id_usuario, id_skin_riot) VALUES (@uid, @sid)" 
            : "DELETE FROM Usuario_Skins WHERE id_usuario = @uid AND id_skin_riot = @sid";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uid", uid);
        cmd.Parameters.AddWithValue("@sid", sid);
        cmd.ExecuteNonQuery();
        return Ok();
    }
   
}