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

            // DICCIONARIO MEGA-EXTENDIDO DE TEMÁTICAS (Español e Inglés)
            var tematicas = new Dictionary<string, string> {
                { "proyecto", "PROYECTO" }, { "project", "PROYECTO" },
                { "luna sangrienta", "Luna Sangrienta" }, { "blood moon", "Luna Sangrienta" },
                { "estrella oscura", "Estrella Oscura" }, { "dark star", "Estrella Oscura" },
                { "guardiana estelar", "Guardianas Estelares" }, { "star guardian", "Guardianas Estelares" }, { "pijamada estelar", "Guardianas Estelares" }, { "némesis estelar", "Guardianas Estelares" },
                { "forajid", "Forajidos" }, { "solo ante el peligro", "Forajidos" }, { "high noon", "Forajidos" },
                { "k/da", "K/DA" }, { "kda", "K/DA" }, { "true damage", "True Damage" }, { "pentakill", "Pentakill" }, { "heartsteel", "HEARTSTEEL" },
                { "arcade", "Arcade" }, { "recreativa", "Arcade" }, { "jefe de batalla", "Arcade" }, { "battle boss", "Arcade" },
                { "piscina", "Pool Party" }, { "pool party", "Pool Party" }, { "veranieg", "Pool Party" }, { "canción del océano", "Pool Party" },
                { "florecer espiritual", "Florecer Espiritual" }, { "spirit blossom", "Florecer Espiritual" },
                { "odisea", "Odisea" }, { "odyssey", "Odisea" },
                { "academia de combate", "Academia de Combate" }, { "battle academia", "Academia de Combate" },
                { "onda espacial", "Onda Espacial" }, { "space groove", "Onda Espacial" },
                { "empíreo", "Empíreo" }, { "empyrean", "Empíreo" },
                { "peleador de almas", "Peleador de Almas" }, { "soul fighter", "Peleador de Almas" },
                { "tinta sombría", "Tinta Sombría" }, { "inkshadow", "Tinta Sombría" },
                { "luna nevada", "Luna Nevada" }, { "snow moon", "Luna Nevada" },
                { "caballero de la ceniza", "Caballero de la Ceniza" }, { "ashen knight", "Caballero de la Ceniza" },
                { "creador de mitos", "Creador de Mitos" }, { "mythmaker", "Creador de Mitos" },
                { "bestia lunar", "Deleite Lunar" }, { "deleite lunar", "Deleite Lunar" }, { "dinamita", "Deleite Lunar" }, { "firecracker", "Deleite Lunar" }, { "reinos combatientes", "Deleite Lunar" },
                { "reinos mecha", "Reinos Mecha" }, { "mecha kingdoms", "Reinos Mecha" },
                { "rosa de cristal", "Rosa de Cristal" }, { "rosa marchita", "Rosa de Cristal" }, { "crystal rose", "Rosa de Cristal" },
                { "galante", "Galante" }, { "debonair", "Galante" },
                { "ciudad del crimen", "Ciudad del Crimen" }, { "mafios", "Ciudad del Crimen" }, { "crime city", "Ciudad del Crimen" },
                { "domador de monstruos", "Domador de Monstruos" },
                { "noche de miedo", "Noche de Miedo" }, { "fright night", "Noche de Miedo" },
                { "embrujada", "Halloween" }, { "zombi", "Halloween" }, { "conde", "Halloween" }, { "nosferatu", "Halloween" }, { "calabaza", "Halloween" },
                { "bendición invernal", "Invierno" }, { "maravilla invernal", "Invierno" }, { "día nevado", "Invierno" }, { "elfo", "Invierno" }, { "santa", "Invierno" }, { "nieve", "Invierno" }, { "sejuani jinete", "Invierno" },
                { "buscacorazones", "Buscacorazones" }, { "cariñosit", "Buscacorazones" }, { "cupido", "Buscacorazones" }, { "heartseeker", "Buscacorazones" },
                { "victorios", "Victoriosa" }, { "victorious", "Victoriosa" },
                { "campeonato", "Campeonato" }, { "championship", "Campeonato" },
                { "conquistador", "Conquistador" }, { "conqueror", "Conquistador" },
                { "aspirante", "Aspirante" }, { "challenger", "Aspirante" },
                { "hextech", "Hextech" },
                { "prestigios", "Edición Prestigiosa" }, { "prestigio", "Edición Prestigiosa" },
                { "arcane", "Arcane" },
                { "astronauta", "Astronauta" }, { "astronaut", "Astronauta" },
                { "abeja", "Abejitas" }, { "abejita", "Abejitas" }, { "bee", "Abejitas" },
                { "porcelana", "Porcelana" }, { "porcelain", "Porcelana" },
                { "cafetería", "Cafetería" }, { "cafe cuties", "Cafetería" },
                { "chef", "Culinarios" }, { "panader", "Culinarios" }, { "carnicer", "Culinarios" }, { "sushiman", "Culinarios" }, { "cociner", "Culinarios" },
                { "comando", "Comando" }, { "commando", "Comando" },
                { "cyber pop", "Cyber Pop" },
                { "definitivamente no", "Día de las Bromas" }, { "este no es", "Día de las Bromas" }, { "bardo bardo", "Día de las Bromas" }, { "miaukai", "Día de las Bromas" }, { "pugMaw", "Día de las Bromas" },
                { "dracomante", "Dracomantes" }, { "dragón", "Dragones" }, { "matadragones", "Dragones" },
                { "terror nova", "Terror Nova" }, { "dreadnova", "Terror Nova" },
                { "pergaminos shan hai", "Pergaminos Shan Hai" }, { "fábulas", "Fábulas" },
                { "gótico", "Gótico" }, { "pesadilla", "Gótico" },
                { "cazador de cabezas", "Cazador de Cabezas" }, { "headhunter", "Cazador de Cabezas" },
                { "lucha libre", "Lucha Libre" }, { "enmascarad", "Lucha Libre" }, { "el león", "Lucha Libre" }, { "el macho", "Lucha Libre" }, { "el rayo", "Lucha Libre" },
                { "merodeador", "Merodeadores y Celadores" }, { "celador", "Merodeadores y Celadores" },
                { "escuadrón omega", "Escuadrón Omega" },
                { "de papel", "De Papel" }, { "papercraft", "De Papel" },
                { "faraón", "Guardián de las Arenas" }, { "arenas", "Guardián de las Arenas" }, { "sandstorm", "Guardián de las Arenas" },
                { "pirata", "Aguas Turbias" }, { "aguas turbias", "Aguas Turbias" }, { "bucaner", "Aguas Turbias" }, { "bilgewater", "Aguas Turbias" }, { "corsari", "Aguas Turbias" }, { "capitán", "Aguas Turbias" }, { "corta y rasga", "Aguas Turbias" },
                { "pretoriano", "Pretoriano" },
                { "prehistórico", "Prehistórico" }, { "dinosaurio", "Prehistórico" },
                { "programa", "Programa" }, { "program ", "Programa" },
                { "riot", "Riot" },
                { "agente secreto", "Agente Secreto" },
                { "hoja relámpago", "Hoja Relámpago" }, { "shockblade", "Hoja Relámpago" },
                { "valquiria", "Valquirias de Acero" }, { "almirante", "Valquirias de Acero" },
                { "frenesí azucarado", "Frenesí Azucarado" }, { "dulce", "Frenesí Azucarado" }, { "sugar rush", "Frenesí Azucarado" },
                { "juguete", "Juguetes" }, { "toy", "Juguetes" }, { "muñec", "Juguetes" },
                { "vándalo", "Vándalos" }, { "vandal", "Vándalos" },
                { "rompemundos", "Rompemundos" }, { "worldbreaker", "Rompemundos" },
                { "juegos del cénit", "Juegos del Cénit" },
                { "pacto quebrado", "Pacto Quebrado" }, { "broken covenant", "Pacto Quebrado" },
                { "corte feérica", "Corte Feérica" }, { "faerie court", "Corte Feérica" },
                { "demonios callejeros", "Demonios Callejeros" }, { "street demon", "Demonios Callejeros" },
                { "escamas celestiales", "Escamas Celestiales" }, { "heavenscale", "Escamas Celestiales" },
                { "emboscada primigenia", "Emboscada Primigenia" }, { "primal ambush", "Emboscada Primigenia" },
                { "aquelarre", "Aquelarre" }, { "coven", "Aquelarre" }, { "dios antiguo", "Aquelarre" },
                { "bosqueviejo", "Bosqueviejo" }, { "elderwood", "Bosqueviejo" },
                { "eclipse", "Eclipse" },
                { "arruinado", "Arruinados" }, { "ruined", "Arruinados" },
                { "centinela", "Centinelas de la Luz" }, { "sentinel", "Centinelas de la Luz" },
                { "meca", "Meca" }, { "mecha ", "Meca" }, { "mecha zero", "Meca" },
                { "máquina de guerra", "Máquina de Guerra" }, { "battlecast", "Máquina de Guerra" }, { "resistencia", "Resistencia" },
                { "escarcha oscura", "Escarcha Oscura" }, { "blackfrost", "Escarcha Oscura" },
                { "infernal", "Infernal" }, { "fuego sombrío", "Infernal" }, { "volcánic", "Infernal" },
                { "luz celestial", "Luz Celestial" }, { "arclight", "Luz Celestial" }, { "justicier", "Luz Celestial" },
                { "portador del amanecer", "Amanecer y Anochecer" }, { "portador de la noche", "Amanecer y Anochecer" }, { "amanecer", "Amanecer y Anochecer" }, { "anochecer", "Amanecer y Anochecer" }, { "dawnbringer", "Amanecer y Anochecer" }, { "nightbringer", "Amanecer y Anochecer" },
                { "crystalis", "Crystalis Motus" },
                { "fnc ", "eSports" }, { "tpa ", "eSports" }, { "skt t1", "eSports" }, { "ssw ", "eSports" }, { "ig ", "eSports" }, { "fpx ", "eSports" }, { "dwg ", "eSports" }, { "edg ", "eSports" }, { "drx ", "eSports" }, { "t1 ", "eSports" },
                { "supergaláctic", "Supergalácticos" },
                { "psyops", "PsyOps" },
                { "ilusión lunar", "Ilusión Lunar" },
                { "rey dios", "Reyes Dioses" }, { "dios rey", "Reyes Dioses" },
                { "purgador", "Purgadores" }, { "purifier", "Purgadores" },
                { "inmortal", "Viaje Inmortal" }, { "espada divina", "Viaje Inmortal" }, { "báculo divino", "Viaje Inmortal" }, { "espada majestuosa", "Viaje Inmortal" },
                { "shurima", "Regiones de Runaterra" }, { "freljord", "Regiones de Runaterra" }, { "noxus", "Regiones de Runaterra" }, { "noxian", "Regiones de Runaterra" }, { "demacia", "Regiones de Runaterra" }, { "jonia", "Regiones de Runaterra" },
                { "eternum", "Eternum" },
                { "pulso de fuego", "Pulso de Fuego" }, { "pulsefire", "Pulso de Fuego" },
                { "corrupción", "Demacia Vice" }, { "demacia vice", "Demacia Vice" },
                { "rey de las clavadas", "Deportes" }, { "delantero", "Deportes" }, { "portero", "Deportes" }, { "árbitro", "Deportes" }, { "líbero", "Deportes" },
                { "pax ", "PAX" },
                { "rolero", "RPG" }, { "barbablanca", "RPG" }, { "martillo", "RPG" }, { "corazón de león", "RPG" }
            };

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                string champName = champProp.First["name"].ToString();
                
                // Mapeo dinámico de líneas basado en roles de Riot (Preparación para la Fase 2)
                string tags = champProp.First["tags"].ToString();
                string linea = AsignarLinea(champId, tags);

                await Task.Delay(25); // Evitar saturar API

                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinName = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    string skinNum = skin["num"].ToString();

                    // FILTRO INFALIBLE: num == "0" es siempre la skin base. Se ignora por completo.
                    if (skinNum == "0") continue; 

                    // Por defecto, lo que no encuentre entra a "Otras / Únicas"
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
        var dictLineas = new Dictionary<string, string> {
            { "Aatrox", "Top" }, { "Camille", "Top" }, { "Darius", "Top" }, { "Fiora", "Top" }, { "Garen", "Top" }, { "Irelia", "Top" }, { "Jax", "Top" }, { "Malphite", "Top" }, { "Mordekaiser", "Top" }, { "Nasus", "Top" }, { "Ornn", "Top" }, { "Renekton", "Top" }, { "Sett", "Top" }, { "Sion", "Top" }, { "Teemo", "Top" }, { "Urgot", "Top" }, { "Volibear", "Top" }, { "Kwen", "Top" }, { "Illaoi", "Top" }, { "Kled", "Top" }, { "Gnar", "Top" }, { "Shen", "Top" }, { "Yorick", "Top" },
            { "Amumu", "Jungle" }, { "Diana", "Jungle" }, { "Ekko", "Jungle" }, { "Evelynn", "Jungle" }, { "Fiddlesticks", "Jungle" }, { "Graves", "Jungle" }, { "Hecarim", "Jungle" }, { "JarvanIV", "Jungle" }, { "Kayn", "Jungle" }, { "KhaZix", "Jungle" }, { "Kindred", "Jungle" }, { "LeeSin", "Jungle" }, { "MasterYi", "Jungle" }, { "Nidalee", "Jungle" }, { "Nocturne", "Jungle" }, { "Nunu", "Jungle" }, { "Rengar", "Jungle" }, { "Sejuani", "Jungle" }, { "Shaco", "Jungle" }, { "Vi", "Jungle" }, { "Viego", "Jungle" }, { "XinZhao", "Jungle" }, { "Zac", "Jungle" }, { "Udyr", "Jungle" }, { "Rammus", "Jungle" }, { "Warwick", "Jungle" }, { "Trundle", "Jungle" },
            { "Ahri", "Mid" }, { "Akali", "Mid" }, { "Anivia", "Mid" }, { "Annie", "Mid" }, { "Azir", "Mid" }, { "Cassiopeia", "Mid" }, { "Fizz", "Mid" }, { "Katarina", "Mid" }, { "LeBlanc", "Mid" }, { "Lissandra", "Mid" }, { "Malzahar", "Mid" }, { "Orianna", "Mid" }, { "Qiyana", "Mid" }, { "Ryze", "Mid" }, { "Sylas", "Mid" }, { "Syndra", "Mid" }, { "Talon", "Mid" }, { "TwistedFate", "Mid" }, { "Veigar", "Mid" }, { "Viktor", "Mid" }, { "Vladimir", "Mid" }, { "Yasuo", "Mid" }, { "Yone", "Mid" }, { "Zed", "Mid" }, { "Zoe", "Mid" }, { "AurelionSol", "Mid" }, { "Naafiri", "Mid" }, { "Hwei", "Mid" }, { "Kassadin", "Mid" }, { "Galio", "Mid" },
            { "Aphelios", "ADC" }, { "Ashe", "ADC" }, { "Caitlyn", "ADC" }, { "Draven", "ADC" }, { "Ezreal", "ADC" }, { "Jhin", "ADC" }, { "Jinx", "ADC" }, { "Kaisa", "ADC" }, { "Kalista", "ADC" }, { "KogMaw", "ADC" }, { "Lucian", "ADC" }, { "MissFortune", "ADC" }, { "Samira", "ADC" }, { "Sivir", "ADC" }, { "Tristana", "ADC" }, { "Twitch", "ADC" }, { "Varus", "ADC" }, { "Vayne", "ADC" }, { "Xayah", "ADC" }, { "Zeri", "ADC" }, { "Smolder", "ADC" }, { "Nilah", "ADC" },
            { "Alistar", "Support" }, { "Bard", "Support" }, { "Braum", "Support" }, { "Janna", "Support" }, { "Karma", "Support" }, { "Leona", "Support" }, { "Lulu", "Support" }, { "Morgana", "Support" }, { "Nami", "Support" }, { "Nautilus", "Support" }, { "Pyke", "Support" }, { "Rakan", "Support" }, { "Rell", "Support" }, { "Renata", "Support" }, { "Senna", "Support" }, { "Seraphine", "Support" }, { "Sona", "Support" }, { "Soraka", "Support" }, { "Taric", "Support" }, { "Thresh", "Support" }, { "Yuumi", "Support" }, { "Zilean", "Support" }, { "Milio", "Support" }, { "Blitzcrank", "Support" }, { "Zyra", "Support" }, { "Brand", "Support" }
        };

        if (dictLineas.ContainsKey(champId)) return dictLineas[champId];

        // Fallback genérico si es un campeón que no está en la lista principal
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