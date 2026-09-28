using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Models;

namespace WebAPI.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(Usuario usuario)
        {
            var jwtConfig = _configuration.GetSection("Jwt");

            var key = jwtConfig["Key"]
                ?? throw new InvalidOperationException("No existe una configuracion con Key en appsettings.json");

            var signKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credenciales = new SigningCredentials(signKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
                new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new(ClaimTypes.Name, usuario.NombreUsuario),
                new(ClaimTypes.GivenName, usuario.NombreCompleto),
                new(ClaimTypes.Role, usuario.Rol?.NombreRol ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var minutosExpiracion = jwtConfig.GetValue<double?>("ExpiresInMinutes") ?? 60;

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutosExpiracion),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
