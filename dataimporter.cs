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
    
    // LÓGICA DE DETECCIÓN MEJORADA
    string tema = "Otros"; // Cambiamos 'Clásica' por 'Otros' para mayor claridad

    // Diccionario de temáticas comunes (puedes agregar más aquí)
    var palabrasClave = new Dictionary<string, string> {
        { "proyecto", "PROYECTO" },
        { "luna sangrienta", "Luna Sangrienta" },
        { "k/da", "K/DA" },
        { "arcade", "Arcade" },
        { "pool party", "Pool Party" },
        { "fiesta en la piscina", "Pool Party" },
        { "estelar", "Guardianas Estelares" },
        { "guardiana estelar", "Guardianas Estelares" },
        { "reinos mecha", "Reinos Mecha" },
        { "odisea", "Odisea" },
        { "solo ante el peligro", "Forajidos" },
        { "forajido", "Forajidos" },
        { "academia de combate", "Academia de Combate" },
        { "vitoriosa", "Victoriosa" },
        { "conquistador", "Conquistador" },
        { "sktt1", "eSports" },
        { "fnatic", "eSports" },
        { "tpa", "eSports" },
        { "ssw", "eSports" },
        { "ig ", "eSports" },
        { "fpx", "eSports" },
        { "dwg", "eSports" },
        { "drx", "eSports" }
    };

    // Buscamos si el nombre de la skin contiene alguna palabra clave
    foreach (var kw in palabrasClave) {
        if (skinName.ToLower().Contains(kw.Key)) {
            tema = kw.Value;
            break;
        }
    }

    // Si es la skin base (el nombre de la skin es igual al del campeón), es 'Clásica'
    if (skinName.ToLower() == champId.ToLower() || skinName.ToLower() == "default") {
        tema = "Aspecto Base";
    }

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
    public async Task SyncRiotData() {
    using var client = new HttpClient();
    try {
        var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
        string latestVersion = JArray.Parse(versionJson)[0].ToString();
        
        var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion.json");
        var champions = JObject.Parse(champsRaw)["data"];

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var champProp in champions) {
            try {
                string champId = champProp.First["id"].ToString();
                // Pequeña pausa para no saturar la API de Riot
                await Task.Delay(100); 

                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    // ... (aquí va tu lógica de palabras clave de temáticas)
                    await SaveSkinToDb(conn, skinIdRiot, skinName, tema);
                }
            } catch (Exception ex) {
                // Si falla un campeón, que siga con el siguiente
                Console.WriteLine($"Error con el campeón {champProp.Key}: {ex.Message}");
                continue;
            }
        }
    } catch (Exception ex) {
        Console.WriteLine("Error general: " + ex.Message);
    }
}
}