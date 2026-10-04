using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LaboratoriosController : ControllerBase
    {
        private readonly ReservasDBContext _context;

        public LaboratoriosController(ReservasDBContext context)
        {
            _context = context;
        }
        // Get: api/Laboratorios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LaboratorioResponse>>> GetAll()
        {
            var laboratorios = await _context.Laboratorios
                .Where(l => l.Active)
                .Select(l=> new LaboratorioResponse 
            {
                LaboratorioId = l.LaboratorioId,
                Nombre = l.Nombre,
                Ubicacion = l.Ubicacion,
                Capacidad = l.Capacidad,
                Estado = l.Estado
            }).ToListAsync();
            return Ok(laboratorios);
        }

        //Get: api/Laboratorios/id
        [HttpGet("{id}")]
        public async Task<ActionResult<LaboratorioResponse>> GetById(int id)
        {
            var laboratorio = await _context.Laboratorios
                              .Where(l => l.LaboratorioId == id && l.Active)
                              .Select(l => new LaboratorioResponse
                              { 
                              LaboratorioId= l.LaboratorioId,
                              Nombre= l.Nombre,
                              Ubicacion= l.Ubicacion,
                              Capacidad= l.Capacidad,
                              Estado= l.Estado
                              }).FirstOrDefaultAsync();

            if (laboratorio is null)
            {
                return NotFound(new { mensaje = "Laboratorio no encontrado" });
            }
            return Ok(laboratorio);
        }


        // GET: api/Laboratorios/5/disponibilidad?fecha=2026-10-10&horaInicio=09:00&horaFin=11:00
        [HttpGet("{id}/disponibilidad")]
        public async Task<ActionResult<DisponibilidadResponse>> ConsultarDisponibilidad
            (int id, [FromQuery] DateTime fecha, [FromQuery] TimeSpan horaInicio, [FromQuery] TimeSpan horaFin)
        {
            // los fromQuerry son parametros de la disponibilidad que tambien se van en la consulta
            // la reserva es exclusiva de un solo dia, no se pueden reservar varios dias seguidos

            var laboratorio = await _context.Laboratorios
                .FirstOrDefaultAsync(l => l.LaboratorioId == id && l.Active);

            if (laboratorio is null)
            {
                return NotFound(new { mensaje = "Laboratorio no encontrado." });
            }

            // Validacioes de horario 
            if (horaFin <= horaInicio)
            {
                return BadRequest(new { mensaje = "La hora final debe ser posterior a la hora inicial." });
            }

            if (fecha.Date < DateTime.Today)
            {
                return BadRequest(new { mensaje = "No se puede consultar disponibilidad en fechas pasadas." });
            }

            if (horaInicio < laboratorio.HoraApertura || horaFin > laboratorio.HoraCierre)
            {
                return BadRequest(new { mensaje = $"El laboratorio solo opera entre {laboratorio.HoraApertura} y {laboratorio.HoraCierre}." });
            }

            // Laboratorio fuera de servicio nunca está disponible
            if (laboratorio.Estado == "FueraDeServicio")
            {
                return Ok(new DisponibilidadResponse
                {
                    Disponible = false,
                    Mensaje = "El laboratorio se encuentra fuera de servicio."
                });
            }

            // Verificación de conflictos con reservas activas (las canceladas no afectan disponibilidad)
            var hayConflicto = await _context.Reservas.AnyAsync(r =>
                r.LaboratorioId == id &&
                r.Estado == "Activa" &&
                r.Fecha.Date == fecha.Date &&
                horaInicio < r.HoraFin &&
                horaFin > r.HoraInicio);

            if (hayConflicto)
            {
                return Ok(new DisponibilidadResponse
                {
                    Disponible = false,
                    Mensaje = "El laboratorio no se encuentra disponible en el horario solicitado."
                });
            }

            return Ok(new DisponibilidadResponse
            {
                Disponible = true,
                Mensaje = "El laboratorio se encuentra disponible."
            });
        }

        // GET: api/Laboratorios/1/reservas?desde=2026-10-05&hasta=2026-10-09
        [HttpGet("{id}/reservas")]
        public async Task<ActionResult<IEnumerable<ReservaOcupadaResponse>>> GetReservasRango(
            int id, [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var reservas = await _context.Reservas
                .Where(r => r.LaboratorioId == id
                         && r.Estado == "Activa"
                         && r.Fecha >= desde.Date
                         && r.Fecha <= hasta.Date)
                .Select(r => new ReservaOcupadaResponse
                {
                    Fecha = r.Fecha,
                    HoraInicio = r.HoraInicio,
                    HoraFin = r.HoraFin
                })
                .ToListAsync();

            return Ok(reservas);
        }
    }
}
