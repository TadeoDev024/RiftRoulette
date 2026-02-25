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
            
            // CAMBIO CLAVE: Cambiamos es_ES por en_US
            var champsRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/en_US/champion.json");
            var champions = JObject.Parse(champsRaw)["data"];

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // DICCIONARIO EN INGLÉS: Mucho más exacto, sin tildes ni géneros
            var tematicas = new Dictionary<string, string> {
                { "project", "PROJECT" },
                { "blood moon", "Blood Moon" },
                { "dark star", "Dark Star" }, { "cosmic", "Cosmic" }, { "dark cosmic", "Dark Star" },
                { "star guardian", "Star Guardian" }, { "star nemesis", "Star Guardian" }, { "pajama guardian", "Star Guardian" },
                { "high noon", "High Noon" },
                { "k/da", "K/DA" }, { "kda", "K/DA" }, { "true damage", "True Damage" }, { "pentakill", "Pentakill" }, { "heartsteel", "HEARTSTEEL" },
                { "arcade", "Arcade" }, { "battle boss", "Arcade" },
                { "pool party", "Pool Party" }, { "ocean song", "Pool Party" },
                { "spirit blossom", "Spirit Blossom" },
                { "odyssey", "Odyssey" },
                { "battle academia", "Battle Academia" },
                { "space groove", "Space Groove" },
                { "empyrean", "Empyrean" },
                { "soul fighter", "Soul Fighter" },
                { "inkshadow", "Inkshadow" },
                { "snow moon", "Snow Moon" },
                { "ashen", "Ashen Knights" },
                { "mythmaker", "Mythmaker" },
                { "lunar beast", "Lunar Revel" }, { "firecracker", "Lunar Revel" }, { "lunar wraith", "Lunar Revel" }, { "warring kingdoms", "Lunar Revel" },
                { "mecha kingdoms", "Mecha Kingdoms" },
                { "crystal rose", "Crystal Rose" }, { "withered rose", "Crystal Rose" },
                { "debonair", "Debonair" },
                { "crime city", "Crime City" }, { "mafia", "Crime City" },
                { "monster tamer", "Monster Tamers" },
                { "fright night", "Fright Night" },
                { "bewitching", "Halloween" }, { "zombie", "Halloween" }, { "count", "Halloween" }, { "nosferatu", "Halloween" }, { "pumpkin", "Halloween" }, { "trick or treat", "Halloween" },
                { "winterblessed", "Winter" }, { "winter wonder", "Winter" }, { "snow day", "Winter" }, { "santa", "Winter" }, { "elf", "Winter" }, { "frost", "Winter" },
                { "heartseeker", "Heartbreakers" }, { "sweetheart", "Heartbreakers" }, { "heartpiercer", "Heartbreakers" },
                { "victorious", "Victorious" },
                { "championship", "Championship / Worlds" }, { "worlds", "Championship / Worlds" },
                { "conqueror", "Conqueror" },
                { "hextech", "Hextech" },
                { "prestige", "Prestige Edition" },
                { "arcane", "Arcane" },
                { "astronaut", "Astronaut" },
                { "bee", "Bees!" }, { "buzz", "Bees!" }, { "wasp", "Bees!" },
                { "porcelain", "Porcelain" },
                { "cafe cuties", "Cafe Cuties" },
                { "chef", "Culinary Masters" }, { "baker", "Culinary Masters" }, { "butcher", "Culinary Masters" }, { "sushi", "Culinary Masters" }, { "pizza", "Culinary Masters" },
                { "commando", "Commando" },
                { "cyber pop", "Cyber Pop" },
                { "definitely not", "April Fools" }, { "meow", "April Fools" }, { "pug", "April Fools" }, { "corgi", "April Fools" }, { "fuzz", "April Fools" }, { "pretty kitty", "April Fools" },
                { "dragonmancer", "Dragonmancers" }, { "dragon slayer", "Dragon Slayers" }, { "dragon trainer", "Dragon Trainers" },
                { "dreadnova", "Dreadnova" },
                { "shan hai", "Shan Hai Scrolls" },
                { "fables", "Fables" },
                { "goth", "Goth" }, { "nightmare", "Goth" },
                { "headhunter", "Headhunter" },
                { "luchador", "Lucha Libre" }, { "el leon", "Lucha Libre" }, { "el macho", "Lucha Libre" }, { "el rayo", "Lucha Libre" }, { "el tigre", "Lucha Libre" },
                { "marauder", "Marauders & Wardens" }, { "warden", "Marauders & Wardens" },
                { "omega squad", "Omega Squad" },
                { "papercraft", "Papercraft" },
                { "guardian of the sands", "Guardian of the Sands" }, { "pharaoh", "Guardian of the Sands" }, { "sandstorm", "Guardian of the Sands" },
                { "bilgewater", "Bilgewater" }, { "pirate", "Bilgewater" }, { "buccaneer", "Bilgewater" }, { "corsair", "Bilgewater" }, { "captain", "Bilgewater" }, { "cutpurse", "Bilgewater" }, { "ironside", "Bilgewater" },
                { "praetorian", "Praetorian" },
                { "prehistoric", "Prehistoric" }, { "dino", "Prehistoric" },
                { "program", "Program" },
                { "riot", "Riot" },
                { "secret agent", "Secret Agent" },
                { "shockblade", "Shockblade" },
                { "steel valkyrie", "Steel Valkyries" }, { "admiral", "Steel Valkyries" }, { "bullet angel", "Steel Valkyries" },
                { "sugar rush", "Sugar Rush" }, { "candy", "Sugar Rush" }, { "lollipoppy", "Sugar Rush" }, { "bittersweet", "Sugar Rush" },
                { "toy", "Toys" }, { "doll", "Toys" }, { "ragdoll", "Toys" },
                { "vandal", "Vandals" },
                { "worldbreaker", "Worldbreaker" },
                { "faerie court", "Faerie Court" },
                { "street demon", "Street Demons" },
                { "heavenscale", "Heavenscale" },
                { "primal ambush", "Primal Ambush" },
                { "coven", "Coven" }, { "old god", "Coven" },
                { "elderwood", "Elderwood" },
                { "eclipse", "Eclipse" },
                { "ruined", "Ruined" },
                { "sentinel", "Sentinels of Light" },
                { "mecha", "Mecha" },
                { "battlecast", "Battlecast" }, { "resistance", "Battlecast" },
                { "blackfrost", "Blackfrost" },
                { "infernal", "Infernal" }, { "volcanic", "Infernal" },
                { "arclight", "Arclight" }, { "justicar", "Arclight" },
                { "dawnbringer", "Night & Dawn" }, { "nightbringer", "Night & Dawn" },
                { "crystalis", "Crystalis Motus" },
                { "fnc ", "eSports" }, { "tpa ", "eSports" }, { "skt t1", "eSports" }, { "ssw ", "eSports" }, { "ig ", "eSports" }, { "fpx ", "eSports" }, { "dwg ", "eSports" }, { "edg ", "eSports" }, { "drx ", "eSports" }, { "t1 ", "eSports" },
                { "super galaxy", "Super Galaxy" },
                { "psyops", "PsyOps" },
                { "god-king", "God-Kings" }, { "god king", "God-Kings" },
                { "immortal journey", "Immortal Journey" }, { "divine sword", "Immortal Journey" }, { "enduring sword", "Immortal Journey" }, { "majestic empress", "Immortal Journey" }, { "splendid staff", "Immortal Journey" }, { "soaring sword", "Immortal Journey" },
                { "eternum", "Eternum" },
                { "pulsefire", "Pulsefire" },
                { "demacia vice", "Demacia Vice" },
                { "striker", "Sports" }, { "goalkeeper", "Sports" }, { "sweeper", "Sports" }, { "dunkmaster", "Sports" }, { "playmaker", "Sports" },
                { "pax ", "PAX" },
                { "rpg", "RPG" }, { "whitebeard", "RPG" }, { "lionheart", "RPG" }, { "braum lionheart", "RPG" }, { "gragas caskbreaker", "RPG" }, { "ryze whitebeard", "RPG" }, { "varus swiftbolt", "RPG" },
                { "battleborn", "RPG" }
            };

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                string champName = champProp.First["name"].ToString();
                string tags = champProp.First["tags"].ToString();
                string linea = AsignarLinea(champId, tags);

                await Task.Delay(25); // Evitar saturar API

                // CAMBIO CLAVE: Cambiamos es_ES por en_US aquí también
                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/en_US/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinNameRiot = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    string skinNum = skin["num"].ToString();

                    if (skinNum == "0" || skinNameRiot.ToLower() == "default") continue; // Filtrar aspectos base

                    string tema = "Other / Unique"; // Ahora la categoría base también es en inglés
                    string skinNameLimpio = skinNameRiot.ToLower();
                    
                    foreach (var kw in tematicas) {
                        if (skinNameLimpio.Contains(kw.Key)) {
                            tema = kw.Value;
                            break;
                        }
                    }

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