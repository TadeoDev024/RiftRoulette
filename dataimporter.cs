using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Globalization;
using System.Linq;

public class RiotDataService {
    private readonly string _connectionString = "Server=ballast.proxy.rlwy.net;Port=38239;Database=railway;Uid=root;Pwd=moBtcnWVmvOzjozxaxNqkuxvyoXOWMbR;";

    // Normalizador: Quita tildes, diéresis y pone todo en minúscula
    private string LimpiarTexto(string texto) {
        if (string.IsNullOrWhiteSpace(texto)) return texto;
        var textoNormalizado = texto.Normalize(NormalizationForm.FormD);
        var constructor = new StringBuilder();

        foreach (var c in textoNormalizado) {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) {
                constructor.Append(c);
            }
        }
        return constructor.ToString().Normalize(NormalizationForm.FormC).ToLower();
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

            // Diccionario SIN TILDES y en minúscula (porque el normalizador limpiará los nombres de las skins)
            var tematicas = new Dictionary<string, string> {
                { "proyecto", "PROYECTO" },
                { "luna sangrienta", "Luna Sangrienta" }, { "luna de sangre", "Luna Sangrienta" },
                { "estrella oscura", "Estrella Oscura" }, { "cosmic", "Estrella Oscura / Cósmicos" }, { "oscuridad cosmica", "Estrella Oscura / Cósmicos" },
                { "guardian estelar", "Guardianas Estelares" }, { "guardiana estelar", "Guardianas Estelares" }, { "pijama estelar", "Guardianas Estelares" }, { "nemesis estelar", "Guardianas Estelares" },
                { "forajid", "Forajidos" }, { "solo ante el peligro", "Forajidos" },
                { "k/da", "K/DA" }, { "kda", "K/DA" }, { "true damage", "True Damage" }, { "pentakill", "Pentakill" }, { "heartsteel", "HEARTSTEEL" },
                { "arcade", "Arcade" }, { "recreativ", "Arcade" }, { "jefe de batalla", "Arcade" }, { "subjefe", "Arcade" },
                { "piscina", "Pool Party" }, { "veranieg", "Pool Party" },
                { "florecer espiritual", "Florecer Espiritual" }, { "flor espiritual", "Florecer Espiritual" },
                { "odisea", "Odisea" },
                { "academia de combate", "Academia de Combate" },
                { "onda espacial", "Onda Espacial" },
                { "empireo", "Empíreo" },
                { "peleador de almas", "Peleador de Almas" }, { "luchador de almas", "Peleador de Almas" },
                { "tinta sombria", "Tinta Sombría" },
                { "luna nevada", "Luna Nevada" },
                { "caballero de la ceniza", "Caballero de la Ceniza" }, { "caballero ceniza", "Caballero de la Ceniza" },
                { "creador de mitos", "Creador de Mitos" },
                { "deleite lunar", "Deleite Lunar" }, { "dinamita", "Deleite Lunar" }, { "artificio", "Deleite Lunar" }, { "bestia lunar", "Deleite Lunar" },
                { "reinos mecha", "Reinos Mecha" },
                { "rosa de cristal", "Rosa de Cristal" }, { "rosa marchita", "Rosa de Cristal" },
                { "galante", "Galante" },
                { "ciudad del crimen", "Ciudad del Crimen" }, { "mafios", "Ciudad del Crimen" },
                { "noche de miedo", "Noche de Miedo" },
                { "embrujad", "Halloween" }, { "zombi", "Halloween" }, { "conde", "Halloween" }, { "nosferatu", "Halloween" }, { "calabaza", "Halloween" }, { "murcielago", "Halloween" }, { "embrujo", "Halloween" },
                { "invernal", "Invierno" }, { "maravilla invernal", "Invierno" }, { "nevado", "Invierno" }, { "nieve", "Invierno" }, { "santa", "Invierno" }, { "elfo", "Invierno" }, { "frio", "Invierno" },
                { "buscacorazones", "Buscacorazones" }, { "cariñosit", "Buscacorazones" }, { "cupido", "Buscacorazones" }, { "rompecorazones", "Buscacorazones" }, { "amoros", "Buscacorazones" },
                { "victorios", "Victoriosa" },
                { "campeonato", "Campeonato" }, { "mundial", "Campeonato" },
                { "conquistador", "Conquistador" },
                { "hextech", "Hextech" },
                { "prestigios", "Edición Prestigiosa" }, { "prestigio", "Edición Prestigiosa" },
                { "arcane", "Arcane" },
                { "astronauta", "Astronauta" },
                { "abejit", "Abejitas" }, { "abeja", "Abejitas" },
                { "porcelana", "Porcelana" },
                { "cafeteria", "Cafetería" }, { "cafe", "Cafetería" },
                { "chef", "Culinarios" }, { "panader", "Culinarios" }, { "carnicer", "Culinarios" }, { "sushiman", "Culinarios" }, { "cociner", "Culinarios" }, { "pizza", "Culinarios" },
                { "comando", "Comando" },
                { "cyber pop", "Cyber Pop" }, { "ciber pop", "Cyber Pop" },
                { "definitivamente no", "Día de las Bromas" }, { "este no es", "Día de las Bromas" }, { "bardo bardo", "Día de las Bromas" }, { "miaukai", "Día de las Bromas" }, { "pugmaw", "Día de las Bromas" },
                { "dracomante", "Dracomantes" }, { "dragon", "Dragones" }, { "matadragon", "Dragones" },
                { "terror nova", "Terror Nova" },
                { "pergaminos shan hai", "Pergaminos Shan Hai" }, { "shan hai", "Pergaminos Shan Hai" },
                { "fabulas", "Fábulas" }, { "fabula", "Fábulas" },
                { "gotic", "Gótico" }, { "pesadilla", "Gótico" },
                { "cazador de cabezas", "Cazador de Cabezas" }, { "cazadora de cabezas", "Cazador de Cabezas" },
                { "lucha libre", "Lucha Libre" }, { "enmascarad", "Lucha Libre" }, { "el leon", "Lucha Libre" }, { "el macho", "Lucha Libre" }, { "el rayo", "Lucha Libre" }, { "tigre", "Lucha Libre" },
                { "merodeador", "Merodeadores y Celadores" }, { "celador", "Merodeadores y Celadores" },
                { "escuadron omega", "Escuadrón Omega" },
                { "de papel", "De Papel" }, { "origami", "De Papel" },
                { "faraon", "Guardián de las Arenas" }, { "arenas", "Guardián de las Arenas" }, { "shurima", "Guardián de las Arenas" },
                { "pirata", "Aguas Turbias" }, { "aguas turbias", "Aguas Turbias" }, { "bucaner", "Aguas Turbias" }, { "corsari", "Aguas Turbias" }, { "capitan", "Aguas Turbias" }, { "corta y rasga", "Aguas Turbias" },
                { "pretoriano", "Pretoriano" },
                { "prehistoric", "Prehistórico" }, { "dinosaurio", "Prehistórico" },
                { "programa", "Programa" },
                { "riot", "Riot" },
                { "agente secreto", "Agente Secreto" }, { "espia", "Agente Secreto" },
                { "hoja relampago", "Hoja Relámpago" },
                { "valquiria", "Valquirias de Acero" }, { "almirante", "Valquirias de Acero" },
                { "frenesi azucarado", "Frenesí Azucarado" }, { "dulce", "Frenesí Azucarado" }, { "caramelo", "Frenesí Azucarado" },
                { "juguete", "Juguetes" }, { "muñec", "Juguetes" }, { "trapo", "Juguetes" },
                { "vandal", "Vándalos" },
                { "rompemundos", "Rompemundos" }, { "rompe mundos", "Rompemundos" },
                { "corte feerica", "Corte Feérica" }, { "feeric", "Corte Feérica" },
                { "demonios callejeros", "Demonios Callejeros" }, { "demonio callejero", "Demonios Callejeros" },
                { "escamas celestiales", "Escamas Celestiales" }, { "escama celestial", "Escamas Celestiales" },
                { "emboscada primigenia", "Emboscada Primigenia" },
                { "aquelarre", "Aquelarre" }, { "dios antiguo", "Aquelarre" },
                { "bosqueviejo", "Bosqueviejo" }, { "bosque viejo", "Bosqueviejo" }, { "arboleda", "Bosqueviejo" },
                { "eclipse", "Eclipse" },
                { "arruinad", "Arruinados" }, { "rey arruinado", "Arruinados" },
                { "centinela", "Centinelas de la Luz" },
                { "meca", "Meca" }, { "mecha", "Meca" },
                { "maquina de guerra", "Máquina de Guerra" }, { "resistencia", "Máquina de Guerra" },
                { "escarcha oscura", "Escarcha Oscura" },
                { "infernal", "Infernal" }, { "fuego", "Infernal" }, { "volcanic", "Infernal" }, { "diabolic", "Infernal" },
                { "luz celestial", "Luz Celestial" }, { "justicier", "Luz Celestial" }, { "arco de luz", "Luz Celestial" },
                { "portador del amanecer", "Amanecer y Anochecer" }, { "portador de la noche", "Amanecer y Anochecer" }, { "amanecer", "Amanecer y Anochecer" }, { "anochecer", "Amanecer y Anochecer" }, { "heraldo", "Amanecer y Anochecer" },
                { "crystalis", "Crystalis Motus" },
                { "fnc ", "eSports" }, { "tpa ", "eSports" }, { "skt t1", "eSports" }, { "ssw ", "eSports" }, { "ig ", "eSports" }, { "fpx ", "eSports" }, { "dwg ", "eSports" }, { "edg ", "eSports" }, { "drx ", "eSports" }, { "t1 ", "eSports" }, { "esports", "eSports" },
                { "supergalactic", "Supergalácticos" },
                { "psyops", "PsyOps" },
                { "rey dios", "Reyes Dioses" }, { "dios rey", "Reyes Dioses" },
                { "inmortal", "Viaje Inmortal" }, { "espada divina", "Viaje Inmortal" }, { "baculo divino", "Viaje Inmortal" }, { "espada majestuosa", "Viaje Inmortal" },
                { "eternum", "Eternum" },
                { "pulso de fuego", "Pulso de Fuego" },
                { "demacia vice", "Demacia Vice" }, { "vice", "Demacia Vice" },
                { "deportes", "Deportes" }, { "delantero", "Deportes" }, { "portero", "Deportes" }, { "arbitro", "Deportes" }, { "libero", "Deportes" }, { "baloncest", "Deportes" },
                { "pax ", "PAX" },
                { "rolero", "RPG" }, { "barbablanca", "RPG" }, { "martillo", "RPG" }, { "corazon de leon", "RPG" },
                { "belic", "Bélico" }, { "blindaje", "Bélico" }
            };

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                string champName = champProp.First["name"].ToString();
                string tags = champProp.First["tags"].ToString();
                string linea = AsignarLinea(champId, tags);

                await Task.Delay(25); 

                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinNameRiot = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    string skinNum = skin["num"].ToString();

                    if (skinNum == "0") continue; // Filtrar aspectos base

                    // APLICAR EL NORMALIZADOR AL NOMBRE DE LA SKIN
                    string skinNameLimpio = LimpiarTexto(skinNameRiot);
                    string tema = "Otras / Únicas";
                    
                    foreach (var kw in tematicas) {
                        if (skinNameLimpio.Contains(kw.Key)) {
                            tema = kw.Value;
                            break;
                        }
                    }

                    // Guardamos el nombre original para que se vea bien visualmente, pero clasificamos usando el limpio
                    await SaveSkinToDb(conn, skinIdRiot, skinNameRiot, champName, linea, tema);
                }
            }
        } catch (Exception ex) { Console.WriteLine("ERROR SYNC: " + ex.Message); }
    }

    private string AsignarLinea(string champId, string tags) {
        var dictLineas = new Dictionary<string, string> {
            { "Aatrox", "Top" }, { "Camille", "Top" }, { "Darius", "Top" }, { "Fiora", "Top" }, { "Garen", "Top" }, { "Irelia", "Top" }, { "Jax", "Top" }, { "Malphite", "Top" }, { "Mordekaiser", "Top" }, { "Nasus", "Top" }, { "Ornn", "Top" }, { "Renekton", "Top" }, { "Sett", "Top" }, { "Sion", "Top" }, { "Teemo", "Top" }, { "Urgot", "Top" }, { "Volibear", "Top" }, { "Gwen", "Top" }, { "Illaoi", "Top" }, { "Kled", "Top" }, { "Gnar", "Top" }, { "Shen", "Top" }, { "Yorick", "Top" },
            { "Amumu", "Jungle" }, { "Diana", "Jungle" }, { "Ekko", "Jungle" }, { "Evelynn", "Jungle" }, { "Fiddlesticks", "Jungle" }, { "Graves", "Jungle" }, { "Hecarim", "Jungle" }, { "JarvanIV", "Jungle" }, { "Kayn", "Jungle" }, { "KhaZix", "Jungle" }, { "Kindred", "Jungle" }, { "LeeSin", "Jungle" }, { "MasterYi", "Jungle" }, { "Nidalee", "Jungle" }, { "Nocturne", "Jungle" }, { "Nunu", "Jungle" }, { "Rengar", "Jungle" }, { "Sejuani", "Jungle" }, { "Shaco", "Jungle" }, { "Vi", "Jungle" }, { "Viego", "Jungle" }, { "XinZhao", "Jungle" }, { "Zac", "Jungle" }, { "Udyr", "Jungle" }, { "Rammus", "Jungle" }, { "Warwick", "Jungle" }, { "Trundle", "Jungle" },
            { "Ahri", "Mid" }, { "Akali", "Mid" }, { "Anivia", "Mid" }, { "Annie", "Mid" }, { "Azir", "Mid" }, { "Cassiopeia", "Mid" }, { "Fizz", "Mid" }, { "Katarina", "Mid" }, { "LeBlanc", "Mid" }, { "Lissandra", "Mid" }, { "Malzahar", "Mid" }, { "Orianna", "Mid" }, { "Qiyana", "Mid" }, { "Ryze", "Mid" }, { "Sylas", "Mid" }, { "Syndra", "Mid" }, { "Talon", "Mid" }, { "TwistedFate", "Mid" }, { "Veigar", "Mid" }, { "Viktor", "Mid" }, { "Vladimir", "Mid" }, { "Yasuo", "Mid" }, { "Yone", "Mid" }, { "Zed", "Mid" }, { "Zoe", "Mid" }, { "AurelionSol", "Mid" }, { "Naafiri", "Mid" }, { "Hwei", "Mid" }, { "Kassadin", "Mid" }, { "Galio", "Mid" },
            { "Aphelios", "ADC" }, { "Ashe", "ADC" }, { "Caitlyn", "ADC" }, { "Draven", "ADC" }, { "Ezreal", "ADC" }, { "Jhin", "ADC" }, { "Jinx", "ADC" }, { "Kaisa", "ADC" }, { "Kalista", "ADC" }, { "KogMaw", "ADC" }, { "Lucian", "ADC" }, { "MissFortune", "ADC" }, { "Samira", "ADC" }, { "Sivir", "ADC" }, { "Tristana", "ADC" }, { "Twitch", "ADC" }, { "Varus", "ADC" }, { "Vayne", "ADC" }, { "Xayah", "ADC" }, { "Zeri", "ADC" }, { "Smolder", "ADC" }, { "Nilah", "ADC" },
            { "Alistar", "Support" }, { "Bard", "Support" }, { "Braum", "Support" }, { "Janna", "Support" }, { "Karma", "Support" }, { "Leona", "Support" }, { "Lulu", "Support" }, { "Morgana", "Support" }, { "Nami", "Support" }, { "Nautilus", "Support" }, { "Pyke", "Support" }, { "Rakan", "Support" }, { "Rell", "Support" }, { "Renata", "Support" }, { "Senna", "Support" }, { "Seraphine", "Support" }, { "Sona", "Support" }, { "Soraka", "Support" }, { "Taric", "Support" }, { "Thresh", "Support" }, { "Yuumi", "Support" }, { "Zilean", "Support" }, { "Milio", "Support" }, { "Blitzcrank", "Support" }, { "Zyra", "Support" }, { "Brand", "Support" }
        };

        if (dictLineas.ContainsKey(champId)) return dictLineas[champId];

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