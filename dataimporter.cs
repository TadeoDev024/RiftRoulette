using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Net.Http;

public class RiotDataService {
    // Usamos la cadena de conexión oficial de tu configuración de Railway
    private readonly string _connectionString = "Server=ballast.proxy.rlwy.net;Port=38239;Database=railway;Uid=root;Pwd=moBtcnWVmvOzjozxaxNqkuxvyoXOWMbR;";

    public async Task SyncRiotData() {
        using var client = new HttpClient();
        
        // 1. Obtener la última versión del juego
        var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
        string latestVersion = JArray.Parse(versionJson)[0].ToString();
        
        // 2. Obtener lista de campeones
        var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
        var champions = JObject.Parse(champsRaw)["data"];

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var champProp in champions) {
            string champId = champProp.First["id"].ToString();
            
            // 3. Obtener detalle de skins por campeón
            var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
            var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

            foreach (var skin in skinData) {
                string skinName = skin["name"].ToString();
                string skinIdRiot = skin["id"].ToString();
                
                // Lógica simple para asignar temáticas basadas en el nombre
                string tema = "Clásica";
                if (skinName.ToLower().Contains("proyecto")) tema = "PROYECTO";
                else if (skinName.ToLower().Contains("luna sangrienta")) tema = "Luna Sangrienta";
                else if (skinName.ToLower().Contains("aspecto de")) tema = "Legado";

                // EJECUCIÓN REAL DEL GUARDADO
                await SaveSkinToDb(conn, skinIdRiot, skinName, tema);
            }
        }
    }

    private async Task SaveSkinToDb(MySqlConnection conn, string skinId, string nombre, string tema) {
        // A. Asegurar que la temática existe y obtener su ID
        string queryTema = @"
            INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema);
            SELECT id_tematica FROM Tematicas WHERE nombre = @tema LIMIT 1;";
        
        int temaId;
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            temaId = Convert.ToInt32(await cmdTema.ExecuteScalarAsync());
        }

        // B. Insertar la Skin vinculada a la temática
        string querySkin = @"
            INSERT IGNORE INTO Skins (id_skin_riot, id_tematica, nombre_skin) 
            VALUES (@sid, @tid, @nombre)";
        
        using (var cmdSkin = new MySqlCommand(querySkin, conn)) {
            cmdSkin.Parameters.AddWithValue("@sid", skinId);
            cmdSkin.Parameters.AddWithValue("@tid", temaId);
            cmdSkin.Parameters.AddWithValue("@nombre", nombre);
            await cmdSkin.ExecuteNonQueryAsync();
        }
    }
}