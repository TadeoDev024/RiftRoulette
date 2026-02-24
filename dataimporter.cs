using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Net.Http;

public class RiotDataService {
    // Usamos la cadena de conexión de tu appsettings.json
    private readonly string _connectionString = "Server=ballast.proxy.rlwy.net;Port=38239;Database=railway;Uid=root;Pwd=moBtcnWVmvOzjozxaxNqkuxvyoXOWMbR;";

    public async Task SyncRiotData() {
        using var client = new HttpClient();
        
        // 1. Obtener la versión más reciente de League of Legends
        var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
        string latestVersion = JArray.Parse(versionJson)[0].ToString();
        
        // 2. Obtener la lista de todos los campeones
        var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
        var champions = JObject.Parse(champsRaw)["data"];

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var champProp in champions) {
            string champId = champProp.First["id"].ToString();
            
            // 3. Obtener el detalle de skins de cada campeón
            var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
            var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

            foreach (var skin in skinData) {
                // Asignamos una temática por defecto o basada en el nombre (Riot no la da directamente)
                string skinName = skin["name"].ToString();
                string skinIdRiot = skin["id"].ToString();
                string tema = "Clásica"; // Aquí podrías implementar lógica para detectar temáticas como 'PROYECTO'

                if (skinName.ToLower().Contains("proyecto")) tema = "PROYECTO";
                if (skinName.ToLower().Contains("sultán")) tema = "Sultán";

                await SaveToDb(conn, skinIdRiot, skinName, tema);
            }
        }
    }

    private async Task SaveToDb(MySqlConnection conn, string skinId, string nombre, string tema) {
        // Primero aseguramos que la temática exista y obtenemos su ID
        string queryTema = "INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema); SELECT id_tematica FROM Tematicas WHERE nombre = @tema;";
        int temaId;
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            temaId = Convert.ToInt32(await cmdTema.ExecuteScalarAsync());
        }

        // Insertamos la skin vinculada a esa temática
        string querySkin = "INSERT IGNORE INTO Skins (id_skin_riot, id_tematica, nombre_skin) VALUES (@sid, @tid, @nombre)";
        using (var cmdSkin = new MySqlCommand(querySkin, conn)) {
            cmdSkin.Parameters.AddWithValue("@sid", skinId);
            cmdSkin.Parameters.AddWithValue("@tid", temaId);
            cmdSkin.Parameters.AddWithValue("@nombre", nombre);
            await cmdSkin.ExecuteNonQueryAsync();
        }
    }
}