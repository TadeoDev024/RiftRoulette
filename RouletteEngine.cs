using System;
using System.Collections.Generic;
using System.Linq;
using RiftRoulette.Models;

namespace RiftRoulette.Logic {
    public class RouletteService {
        
        // MÉTODOS QUE FALTABAN PARA QUE EL BUILD NO DE ERROR
       private List<dynamic> GetThemesSharedByAll(List<int> userIds) {
    using var conn = new MySqlConnection(_connectionString);
    conn.Open();
    // Condición A: Temáticas que poseen TODOS los N jugadores
    string query = @"
        SELECT t.id_tematica, t.nombre 
        FROM Usuario_Skins us
        JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
        JOIN Tematicas t ON s.id_tematica = t.id_tematica
        WHERE us.id_usuario IN (" + string.Join(",", userIds) + @")
        GROUP BY t.id_tematica
        HAVING COUNT(DISTINCT us.id_usuario) = @playerCount";

    var themes = new List<dynamic>();
    using var cmd = new MySqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@playerCount", userIds.Count);
    using var reader = cmd.ExecuteReader();
    while (reader.Read()) {
        themes.Add(new { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
    }
    return themes;
}

        private List<SkinDTO> GetUserSkinsInTheme(int userId, int themeId) {
            return new List<SkinDTO>(); // Temporalmente vacío
        }

        public MatchResult? ExecuteSpin(List<int> userIds) {
            var possibleThemes = GetThemesSharedByAll(userIds);

            foreach (var theme in possibleThemes) {
                var assignments = new Dictionary<int, SkinDTO>();
                if (CanAssign(userIds, theme.Id, 0, new HashSet<int>(), assignments)) {
                    return new MatchResult {
                        Tematica = theme.Nombre,
                        Assignments = assignments.Select(a => new Assignment {
                            UserId = a.Key,
                            SkinName = a.Value.Nombre,
                            SplashUrl = a.Value.Url
                        }).ToList()
                    };
                }
            }
            return null;
        }

        private bool CanAssign(List<int> users, int themeId, int idx, HashSet<int> usedChamps, Dictionary<int, SkinDTO> res) {
            if (idx == users.Count) return true;
            var userSkinsInTheme = GetUserSkinsInTheme(users[idx], themeId);
            foreach (var skin in userSkinsInTheme) {
                if (!usedChamps.Contains(skin.ChampionId)) {
                    usedChamps.Add(skin.ChampionId);
                    res[users[idx]] = skin;
                    if (CanAssign(users, themeId, idx + 1, usedChamps, res)) return true;
                    usedChamps.Remove(skin.ChampionId);
                }
            }
            return false;
        }
    }

    public class MatchResult {
        public string Tematica { get; set; } = "";
        public List<Assignment> Assignments { get; set; } = new();
    }

    public class Assignment {
        public int UserId { get; set; }
        public string SkinName { get; set; } = "";
        public string SplashUrl { get; set; } = "";
    }
}