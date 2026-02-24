public class RouletteService {
    public class MatchResult {
        public string Tematica { get; set; }
        public List<Assignment> Assignments { get; set; }
    }

    public class Assignment {
        public int UserId { get; set; }
        public string SkinName { get; set; }
        public string SplashUrl { get; set; }
    }

    public MatchResult ExecuteSpin(List<int> userIds) {
        // 1. Obtener de DB las temáticas que TODOS los N integrantes poseen
        var possibleThemes = GetThemesSharedByAll(userIds);

        foreach (var theme in possibleThemes.OrderBy(x => Guid.NewGuid())) { // Sorteo aleatorio
            var assignments = new Dictionary<int, SkinDTO>();
            
            // 2. Backtracking para asegurar campeones únicos entre los N jugadores
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
        return null; // No hay combinación válida
    }

    private bool CanAssign(List<int> users, int themeId, int idx, HashSet<int> usedChamps, Dictionary<int, SkinDTO> res) {
        if (idx == users.Count) return true;

        var userSkinsInTheme = GetUserSkinsInTheme(users[idx], themeId);
        foreach (var skin in userSkinsInTheme) {
            if (!usedChamps.Contains(skin.ChampionId)) {
                usedChamps.Add(skin.ChampionId);
                res[users[idx]] = skin;
                if (CanAssign(users, themeId, idx + 1, usedChamps, res)) return true;
                usedChamps.Remove(skin.ChampionId); // Backtrack
            }
        }
        return false;
    }
}