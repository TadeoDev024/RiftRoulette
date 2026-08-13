using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RiftRoulette.Helpers
{
    public static class AuthHelper
    {
        // Hash password con BCrypt
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            try 
            {
                // Intenta verificar como hash de BCrypt
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // Fallback de retrocompatibilidad: si el hash antiguo es texto plano y no BCrypt, el verify arrojaría excepción de "Invalid salt version".
                return password == hash;
            }
        }

        // Generar token JWT
        public static string GenerateJwtToken(string username, int userId, string secret)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}