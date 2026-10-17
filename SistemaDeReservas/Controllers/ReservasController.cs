using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeReservas.Models;

namespace SistemaDeReservas.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public ReservasController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // GET: /Reservas?filtro=proximas|historial
        [HttpGet]
        public async Task<IActionResult> Index(string? filtro)
        {
            var vm = new MisReservasViewModel
            {
                Filtro = string.Equals(filtro, MisReservasViewModel.FiltroHistorial, StringComparison.OrdinalIgnoreCase)
                    ? MisReservasViewModel.FiltroHistorial
                    : MisReservasViewModel.FiltroProximas,
                Ahora = DateTime.Now
            };

            var client = _factory.CreateClient("WebAPI"); // el handler agrega el JWT

            try
            {
                var response = await client.GetAsync("api/Reservas/mias");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // El token de la API venció: se cierra la sesión y se pide login de nuevo
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewData["Error"] = $"La WebAPI respondió {(int)response.StatusCode} al pedir tus reservas. Verifica que esté actualizada y en ejecución.";
                    return View(vm);
                }

                var reservas = await response.Content.ReadFromJsonAsync<List<MiReserva>>() ?? new();

                vm.Proximas = reservas
                    .Where(r => r.ObtenerEstado(vm.Ahora) is EstadoReserva.Proxima or EstadoReserva.EnCurso)
                    .OrderBy(r => r.Inicio)
                    .ToList();

                vm.Historial = reservas
                    .Where(r => r.ObtenerEstado(vm.Ahora) is EstadoReserva.Finalizada or EstadoReserva.Cancelada)
                    .OrderByDescending(r => r.Inicio)
                    .ToList();
            }
            catch (HttpRequestException)
            {
                ViewData["Error"] = "No se pudieron cargar tus reservas. Verifica que la WebAPI esté en ejecución.";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int laboratorioId, DateTime? fecha,
            TimeSpan? horaInicio, TimeSpan? horaFin, DateTime? semana)
        {
            var client = _factory.CreateClient("WebAPI");

            try
            {
                var response = await client.PostAsJsonAsync("api/Reservas",
                    new { laboratorioId, fecha, horaInicio, horaFin });

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                if (response.IsSuccessStatusCode)
                {
                    var ok = await response.Content.ReadFromJsonAsync<RespuestaApi>();
                    TempData["Exito"] = ok?.Mensaje ?? "Reserva registrada correctamente.";
                }
                else
                {
                    string? mensaje = null;
                    try
                    {
                        mensaje = (await response.Content.ReadFromJsonAsync<RespuestaApi>())?.Mensaje;
                    }
                    catch (JsonException) { }

                    TempData["Error"] = mensaje ?? "No se pudo registrar la reserva.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor.";
            }

            return RedirectToAction("Disponibilidad", "Laboratorios",
                new { id = laboratorioId, semana = semana?.ToString("yyyy-MM-dd") });
        }
    }

    public record RespuestaApi(string? Mensaje);
}