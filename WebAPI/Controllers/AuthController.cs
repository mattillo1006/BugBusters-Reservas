using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ReservasDBContext _context;
    private readonly TokenService _tokenService;

    public AuthController(ReservasDBContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
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
            .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario);

        // Caso negativo: usuario no existe o contraseña incorrecta
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.Contraseña))
        {
            return Unauthorized(new { mensaje = "Credenciales incorrectas." });
        }

        // Caso negativo adicional: usuario inactivo
        if (!usuario.Activo)
        {
            return Unauthorized(new { mensaje = "El usuario se encuentra inactivo." });
        }

        // Caso positivo
        var token = _tokenService.GenerarToken(usuario);

        var response = new LoginResponse
        {
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol?.NombreRol ?? string.Empty,
            Token = token
        };

        return Ok(response);
    }
}
