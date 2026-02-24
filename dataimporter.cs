using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class RiotDataService {
    private readonly string _connectionString = "Server=ballast.proxy.rlwy.net;Port=38239;Database=railway;Uid=root;Pwd=moBtcnWVmvOzjozxaxNqkuxvyoXOWMbR;";

    public async Task SyncRiotData() {
        using var client = new HttpClient();
        try {
            var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
            string latestVersion = JArray.Parse(versionJson)[0].ToString();
            var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
            var champions = JObject.Parse(champsRaw)["data"];

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var palabrasClave = new Dictionary<string, string> {
                { "proyecto", "PROYECTO" }, { "project", "PROYECTO" },
                { "luna sangrienta", "Luna Sangrienta" }, { "blood moon", "Luna Sangrienta" },
                { "star guardian", "Guardianas Estelares" }, { "estelar", "Guardianas Estelares" },
                { "k/da", "K/DA" }, { "pentakill", "Pentakill" }, { "true damage", "True Damage" },
                { "arcade", "Arcade" }, { "recreativa", "Arcade" },
                { "solo ante el peligro", "Forajidos" }, { "high noon", "Forajidos" },
                { "pool party", "Pool Party" }, { "fiesta en la piscina", "Pool Party" },
                { "vitoriosa", "Victoriosa" }, { "victorious", "Victoriosa" },
                { "campeonato", "Campeonato" }, { "championship", "Campeonato" },
                { "sktt1", "eSports" }, { "fnatic", "eSports" }, { "tpa", "eSports" }, { "ssw", "eSports" }, 
                { "ig ", "eSports" }, { "fpx", "eSports" }, { "dwg", "eSports" }, { "drx", "eSports" }, { "t1 ", "eSports" }
            };

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinName = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();

                    // FILTRO: Ignorar aspectos base
                    if (skinName.ToLower() == "default" || skinName.ToLower() == champId.ToLower()) {
                        continue; 
                    }

                    string tema = "Otros";
                    foreach (var kw in palabrasClave) {
                        if (skinName.ToLower().Contains(kw.Key)) {
                            tema = kw.Value;
                            break;
                        }
                    }

                    // Aquí es donde se llama al método que te da el error
                    await SaveSkinToDb(conn, skinIdRiot, skinName, tema);
                }
            }
        } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    // ESTE ES EL MÉTODO QUE FALTA EN TU CONTEXTO
    private async Task SaveSkinToDb(MySqlConnection conn, string skinId, string nombre, string tema) {
        int temaId;
        string queryTema = "INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema); SELECT id_tematica FROM Tematicas WHERE nombre = @tema LIMIT 1;";
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            var result = await cmdTema.ExecuteScalarAsync();
            temaId = Convert.ToInt32(result);
        }

        string querySkin = "INSERT IGNORE INTO Skins (id_skin_riot, id_tematica, nombre_skin) VALUES (@sid, @tid, @nombre)";
        using (var cmdSkin = new MySqlCommand(querySkin, conn)) {
            cmdSkin.Parameters.AddWithValue("@sid", skinId);
            cmdSkin.Parameters.AddWithValue("@tid", temaId);
            cmdSkin.Parameters.AddWithValue("@nombre", nombre);
            await cmdSkin.ExecuteNonQueryAsync();
        }
    }
}