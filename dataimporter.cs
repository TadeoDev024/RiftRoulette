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
            // 1. Obtener versión
            var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
            string latestVersion = JArray.Parse(versionJson)[0].ToString();
            
            // 2. Obtener lista de campeones
            var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
            var champions = JObject.Parse(champsRaw)["data"];

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Diccionario masivo de temáticas (Español e Inglés)
            var palabrasClave = new Dictionary<string, string> {
                { "proyecto", "PROYECTO" }, { "project", "PROYECTO" },
                { "luna sangrienta", "Luna Sangrienta" }, { "blood moon", "Luna Sangrienta" },
                { "star guardian", "Guardianas Estelares" }, { "estelar", "Guardianas Estelares" },
                { "k/da", "K/DA" }, { "true damage", "True Damage" }, { "pentakill", "Pentakill" },
                { "arcade", "Arcade / Recreativa" }, { "recreativa", "Arcade / Recreativa" },
                { "florecer espiritual", "Florecer Espiritual" }, { "spirit blossom", "Florecer Espiritual" },
                { "solo ante el peligro", "Forajidos" }, { "high noon", "Forajidos" }, { "forajid", "Forajidos" },
                { "pool party", "Pool Party" }, { "fiesta en la piscina", "Pool Party" },
                { "odisea", "Odisea" }, { "odyssey", "Odisea" },
                { "academia de combate", "Academia de Combate" }, { "battle academia", "Academia de Combate" },
                { "estrella oscura", "Estrella Oscura" }, { "dark star", "Estrella Oscura" },
                { "cósmic", "Cósmico" }, { "cosmic", "Cósmico" },
                { "vitoriosa", "Victoriosa" }, { "victorious", "Victoriosa" },
                { "campeonato", "Campeonato" }, { "championship", "Campeonato" },
                { "infernal", "Infernal" }, { "meca", "Mecha" }, { "mecha", "Mecha" },
                { "máquina de guerra", "Máquina de Guerra" }, { "battlecast", "Máquina de Guerra" },
                { "pulso de fuego", "Pulso de Fuego" }, { "pulsefire", "Pulso de Fuego" },
                { "aquelarre", "Aquelarre" }, { "coven", "Aquelarre" },
                { "bosqueviejo", "Elderwood" }, { "elderwood", "Elderwood" },
                { "portador del amanecer", "Amanecer / Anochecer" }, { "dawnbringer", "Amanecer / Anochecer" },
                { "portador de la noche", "Amanecer / Anochecer" }, { "nightbringer", "Amanecer / Anochecer" },
                { "buscacorazones", "Buscacorazones" }, { "heartseeker", "Buscacorazones" },
                { "nevado", "Invierno" }, { "winter wonder", "Invierno" }, { "snow day", "Invierno" },
                { "sktt1", "eSports" }, { "fnatic", "eSports" }, { "tpa", "eSports" }, { "ssw", "eSports" },
                { "ig ", "eSports" }, { "fpx", "eSports" }, { "dwg", "eSports" }, { "drx", "eSports" }, { "t1 ", "eSports" },
                { "hextech", "Hextech" }, { "arcane", "Arcane" }
            };

            foreach (var champProp in champions) {
                try {
                    string champId = champProp.First["id"].ToString();
                    await Task.Delay(50); // Evitar saturar la API

                    var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                    var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                    foreach (var skin in skinData) {
                        string skinName = skin["name"].ToString();
                        string skinIdRiot = skin["id"].ToString();
                        string tema = "Otros";

                        foreach (var kw in palabrasClave) {
                            if (skinName.ToLower().Contains(kw.Key)) {
                                tema = kw.Value;
                                break;
                            }
                        }

                        if (skinName.ToLower() == "default" || skinName.ToLower() == champId.ToLower()) {
                            tema = "Aspecto Base";
                        }

                        await SaveSkinToDb(conn, skinIdRiot, skinName, tema);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error procesando campeón: {ex.Message}");
                    continue; 
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error general en Sync: " + ex.Message);
            throw;
        }
    }

    private async Task SaveSkinToDb(MySqlConnection conn, string skinId, string nombre, string tema) {
        int temaId;
        string queryTema = "INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema); SELECT id_tematica FROM Tematicas WHERE nombre = @tema LIMIT 1;";
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            temaId = Convert.ToInt32(await cmdTema.ExecuteScalarAsync());
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