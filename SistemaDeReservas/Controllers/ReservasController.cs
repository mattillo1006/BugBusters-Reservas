using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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