using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using RiftRoulette.Models;

namespace RiftRoulette.Logic {
    public class RouletteService {
        private readonly string _connectionString;
        public RouletteService(string connectionString) { _connectionString = connectionString; }

        private List<dynamic> GetThemesSharedByAll(List<int> userIds) {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT t.id_tematica, t.nombre 
                FROM Usuario_Skins us
                JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
                JOIN Tematicas t ON s.id_tematica = t.id_tematica
                WHERE us.id_usuario IN (" + string.Join(",", userIds) + @")
                GROUP BY t.id_tematica
                HAVING COUNT(DISTINCT us.id_usuario) = " + userIds.Count;

            var themes = new List<dynamic>();
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                themes.Add(new { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
            }
            return themes;
        }
        // ... mantener el resto del código igual ...
    }
}   