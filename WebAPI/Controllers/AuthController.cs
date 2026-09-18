using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;

namespace WebAPI;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ReservasDBContext _context;

    public AuthController(ReservasDBContext context)
    {
        _context = context;
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { mensaje = "Usuario y contraseña son obligatorios." });
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario && u.Activo);

        // Caso negativo: usuario no existe o contraseña incorrecta
        if (usuario is null || usuario.PasswordHash != request.Password)
        {
            return Unauthorized(new { mensaje = "Credenciales incorrectas." });
        }

        // Caso negativo adicional: usuario inactivo
        if (!usuario.Activo)
        {
            return Unauthorized(new { mensaje = "El usuario se encuentra inactivo." });
        }

        // Caso positivo
        var response = new LoginResponse
        {
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol?.NombreRol ?? string.Empty
        };

        return Ok(response);
    }
}
