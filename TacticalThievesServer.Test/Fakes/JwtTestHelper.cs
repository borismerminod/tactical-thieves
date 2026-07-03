using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TacticalThievesServer.Test.Fakes
{
    public static class JwtTestHelper
    {
        private const string TestKey = "test-jwt-key-for-testing-purposes-32chars!";

        public static string GenerateToken(string username, bool expired = false)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] { new Claim("username", username) };

            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                claims: claims,
                notBefore: expired ? now.AddHours(-2) : now,
                expires: expired ? now.AddHours(-1) : now.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
