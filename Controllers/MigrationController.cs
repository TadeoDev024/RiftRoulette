using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RiftRoulette.Controllers
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════════╗
    /// ║  CONTROLADOR DE MIGRACIÓN — USO ÚNICO                       ║
    /// ║                                                              ║
    /// ║  Propósito: Hashear con BCrypt todas las contraseñas que     ║
    /// ║  actualmente están en texto plano en la tabla Usuarios.      ║
    /// ║                                                              ║
    /// ║  INSTRUCCIONES:                                              ║
    /// ║  1. Deployar esta versión del código en Render.              ║
    /// ║  2. Llamar POST /api/Migration/hash-passwords                ║
    /// ║     con el header: X-Migration-Key: <valor de MIGRATION_KEY> ║
    /// ║  3. Verificar que el response diga "migrated > 0".           ║
    /// ║  4. ELIMINAR este archivo y hacer un segundo deploy.         ║
    /// ╚══════════════════════════════════════════════════════════════╝
    /// </summary>
    [ApiController]
    [Route("api/Migration")]
    public class MigrationController : ControllerBase
    {
        private readonly string _connectionString;

        // La clave se lee desde variable de entorno MIGRATION_KEY.
        // Fallback solo para desarrollo local.
        private static readonly string MIGRATION_KEY =
            Environment.GetEnvironmentVariable("MIGRATION_KEY")
            ?? "RIFTROULETTE_MIGRATE_LOCAL_DEV_ONLY";

        private const int BCRYPT_WORK_FACTOR = 12;

        public MigrationController(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                ?? configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        /// <summary>
        /// Lee todos los usuarios, detecta cuáles tienen contraseña en texto plano
        /// (las contraseñas BCrypt siempre empiezan con "$2a$" o "$2b$"),
        /// las hashea y actualiza la BD.
        /// Es idempotente: si se llama dos veces, la segunda no modifica nada.
        /// </summary>
        [HttpPost("hash-passwords")]
        public async Task<IActionResult> HashAllPasswords()
        {
            // ── Verificación de clave de migración ─────────────────────
            if (!Request.Headers.TryGetValue("X-Migration-Key", out var providedKey)
                || providedKey.ToString() != MIGRATION_KEY)
            {
                return Unauthorized(new { message = "Clave de migración inválida o ausente." });
            }

            var migrated  = new List<string>();
            var skipped   = new List<string>(); // Ya tenían hash
            var failed    = new List<string>(); // Error individual

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                // 1. Leer todos los usuarios
                var users = new List<(int Id, string Username, string Password)>();
                const string selectQuery = "SELECT id_usuario, username, password FROM Usuarios";
                using (var selectCmd = new MySqlCommand(selectQuery, conn))
                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add((
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2)
                        ));
                    }
                }

                // 2. Procesar cada usuario
                foreach (var (id, username, password) in users)
                {
                    try
                    {
                        // Detectar si ya es un hash BCrypt válido
                        // Los hashes BCrypt siempre tienen formato: $2a$12$... (60 chars)
                        bool alreadyHashed = password.StartsWith("$2a$")
                                          || password.StartsWith("$2b$")
                                          || password.StartsWith("$2y$");

                        if (alreadyHashed)
                        {
                            skipped.Add(username);
                            continue;
                        }

                        // Hashear la contraseña en texto plano
                        string newHash = BCrypt.Net.BCrypt.HashPassword(password, BCRYPT_WORK_FACTOR);

                        // Actualizar en BD
                        const string updateQuery = "UPDATE Usuarios SET password = @hash WHERE id_usuario = @id";
                        using var updateCmd = new MySqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@hash", newHash);
                        updateCmd.Parameters.AddWithValue("@id", id);
                        await updateCmd.ExecuteNonQueryAsync();

                        migrated.Add(username);
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{username}: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    message  = "Migración completada.",
                    migrated = migrated.Count,
                    skipped  = skipped.Count,
                    failed   = failed.Count,
                    details  = new
                    {
                        migratedUsers = migrated,
                        skippedUsers  = skipped,
                        failedUsers   = failed
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fatal en la migración: " + ex.Message });
            }
        }
    }
}
