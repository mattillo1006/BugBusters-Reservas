using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Hosting;
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
        public async Task <IActionResult>addAsync(int LabId, DateTime? fecha, TimeSpan? inicio,
            TimeSpan? fin, DateTime? semana)
        
        {
            var client = _factory.CreateClient("WebAPI");

            try
            {
                var response = await client.PostAsJsonAsync("api/Reservas", new { LabId, fecha, inicio, fin });

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }
                    if (response.IsSuccessStatusCode)
                    {
                        var ok = await response.Content.ReadFromJsonAsync<RespuestaApi>();
                        TempData["Exito"] = ok?.mensaje ?? "Reserva registrada correctamente.";
                    }
                    else 
                    {
                        string? mensaje = null;
                        try
                        {
                            mensaje = (await response.Content.ReadFromJsonAsync<RespuestaApi>())?.mensaje;
                        }
                        catch (JsonException){}
                        TempData["Error"] = mensaje ?? "No se pudo registrar la reserva";
                    }
            }
            catch (HttpRequestException)
            {

                TempData["Error"] = "Error al conectar con el servidor";
            }
            return RedirectToAction("Disponibilidad", "Laboratorios",
                new { id = LabId, Semana = semana?.ToString("yyyy-MM-dd")});


        }
    }
}
public record RespuestaApi(string? mensaje);
