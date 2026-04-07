using Newtonsoft.Json.Linq;
using MySqlConnector;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class RiotDataService {
    
    // Lee la contraseña secreta del servidor de Render de forma segura
    private readonly string _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                                                ?? "Server=HOST_OCULTO;Port=1234;Database=defaultdb;Uid=avnadmin;Pwd=PASSWORD_OCULTA;SslMode=Required;";

    public async Task SyncRiotData() {
        using var client = new HttpClient();
        try {
            Console.WriteLine("Iniciando sincronización masiva...");
            
            // 1. Obtenemos la última versión del juego
            var versionJson = await client.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
            string latestVersion = JArray.Parse(versionJson)[0].ToString();
            
            // 2. Descargamos el championFull.json directamente de Riot
            var fullRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/en_US/championFull.json");
            var champions = JObject.Parse(fullRaw)["data"];

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // 3. DICCIONARIO MAESTRO ULTRA-AMPLIADO
            var tematicas = new Dictionary<string, string> {
                // Bandas y Música
                { "k/da", "K/DA" }, { "true damage", "True Damage" }, { "pentakill", "Pentakill" }, { "heartsteel", "HEARTSTEEL" },
                
                // Sci-Fi, Futurista y Mechas
                { "project", "PROYECTO" }, { "program", "Programa" }, { "pulsefire", "Pulso de Fuego" }, { "psyops", "PsyOps" },
                { "mecha", "Mecha" }, { "battlecast", "Blindaje Bélico" }, { "praetorian", "Pretoriano" }, { "super galaxy", "Super Galaxia" },
                { "odyssey", "Odisea" }, { "steel valkyrie", "Valquirias de Acero" }, { "astronaut", "Astronauta" }, { "space groove", "Onda Espacial" },
                { "cyber pop", "Cyber Pop" }, { "dreadnova", "Dreadnova" }, { "omega squad", "Escuadrón Omega" },
                
                // Magia, Fantasía y Mitología
                { "star guardian", "Guardianas Estelares" }, { "pajama guardian", "Guardianas Estelares" }, { "star nemesis", "Guardianas Estelares" },
                { "blood moon", "Luna Sangrienta" }, { "snow moon", "Luna de Nieve" },
                { "coven", "Aquelarre" }, { "elderwood", "Bosque Viejo" }, { "eclipse", "Eclipse" }, { "old god", "Aquelarre" }, { "thousand-pierced", "Aquelarre" },
                { "spirit blossom", "Flor Espiritual" }, { "inkshadow", "Tinta Sombría" }, { "shan hai", "Pergaminos Shan Hai" },
                { "ruined", "Arruinado" }, { "sentinel", "Centinelas de la Luz" }, { "dawn", "Portadores" }, { "nightbringer", "Portadores" },
                { "dragonmancer", "Dracomantes" }, { "dragon slayer", "Matadragones" }, { "dragon trainer", "Entrenador de Dragones" },
                { "faerie court", "Corte Feérica" }, { "crystal rose", "Rosa de Cristal" }, { "withered rose", "Rosa de Cristal" },
                { "immortal journey", "Viaje Inmortal" }, { "divine sword", "Viaje Inmortal" }, { "god-king", "Rey Dios" }, { "god king", "Rey Dios" },
                { "ashen", "Caballeros de la Ceniza" }, { "arclight", "Luz Celestial" }, { "justicar", "Luz Celestial" }, { "dark star", "Estrella Oscura" }, { "cosmic", "Cósmico" },
                
                // RPG y Fantasía Clásica
                { "rift quest", "Juego de Rol" }, { "worldbreaker", "Rompemundos" }, { "broken covenant", "Pacto Roto" }, { "vanguard", "Vanguardia" },
                
                // Terror, Sombras y Oscuridad
                { "blackfrost", "Escarcha Negra" }, { "death sworn", "Juramentados" }, { "gothic", "Gótico" },
                { "nightmare", "Pesadilla" }, { "shadow isles", "Islas de la Sombra" }, { "soulstealer", "Ladrones de Almas" },
                
                // Festividades y Estaciones
                { "pool party", "Veraniega" }, { "ocean song", "Veraniega" },
                { "winterblessed", "Bendición Invernal" }, { "winter wonder", "Maravilla Invernal" }, { "snow day", "Día Nevado" }, { "santa", "Navidad" }, { "elf", "Navidad" }, { "frost", "Escarcha" },
                { "bewitching", "Halloween" }, { "zombie", "Halloween" }, { "fright night", "Noche de Miedo" },
                { "heartseeker", "Día de San Valentín" }, { "sweetheart", "Día de San Valentín" },
                { "firecracker", "Año Nuevo Lunar" }, { "warring kingdoms", "Año Nuevo Lunar" }, { "lunar beast", "Año Nuevo Lunar" }, { "mythmaker", "Año Nuevo Lunar" }, { "heavenscale", "Año Nuevo Lunar" },
                
                // Combate, Calles y Profesiones
                { "high noon", "Forajidos" }, { "crime city", "Ciudad del Crimen" }, { "mafia", "Ciudad del Crimen" }, { "debonair", "Galante" },
                { "soul fighter", "Soul Fighter" }, { "street demon", "Demonios de la Calle" }, { "battle academia", "Academia de Combate" },
                { "arcade", "Arcadia" }, { "battle boss", "Arcadia" },
                { "empyrean", "Empíreo" }, { "vandal", "Vándalos" }, { "headhunter", "Cazador de Cabezas" }, { "infernal", "Infernal" }, { "warden", "Celadores" }, { "marauder", "Merodeadores" },
                
                // Lore, Regiones y Reinos
                { "bilgewater", "Aguasturbias" }, { "guardian of the sands", "Guardián de las Arenas" }, 
                { "order of the lotus", "Orden del Loto" }, { "shurima", "Shurima" }, { "freljord", "Freljord" },
                { "ionia", "Jonia" }, { "noxus", "Noxus" }, { "demacia", "Demacia" },
                
                // Naturaleza, Primal y Elementos
                { "prehistoric", "Prehistórico" }, { "beast hunter", "Cazadores de Bestias" }, 
                { "primal ambush", "Emboscada Primal" }, { "thunder lord", "Señor del Trueno" },
                { "scorch", "Fuego Infernal" }, { "deep sea", "Profundidades" },
                
                // Divertidas y Variadas
                { "bee", "Abejas" }, { "buzz", "Abejas" }, { "wasp", "Abejas" },
                { "cafe cuties", "Cafe Cuties" }, { "porcelain", "Porcelana" }, { "sugar rush", "Dulce Locura" }, { "candy", "Dulce Locura" },
                { "definitely not", "Día de las Bromas" }, { "meow", "Día de las Bromas" }, { "pug", "Día de las Bromas" }, { "corgi", "Día de las Bromas" },
                { "toy", "Juguetes" }, { "papercraft", "Manualidades" }, { "monster tamer", "Domadores de Monstruos" }, { "luchador", "Lucha Libre" },
                { "chef", "Cocineros" }, { "baker", "Cocineros" },
                
                // Histórico y Combatientes
                { "gladiator", "Gladiadores" }, { "warrior", "Guerreros" }, { "viking", "Vikingos" },
                { "triumphant", "Triunfante" }, { "traditional", "Tradicional" },
                
                // eSports, Competitivo y Especiales
                { "victorious", "Victorioso" }, { "championship", "Campeonato / Mundial" }, { "worlds", "Campeonato / Mundial" }, { "conqueror", "Conquistador" }, { "challenger", "Conquistador" },
                { "skt t1", "eSports" }, { "ssw", "eSports" }, { "ig ", "eSports" }, { "fpx ", "eSports" }, { "dwg ", "eSports" }, { "edg ", "eSports" }, { "drx ", "eSports" }, { "t1 ", "eSports" }, { "fnc ", "eSports" }, { "tpa ", "eSports" },
                { "arcane", "Arcane" }, { "hextech", "Hextech" }, { "prestige", "Edición Prestigiosa" },
                
                // Otras Líneas Menores
                { "eternum", "Eternum" }, { "heavy metal", "Heavy Metal" }, { "lancer", "Lanceros" },
                { "resistance", "Resistencia" }, { "overlord", "Soberanos" }
            };

            foreach (var champProp in champions) {
                var champData = champProp.First;
                string champId = champData["id"].ToString(); 
                string champName = champData["name"].ToString(); 
                string tags = champData["tags"].ToString();
                string linea = AsignarLinea(champId, tags);

                var skinData = champData["skins"];

                foreach (var skin in skinData) {
                    string skinNameRiot = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    string skinNum = skin["num"].ToString();

                    // Ignorar la skin base (la de número 0)
                    if (skinNum == "0" || skinNameRiot.ToLower() == "default") continue; 

                    // REGLA: Si no coincide con nada, se va a "Others"
                    string tema = "Others";
                    string skinNameLimpio = skinNameRiot.ToLower();
                    
                    foreach (var kw in tematicas) {
                        if (skinNameLimpio.Contains(kw.Key)) {
                            tema = kw.Value;
                            break; // Se detiene apenas encuentra su temática
                        }
                    }

                    await SaveSkinToDb(conn, skinIdRiot, skinNameRiot, champName, champId, linea, tema);
                }
            }
            Console.WriteLine("¡Sincronización masiva completada! Todas las skins fueron agrupadas.");
        } catch (Exception ex) { 
            Console.WriteLine("ERROR SYNC: " + ex.Message); 
        }
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
        if (tags.Contains("Assassin") || tags.Contains("Mage")) return "Mid";
        return "Flex";
    }

    private async Task SaveSkinToDb(MySqlConnection conn, string skinId, string nombre_skin, string campeon, string campeon_id, string linea, string tema) {
        int temaId;
        string queryTema = "INSERT IGNORE INTO Tematicas (nombre) VALUES (@tema); SELECT id_tematica FROM Tematicas WHERE nombre = @tema LIMIT 1;";
        using (var cmdTema = new MySqlCommand(queryTema, conn)) {
            cmdTema.Parameters.AddWithValue("@tema", tema);
            temaId = Convert.ToInt32(await cmdTema.ExecuteScalarAsync());
        }

        string querySkin = "INSERT IGNORE INTO Skins (id_skin_riot, id_tematica, nombre_skin, campeon, campeon_id, linea) VALUES (@sid, @tid, @nombre, @camp, @camp_id, @linea)";
        using (var cmdSkin = new MySqlCommand(querySkin, conn)) {
            cmdSkin.Parameters.AddWithValue("@sid", skinId);
            cmdSkin.Parameters.AddWithValue("@tid", temaId);
            cmdSkin.Parameters.AddWithValue("@nombre", nombre_skin);
            cmdSkin.Parameters.AddWithValue("@camp", campeon);
            cmdSkin.Parameters.AddWithValue("@camp_id", campeon_id);
            cmdSkin.Parameters.AddWithValue("@linea", linea);
            await cmdSkin.ExecuteNonQueryAsync();
        }
    }
}