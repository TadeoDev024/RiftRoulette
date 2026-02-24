using Newtonsoft.Json.Linq; // Instalar vía NuGet
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
public class RiotDataService {
    private readonly string _connectionString = "mysql://root:moBtcnWVmvOzjozxaxNqkuxvyoXOWMbR@ballast.proxy.rlwy.net:38239/railway";

    public async Task SyncRiotData() {
        using var client = new HttpClient();
        // 1. Obtener versión actual y lista de campeones
        var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
        string latestVersion = JArray.Parse(versionJson)[0].ToString();
        
        var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
        var champions = JObject.Parse(champsRaw)["data"];

        foreach (var champProp in champions) {
            string champId = champProp.First["id"].ToString();
            // 2. Obtener detalle de cada campeón para ver sus skins
            var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
            var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

            foreach (var skin in skinData) {
                // Aquí extraemos o asignamos la temática (Nota: Riot no da la temática en el JSON básico, 
                // pero puedes mapearla por palabras clave o usar un diccionario manual de temáticas populares)
                // await SaveSkinToDb(champProp.First, skin, champId);
            }
        }
    }
    
    // El método SaveSkinToDb ejecutaría el INSERT en MySQL...
}