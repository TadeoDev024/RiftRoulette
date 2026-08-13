using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Microsoft.Extensions.Logging;

namespace RiftRoulette.Services
{
    public class LobbyCleanupService : BackgroundService
    {
        private readonly LobbyStateTracker _tracker;
        private readonly string _connectionString;
        private readonly ILogger<LobbyCleanupService> _logger;
        private readonly int _timeoutSeconds = 15; // 15 seconds timeout

        public LobbyCleanupService(LobbyStateTracker tracker, IConfiguration configuration, ILogger<LobbyCleanupService> logger)
        {
            _tracker = tracker;
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                ?? configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupInactivePlayersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in LobbyCleanupService: {ex.Message}");
                }

                // Esperar 10 segundos antes del próximo chequeo
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task CleanupInactivePlayersAsync()
        {
            var now = DateTime.UtcNow;
            var allUsers = _tracker.GetAllLastSeen();
            
            var inactiveUsers = allUsers
                .Where(kvp => (now - kvp.Value).TotalSeconds > _timeoutSeconds)
                .Select(kvp => kvp.Key)
                .ToList();

            if (inactiveUsers.Count == 0) return;

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Eliminar a los usuarios inactivos de la tabla de LobbyPlayers
            string deletePlayersQuery = $"DELETE FROM LobbyPlayers WHERE user_id IN ({string.Join(",", inactiveUsers)})";
            using (var cmd = new MySqlCommand(deletePlayersQuery, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Limpiarlos del caché para no volver a borrarlos
            foreach (var userId in inactiveUsers)
            {
                _tracker.RemoveUser(userId);
                _logger.LogInformation($"Usuario inactivo {userId} removido de la sala.");
            }

            // 3. Eliminar salas que hayan quedado vacías
            string deleteEmptyLobbiesQuery = @"
                DELETE FROM Lobbies 
                WHERE code NOT IN (SELECT DISTINCT lobby_code FROM LobbyPlayers)";
            
            using (var cmdLobbies = new MySqlCommand(deleteEmptyLobbiesQuery, conn))
            {
                int deletedLobbies = await cmdLobbies.ExecuteNonQueryAsync();
                if (deletedLobbies > 0)
                {
                    _logger.LogInformation($"Se eliminaron {deletedLobbies} salas vacías.");
                }
            }
        }
    }
}
