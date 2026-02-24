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

            // DICCIONARIO DE TEMÁTICAS EXTENDIDO
            var tematicas = new Dictionary<string, string> {
                { "proyecto", "PROYECTO" }, { "project", "PROYECTO" },
                { "luna sangrienta", "Luna Sangrienta" }, { "blood moon", "Luna Sangrienta" },
                { "estrella oscura", "Estrella Oscura" }, { "dark star", "Estrella Oscura" },
                { "guardiana estelar", "Guardianas Estelares" }, { "star guardian", "Guardianas Estelares" },
                { "forajid", "Forajidos" }, { "high noon", "Forajidos" },
                { "k/da", "K/DA" }, { "true damage", "True Damage" }, { "pentakill", "Pentakill" }, { "heartsteel", "HEARTSTEEL" },
                { "arcade", "Arcade" }, { "recreativa", "Arcade" }, { "jefe de batalla", "Arcade" },
                { "piscina", "Pool Party" }, { "pool party", "Pool Party" }, { "veranieg", "Pool Party" },
                { "florecer espiritual", "Florecer Espiritual" }, { "spirit blossom", "Florecer Espiritual" },
                { "odisea", "Odisea" }, { "odyssey", "Odisea" },
                { "academia de combate", "Academia de Combate" },
                { "onda espacial", "Onda Espacial" }, { "empíreo", "Empíreo" }, { "peleador de almas", "Peleador de Almas" },
                { "reinos mecha", "Reinos Mecha" }, { "rosa de cristal", "Rosa de Cristal" },
                { "ciudad del crimen", "Ciudad del Crimen" }, { "mafios", "Ciudad del Crimen" },
                { "noche de miedo", "Noche de Miedo" }, { "embrujada", "Halloween" }, { "zombi", "Halloween" },
                { "invernal", "Invierno" }, { "nevado", "Invierno" }, { "santa", "Invierno" }, { "elfo", "Invierno" },
                { "buscacorazones", "Buscacorazones" }, { "cariñosit", "Buscacorazones" },
                { "victorios", "Victoriosa" }, { "campeonato", "Campeonato" }, { "conquistador", "Conquistador" },
                { "hextech", "Hextech" }, { "prestigios", "Edición Prestigiosa" }, { "arcane", "Arcane" },
                { "astronauta", "Astronauta" }, { "abeja", "Abejitas" }, { "porcelana", "Porcelana" },
                { "cafetería", "Cafetería" }, { "chef", "Culinarios" }, { "panader", "Culinarios" },
                { "dracomante", "Dracomantes" }, { "dragón", "Dragones" }, { "matadragones", "Dragones" },
                { "merodeador", "Merodeadores y Celadores" }, { "celador", "Merodeadores y Celadores" },
                { "escuadrón omega", "Escuadrón Omega" }, { "faraón", "Guardián de las Arenas" },
                { "pirata", "Aguas Turbias" }, { "aguas turbias", "Aguas Turbias" },
                { "aquelarre", "Aquelarre" }, { "bosqueviejo", "Bosqueviejo" },
                { "arruinado", "Arruinados" }, { "centinela", "Centinelas de la Luz" },
                { "meca", "Meca" }, { "máquina de guerra", "Máquina de Guerra" },
                { "infernal", "Infernal" }, { "luz celestial", "Luz Celestial" },
                { "portador del amanecer", "Amanecer y Anochecer" }, { "portador de la noche", "Amanecer y Anochecer" },
                { "skt t1", "eSports" }, { "ssw", "eSports" }, { "ig ", "eSports" }, { "fpx", "eSports" }, { "dwg", "eSports" }, { "edg", "eSports" }, { "drx", "eSports" }, { "t1 ", "eSports" }
            };

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                string champName = champProp.First["name"].ToString();
                
                // Mapeo dinámico de líneas basado en roles de Riot
                string tags = champProp.First["tags"].ToString();
                string linea = AsignarLinea(champId, tags);

                await Task.Delay(25); // Evitar saturar API

                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinName = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    string skinNum = skin["num"].ToString();

                    // FILTRO INFALIBLE: num == "0" es siempre la skin base
                    if (skinNum == "0") continue; 

                    string tema = "Otras / Únicas";
                    foreach (var kw in tematicas) {
                        if (skinName.ToLower().Contains(kw.Key)) {
                            tema = kw.Value;
                            break;
                        }
                    }

                    await SaveSkinToDb(conn, skinIdRiot, skinName, champName, linea, tema);
                }
            }
        } catch (Exception ex) { Console.WriteLine("ERROR SYNC: " + ex.Message); }
    }

    private string AsignarLinea(string champId, string tags) {
        // Asignaciones manuales estrictas para los campeones más populares
        var dictLineas = new Dictionary<string, string> {
            { "Aatrox", "Top" }, { "Camille", "Top" }, { "Darius", "Top" }, { "Fiora", "Top" }, { "Garen", "Top" }, { "Irelia", "Top" }, { "Jax", "Top" }, { "Malphite", "Top" }, { "Mordekaiser", "Top" }, { "Nasus", "Top" }, { "Ornn", "Top" }, { "Renekton", "Top" }, { "Sett", "Top" }, { "Sion", "Top" }, { "Teemo", "Top" }, { "Urgot", "Top" }, { "Volibear", "Top" },
            { "Amumu", "Jungle" }, { "Diana", "Jungle" }, { "Ekko", "Jungle" }, { "Evelynn", "Jungle" }, { "Fiddlesticks", "Jungle" }, { "Graves", "Jungle" }, { "Hecarim", "Jungle" }, { "JarvanIV", "Jungle" }, { "Kayn", "Jungle" }, { "KhaZix", "Jungle" }, { "Kindred", "Jungle" }, { "LeeSin", "Jungle" }, { "MasterYi", "Jungle" }, { "Nidalee", "Jungle" }, { "Nocturne", "Jungle" }, { "Nunu", "Jungle" }, { "Rengar", "Jungle" }, { "Sejuani", "Jungle" }, { "Shaco", "Jungle" }, { "Vi", "Jungle" }, { "Viego", "Jungle" }, { "XinZhao", "Jungle" }, { "Zac", "Jungle" },
            { "Ahri", "Mid" }, { "Akali", "Mid" }, { "Anivia", "Mid" }, { "Annie", "Mid" }, { "Azir", "Mid" }, { "Cassiopeia", "Mid" }, { "Fizz", "Mid" }, { "Katarina", "Mid" }, { "LeBlanc", "Mid" }, { "Lissandra", "Mid" }, { "Malzahar", "Mid" }, { "Orianna", "Mid" }, { "Qiyana", "Mid" }, { "Ryze", "Mid" }, { "Sylas", "Mid" }, { "Syndra", "Mid" }, { "Talon", "Mid" }, { "TwistedFate", "Mid" }, { "Veigar", "Mid" }, { "Viktor", "Mid" }, { "Vladimir", "Mid" }, { "Yasuo", "Mid" }, { "Yone", "Mid" }, { "Zed", "Mid" }, { "Zoe", "Mid" },
            { "Aphelios", "ADC" }, { "Ashe", "ADC" }, { "Caitlyn", "ADC" }, { "Draven", "ADC" }, { "Ezreal", "ADC" }, { "Jhin", "ADC" }, { "Jinx", "ADC" }, { "Kaisa", "ADC" }, { "Kalista", "ADC" }, { "KogMaw", "ADC" }, { "Lucian", "ADC" }, { "MissFortune", "ADC" }, { "Samira", "ADC" }, { "Sivir", "ADC" }, { "Tristana", "ADC" }, { "Twitch", "ADC" }, { "Varus", "ADC" }, { "Vayne", "ADC" }, { "Xayah", "ADC" }, { "Zeri", "ADC" },
            { "Alistar", "Support" }, { "Bard", "Support" }, { "Braum", "Support" }, { "Janna", "Support" }, { "Karma", "Support" }, { "Leona", "Support" }, { "Lulu", "Support" }, { "Morgana", "Support" }, { "Nami", "Support" }, { "Nautilus", "Support" }, { "Pyke", "Support" }, { "Rakan", "Support" }, { "Rell", "Support" }, { "Renata", "Support" }, { "Senna", "Support" }, { "Seraphine", "Support" }, { "Sona", "Support" }, { "Soraka", "Support" }, { "Taric", "Support" }, { "Thresh", "Support" }, { "Yuumi", "Support" }, { "Zilean", "Support" }
        };

        if (dictLineas.ContainsKey(champId)) return dictLineas[champId];

        // Fallback genérico si es un campeón muy nuevo o no listado arriba
        if (tags.Contains("Marksman")) return "ADC";
        if (tags.Contains("Support")) return "Support";
        if (tags.Contains("Assassin")) return "Mid";
        if (tags.Contains("Mage")) return "Mid";
        if (tags.Contains("Tank")) return "Top";
        return "Flex";
    }

    private async Task SaveSkinToDb(MySqlConnection conn, string skinId, string nombre_skin, string campeon, string linea, string tema) {
        int temaId;
        string queryTema = "INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema); SELECT id_tematica FROM Tematicas WHERE nombre = @tema LIMIT 1;";
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            temaId = Convert.ToInt32(await cmdTema.ExecuteScalarAsync());
        }

        string querySkin = "INSERT IGNORE INTO Skins (id_skin_riot, id_tematica, nombre_skin, campeon, linea) VALUES (@sid, @tid, @nombre, @camp, @linea)";
        using (var cmdSkin = new MySqlCommand(querySkin, conn)) {
            cmdSkin.Parameters.AddWithValue("@sid", skinId);
            cmdSkin.Parameters.AddWithValue("@tid", temaId);
            cmdSkin.Parameters.AddWithValue("@nombre", nombre_skin);
            cmdSkin.Parameters.AddWithValue("@camp", campeon);
            cmdSkin.Parameters.AddWithValue("@linea", linea);
            await cmdSkin.ExecuteNonQueryAsync();
        }
    }
}