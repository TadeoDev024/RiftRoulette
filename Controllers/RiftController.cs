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
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [HttpGet("sync-data")]
    public async Task<IActionResult> Sync()
    {
        var service = new RiotDataService();
        await service.SyncRiotData();
        return Ok("Sincronización con Riot completada.");
    }

    [HttpGet("skins/{userId}")]
    public IActionResult GetSkins(int userId)
    {
        var list = new List<object>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();
        
        // Corregido: id_skin_riot para coincidir con el resto del código
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
                id = reader["id_skin_riot"].ToString(), 
                nombre = reader["nombre_skin"].ToString(), 
                tema = reader["tematica"].ToString(),
                owned = Convert.ToBoolean(reader["poseida"])
            });
        }
        return Ok(list);
    }

    [HttpPost("inventory/toggle")]
    public IActionResult ToggleSkin([FromBody] System.Text.Json.JsonElement data)
    {
        // Corregido el acceso a las propiedades del JSON
        int uid = data.GetProperty("userId").GetInt32();
        string sid = data.GetProperty("skinId").GetString() ?? "";
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