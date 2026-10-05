using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeReservas.Models;

namespace SistemaDeReservas.Controllers
{
    //[Authorize]
    public class LaboratoriosController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public LaboratoriosController(IHttpClientFactory factory) => _factory = factory;

        public async Task<IActionResult> Disponibilidad(int id, DateTime? semana)
        {
            var client = _factory.CreateClient("WebAPI");

            Laboratorio? lab;
            try
            {
                var resp = await client.GetAsync($"api/Laboratorios/{id}");
                if (resp.StatusCode == HttpStatusCode.NotFound) return NotFound();
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }
                resp.EnsureSuccessStatusCode();
                lab = await resp.Content.ReadFromJsonAsync<Laboratorio>();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor.";
                return RedirectToAction("Index", "Home");
            }

            if (lab is null) return NotFound();
            if (!lab.Disponible)
            {
                TempData["Error"] = $"El laboratorio {lab.Nombre} se encuentra fuera de servicio.";
                return RedirectToAction("Index", "Home");
            }

            // Lunes de la semana pedida (si hoy es fin de semana, arranca el lunes siguiente)
            var hoy = DateTime.Today;
            var primerLunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            if (hoy.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) primerLunes = primerLunes.AddDays(7);

            var lunes = (semana ?? primerLunes).Date;
            lunes = lunes.AddDays(-(((int)lunes.DayOfWeek + 6) % 7));
            if (lunes < primerLunes) lunes = primerLunes;

            var apertura = lab.HoraApertura;
            var cierre = lab.HoraCierre;
            if (cierre <= apertura) { apertura = TimeSpan.FromHours(7); cierre = TimeSpan.FromHours(17); }

            var vm = new DisponibilidadViewModel
            {
                Laboratorio = lab,
                HoraApertura = apertura,
                HoraCierre = cierre,
                InicioSemana = lunes,
                PuedeRetroceder = lunes > primerLunes,
                Dias = Enumerable.Range(0, 5).Select(i => lunes.AddDays(i)).ToList()
            };

            for (var h = apertura; h + TimeSpan.FromHours(1) <= cierre; h += TimeSpan.FromHours(1))
                vm.Horas.Add(h);

            try
            {
                var respRes = await client.GetAsync(
                    $"api/Laboratorios/{id}/reservas?desde={lunes:yyyy-MM-dd}&hasta={lunes.AddDays(4):yyyy-MM-dd}");

                if (respRes.IsSuccessStatusCode)
                {
                    var reservas = await respRes.Content.ReadFromJsonAsync<List<ReservaOcupada>>() ?? new();
                    foreach (var r in reservas)
                        for (var h = r.HoraInicio; h < r.HoraFin; h += TimeSpan.FromHours(1))
                            vm.Ocupados.Add((r.Fecha.Date, h));
                }
            }
            catch (HttpRequestException) { /* si falla, se muestra todo libre */ }

            return View(vm);
        }
    }
}

public record ReservaOcupada(DateTime Fecha, TimeSpan HoraInicio, TimeSpan HoraFin);