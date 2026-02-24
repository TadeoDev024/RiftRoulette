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

            // DICCIONARIO MASIVO: Cubre el 99% de las temáticas del juego
            var tematicas = new List<(string key, string theme)> {
                ("proyecto", "PROYECTO"), ("project", "PROYECTO"),
                ("luna sangrienta", "Luna Sangrienta"), ("blood moon", "Luna Sangrienta"),
                ("estrella oscura", "Estrella Oscura"), ("dark star", "Estrella Oscura"),
                ("guardiana estelar", "Guardianas Estelares"), ("star guardian", "Guardianas Estelares"),
                ("pijamada estelar", "Guardianas Estelares"), ("némesis estelar", "Guardianas Estelares"),
                ("forajid", "Forajidos"), ("solo ante el peligro", "Forajidos"), ("high noon", "Forajidos"),
                ("k/da", "K/DA"), ("kda", "K/DA"),
                ("true damage", "True Damage"),
                ("pentakill", "Pentakill"),
                ("heartsteel", "HEARTSTEEL"),
                ("arcade", "Arcade"), ("recreativa", "Arcade"), ("jefe de batalla", "Arcade"), ("battle boss", "Arcade"),
                ("piscina", "Pool Party"), ("pool party", "Pool Party"), ("veranieg", "Pool Party"), ("canción del océano", "Pool Party"),
                ("florecer espiritual", "Florecer Espiritual"), ("spirit blossom", "Florecer Espiritual"),
                ("odisea", "Odisea"), ("odyssey", "Odisea"),
                ("academia de combate", "Academia de Combate"),
                ("onda espacial", "Onda Espacial"), ("space groove", "Onda Espacial"),
                ("empíreo", "Empíreo"), ("empyrean", "Empíreo"),
                ("peleador de almas", "Peleador de Almas"), ("soul fighter", "Peleador de Almas"),
                ("tinta sombría", "Tinta Sombría"), ("inkshadow", "Tinta Sombría"),
                ("luna nevada", "Luna Nevada"), ("snow moon", "Luna Nevada"),
                ("caballero de la ceniza", "Caballero de la Ceniza"), ("ashen knight", "Caballero de la Ceniza"),
                ("creador de mitos", "Creador de Mitos"), ("mythmaker", "Creador de Mitos"),
                ("bestia lunar", "Bestia Lunar"),
                ("deleite lunar", "Deleite Lunar"), ("dinamita", "Deleite Lunar"), ("firecracker", "Deleite Lunar"),
                ("reinos mecha", "Reinos Mecha"), ("mecha kingdoms", "Reinos Mecha"),
                ("rosa de cristal", "Rosa de Cristal"), ("rosa marchita", "Rosa de Cristal"), ("crystal rose", "Rosa de Cristal"),
                ("galante", "Galante"), ("debonair", "Galante"),
                ("ciudad del crimen", "Ciudad del Crimen"), ("mafios", "Ciudad del Crimen"), ("crime city", "Ciudad del Crimen"),
                ("domador de monstruos", "Domador de Monstruos"),
                ("noche de miedo", "Noche de Miedo"), ("fright night", "Noche de Miedo"),
                ("embrujada", "Halloween"), ("zombi", "Halloween"), ("conde", "Halloween"), ("nosferatu", "Halloween"), ("calabaza", "Halloween"),
                ("bendición invernal", "Invierno"), ("maravilla invernal", "Invierno"), ("día nevado", "Invierno"), ("elfo", "Invierno"), ("santa", "Invierno"), ("nieve", "Invierno"),
                ("buscacorazones", "Buscacorazones"), ("cariñosit", "Buscacorazones"), ("cupido", "Buscacorazones"), ("heartseeker", "Buscacorazones"),
                ("victorios", "Victoriosa"), ("victorious", "Victoriosa"),
                ("campeonato", "Campeonato"), ("championship", "Campeonato"),
                ("conquistador", "Conquistador"), ("conqueror", "Conquistador"),
                ("aspirante", "Aspirante"), ("challenger", "Aspirante"),
                ("hextech", "Hextech"),
                ("prestigios", "Edición Prestigiosa"), ("prestigio", "Edición Prestigiosa"),
                ("arcane", "Arcane"),
                ("astronauta", "Astronauta"), ("astronaut", "Astronauta"),
                ("abeja", "Abejitas"), ("abejita", "Abejitas"), ("bee", "Abejitas"),
                ("porcelana", "Porcelana"), ("porcelain", "Porcelana"),
                ("cafetería", "Cafetería"), ("cafe cuties", "Cafetería"),
                ("chef", "Culinarios"), ("panader", "Culinarios"), ("carnicer", "Culinarios"), ("sushiman", "Culinarios"), ("cociner", "Culinarios"),
                ("comando", "Comando"), ("commando", "Comando"),
                ("cyber pop", "Cyber Pop"),
                ("definitivamente no", "Definitivamente No"),
                ("dracomante", "Dracomantes"), ("dragón", "Dragones"), ("matadragones", "Dragones"),
                ("terror nova", "Terror Nova"), ("dreadnova", "Terror Nova"),
                ("fábulas", "Fábulas"),
                ("gótico", "Gótico"),
                ("cazador de cabezas", "Cazador de Cabezas"), ("headhunter", "Cazador de Cabezas"),
                ("lucha libre", "Lucha Libre"), ("enmascarad", "Lucha Libre"), ("el león", "Lucha Libre"), ("el macho", "Lucha Libre"), ("el rayo", "Lucha Libre"),
                ("merodeador", "Merodeadores y Celadores"), ("celador", "Merodeadores y Celadores"),
                ("escuadrón omega", "Escuadrón Omega"),
                ("de papel", "De Papel"), ("papercraft", "De Papel"),
                ("faraón", "Guardián de las Arenas"), ("arenas", "Guardián de las Arenas"), ("sandstorm", "Guardián de las Arenas"),
                ("pirata", "Aguas Turbias"), ("aguas turbias", "Aguas Turbias"), ("bucaner", "Aguas Turbias"), ("bilgewater", "Aguas Turbias"), ("corsari", "Aguas Turbias"), ("capitán", "Aguas Turbias"),
                ("pretoriano", "Pretoriano"),
                ("prehistórico", "Prehistórico"), ("dinosaurio", "Prehistórico"),
                ("programa", "Programa"), ("program ", "Programa"),
                ("riot", "Riot"),
                ("agente secreto", "Agente Secreto"),
                ("hoja relámpago", "Hoja Relámpago"), ("shockblade", "Hoja Relámpago"),
                ("valquiria", "Valquirias de Acero"), ("almirante", "Valquirias de Acero"),
                ("frenesí azucarado", "Frenesí Azucarado"), ("dulce", "Frenesí Azucarado"), ("sugar rush", "Frenesí Azucarado"),
                ("juguete", "Juguetes"), ("toy", "Juguetes"), ("muñec", "Juguetes"),
                ("vándalo", "Vándalos"), ("vandal", "Vándalos"),
                ("rompemundos", "Rompemundos"), ("worldbreaker", "Rompemundos"),
                ("juegos del cénit", "Juegos del Cénit"),
                ("pacto quebrado", "Pacto Quebrado"), ("broken covenant", "Pacto Quebrado"),
                ("corte feérica", "Corte Feérica"), ("faerie court", "Corte Feérica"),
                ("demonios callejeros", "Demonios Callejeros"), ("street demon", "Demonios Callejeros"),
                ("escamas celestiales", "Escamas Celestiales"), ("heavenscale", "Escamas Celestiales"),
                ("emboscada primigenia", "Emboscada Primigenia"), ("primal ambush", "Emboscada Primigenia"),
                ("aquelarre", "Aquelarre"), ("coven", "Aquelarre"),
                ("bosqueviejo", "Bosqueviejo"), ("elderwood", "Bosqueviejo"),
                ("eclipse", "Eclipse"),
                ("arruinado", "Arruinados"), ("ruined", "Arruinados"),
                ("centinela", "Centinelas de la Luz"), ("sentinel", "Centinelas de la Luz"),
                ("meca", "Meca"), ("mecha ", "Meca"),
                ("máquina de guerra", "Máquina de Guerra"), ("battlecast", "Máquina de Guerra"),
                ("escarcha oscura", "Escarcha Oscura"), ("blackfrost", "Escarcha Oscura"),
                ("infernal", "Infernal"), ("fuego sombrío", "Infernal"), ("volcánic", "Infernal"),
                ("luz celestial", "Luz Celestial"), ("arclight", "Luz Celestial"), ("justicier", "Luz Celestial"),
                ("portador del amanecer", "Amanecer y Anochecer"), ("portador de la noche", "Amanecer y Anochecer"), ("amanecer", "Amanecer y Anochecer"), ("anochecer", "Amanecer y Anochecer"), ("dawnbringer", "Amanecer y Anochecer"), ("nightbringer", "Amanecer y Anochecer"),
                ("crystalis", "Crystalis Motus"),
                ("fnc ", "eSports"), ("tpa ", "eSports"), ("skt t1", "eSports"), ("ssw ", "eSports"), ("ig ", "eSports"), ("fpx ", "eSports"), ("dwg ", "eSports"), ("edg ", "eSports"), ("drx ", "eSports"), ("t1 ", "eSports"),
                ("supergaláctic", "Supergalácticos"),
                ("psyops", "PsyOps"),
                ("ilusión lunar", "Ilusión Lunar"),
                ("rey dios", "Reyes Dioses"), ("dios rey", "Reyes Dioses"),
                ("purgador", "Purgadores"),
                ("inmortal", "Viaje Inmortal"), ("espada divina", "Viaje Inmortal"), ("báculo divino", "Viaje Inmortal"),
                ("shurima", "Shurima"), ("freljord", "Freljord"), ("noxus", "Noxus"), ("noxian", "Noxus"), ("demacia", "Demacia"), ("jonia", "Jonia"),
                ("reinos combatientes", "Reinos Combatientes"), ("warring kingdoms", "Reinos Combatientes")
            };

            foreach (var champProp in champions) {
                string champId = champProp.First["id"].ToString();
                var detailRaw = await client.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/es_ES/champion/{champId}.json");
                var skinData = JObject.Parse(detailRaw)["data"][champId]["skins"];

                foreach (var skin in skinData) {
                    string skinName = skin["name"].ToString();
                    string skinIdRiot = skin["id"].ToString();
                    
                    // ATRIBUTO 'NUM': La clave definitiva para filtrar aspectos base
                    string skinNum = skin["num"].ToString();

                    // Si el número de la skin es 0, ES un aspecto base. Lo ignoramos.
                    if (skinNum == "0") {
                        continue; 
                    }

                    // Si pasa el filtro, empezamos asumiendo que es una skin única
                    string tema = "Otras / Únicas";
                    
                    foreach (var kw in tematicas) {
                        if (skinName.ToLower().Contains(kw.key)) {
                            tema = kw.theme;
                            break;
                        }
                    }

                    await SaveSkinToDb(conn, skinIdRiot, skinName, tema);
                }
            }
        } catch (Exception ex) { Console.WriteLine("ERROR SYNC: " + ex.Message); }
    }

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