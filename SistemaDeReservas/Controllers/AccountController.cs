using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaDeReservas.Models;

namespace SistemaDeReservas.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _factory;
    public AccountController(IHttpClientFactory factory) => _factory = factory;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _factory.CreateClient("WebAPI");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/auth/login",
                new { NombreUsuario = model.Username, model.Password });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo conectar con el servidor");
            return View(model);
        }

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Usuario o contraseña incorrectos");
            return View(model);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result is null)
        {
            ModelState.AddModelError("", "Respuesta inválida del servidor");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UsuarioId.ToString()),
            new(ClaimTypes.Name, result.NombreUsuario),
            new(ClaimTypes.GivenName, result.NombreCompleto),
            new(ClaimTypes.Role, result.Rol)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}

public record LoginResponse(int UsuarioId, string NombreUsuario, string NombreCompleto, string Rol);