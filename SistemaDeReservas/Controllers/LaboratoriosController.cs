using Microsoft.AspNetCore.Mvc;
using SistemaDeReservas.Models;

namespace SistemaDeReservas.Controllers
{
    // [Authorize]  // TODO: reactivar login
    public class LaboratoriosController : Controller
    {
        // TODO: reemplazar por llamadas a la WebAPI
        private static readonly List<Laboratorio> Demo = new()
        {
            new() { LaboratorioId = 1, Nombre = "Laboratorio de Química", Ubicacion = "Edificio A, piso 1", Capacidad = 30, Estado = "Disponible" },
            new() { LaboratorioId = 2, Nombre = "Laboratorio de Física", Ubicacion = "Edificio A, piso 2", Capacidad = 25, Estado = "Disponible" },
            new() { LaboratorioId = 3, Nombre = "Laboratorio de Cómputo", Ubicacion = "Edificio B, piso 1", Capacidad = 40, Estado = "FueraDeServicio" },
            new() { LaboratorioId = 4, Nombre = "Laboratorio de Biología", Ubicacion = "Edificio C, piso 1", Capacidad = 20, Estado = "Disponible" }
        };

        public IActionResult Disponibilidad(int id, DateTime? semana)
        {
            var lab = Demo.FirstOrDefault(l => l.LaboratorioId == id);
            if (lab is null) return NotFound();

            var apertura = TimeSpan.FromHours(7);
            var cierre = TimeSpan.FromHours(17);

            // Lunes de la semana pedida (si hoy es fin de semana, arranca el lunes siguiente)
            var hoy = DateTime.Today;
            var primerLunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            if (hoy.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) primerLunes = primerLunes.AddDays(7);

            var lunes = (semana ?? primerLunes).Date;
            lunes = lunes.AddDays(-(((int)lunes.DayOfWeek + 6) % 7));
            if (lunes < primerLunes) lunes = primerLunes;

            var vm = new DisponibilidadViewModel
            {
                Laboratorio = lab,
                HoraApertura = apertura,
                HoraCierre = cierre,
                InicioSemana = lunes,
                PuedeRetroceder = lunes > primerLunes,
                Dias = Enumerable.Range(0, 5).Select(i => lunes.AddDays(i)).ToList()
            };

            for (var h = apertura; h < cierre; h += TimeSpan.FromHours(1))
                vm.Horas.Add(h);

            // Reservas de ejemplo (patrón fijo para poder ver verdes y rojos)
            foreach (var dia in vm.Dias)
                foreach (var h in vm.Horas)
                    if ((dia.Day + (int)h.TotalHours + id) % 4 == 0)
                        vm.Ocupados.Add((dia, h));

            return View(vm);
        }
    }
}