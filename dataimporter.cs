using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Net.Http;

public class RiotDataService {
    // Cadena de conexión directa a tu instancia de Railway
    private readonly string _connectionString = "Server=ballast.proxy.rlwy.net;Port=38239;Database=railway;Uid=root;Pwd=moBtcnWVmvOzjozxaxNqkuxvyoXOWMbR;";

    public async Task SyncRiotData() {
        using var client = new HttpClient();
        
        try {
            // 1. Obtener versión
            var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
            string latestVersion = JArray.Parse(versionJson)[0].ToString();
            
            // 2. Obtener campeones
            var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
            var champions = JObject.Parse(champsRaw)["data"];

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                
                // 3. Detalle de skins
                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinName = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    
                    // Lógica de temáticas
                    string tema = "Clásica";
                    if (skinName.ToLower().Contains("proyecto")) tema = "PROYECTO";
                    else if (skinName.ToLower().Contains("luna sangrienta")) tema = "Luna Sangrienta";
                    else if (skinName.ToLower().Contains("aspecto de")) tema = "Legado";

                    await SaveSkinToDb(conn, skinIdRiot, skinName, tema);
                }
            }
        } catch (Exception ex) {
            // Esto imprimirá el error real en la consola de Railway
            Console.WriteLine("ERROR EN SYNC: " + ex.Message);
            throw; 
        }
    }

    private async Task SaveSkinToDb(MySqlConnection conn, string skinId, string nombre, string tema) {
        // A. Insertar temática y obtener ID
        int temaId;
        string queryTema = "INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema); SELECT id_tematica FROM Tematicas WHERE nombre = @tema LIMIT 1;";
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            var result = await cmdTema.ExecuteScalarAsync();
            temaId = Convert.ToInt32(result);
        }

        // B. Insertar Skin (Asegúrate que las columnas se llamen id_skin_riot y nombre_skin)
        string querySkin = "INSERT IGNORE INTO Skins (id_skin_riot, id_tematica, nombre_skin) VALUES (@sid, @tid, @nombre)";
        using (var cmdSkin = new MySqlCommand(querySkin, conn)) {
            cmdSkin.Parameters.AddWithValue("@sid", skinId);
            cmdSkin.Parameters.AddWithValue("@tid", temaId);
            cmdSkin.Parameters.AddWithValue("@nombre", nombre);
            await cmdSkin.ExecuteNonQueryAsync();
        }
    }
}