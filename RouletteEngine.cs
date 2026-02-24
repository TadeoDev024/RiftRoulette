using System;
using System.Collections.Generic;
using System.Linq;
using RiftRoulette.Models;

namespace RiftRoulette.Logic {
    public class RouletteService {
        
        // MÉTODOS QUE FALTABAN PARA QUE EL BUILD NO DE ERROR
        private List<dynamic> GetThemesSharedByAll(List<int> userIds) {
            return new List<dynamic>(); // Temporalmente vacío
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