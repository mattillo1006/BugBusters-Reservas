using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeReservas.Models;

namespace SistemaDeReservas.Controllers
{
    //[Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public HomeController(IHttpClientFactory factory) => _factory = factory;

        public async Task<IActionResult> Index()
        {
            var client = _factory.CreateClient("WebAPI"); // el handler agrega el JWT
            try
            {
                var resp = await client.GetAsync("api/Laboratorios");

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // El token de la API venció: se cierra la sesión y se pide login de nuevo
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                resp.EnsureSuccessStatusCode();
                var labs = await resp.Content.ReadFromJsonAsync<List<Laboratorio>>();
                return View(labs ?? new List<Laboratorio>());
            }
            catch (HttpRequestException)
            {
                ViewData["Error"] = "No se pudo cargar la lista de laboratorios. Verifica que la WebAPI esté en ejecución.";
                return View(new List<Laboratorio>());
            }
        }

        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}