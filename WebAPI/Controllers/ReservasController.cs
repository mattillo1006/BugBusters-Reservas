using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservasController : ControllerBase
    {
        private const int DuracionMaximaHoras = 4;
        private readonly ReservasDBContext _context;

        public ReservasController(ReservasDBContext context) => _context = context;

        // POST: api/Reservas
        [HttpPost]
        public async Task<ActionResult<ReservaResponse>> Crear([FromBody] CrearReservaRequest request)
        {
            
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
                return Unauthorized();

            
            var faltantes = new List<string>();
            if (request.LaboratorioId is null or <= 0) faltantes.Add("el laboratorio");
            if (request.Fecha is null) faltantes.Add("la fecha");
            if (request.HoraInicio is null) faltantes.Add("la hora de inicio");
            if (request.HoraFin is null) faltantes.Add("la hora de finalización");

            if (faltantes.Count > 0)
                return BadRequest(new { mensaje = $"Debe indicar {string.Join(", ", faltantes)}." });

            var labId = request.LaboratorioId!.Value;
            var fecha = request.Fecha!.Value.Date;
            var inicio = request.HoraInicio!.Value;
            var fin = request.HoraFin!.Value;

            var laboratorio = await _context.Laboratorios
                .FirstOrDefaultAsync(l => l.LaboratorioId == labId && l.Active);

            if (laboratorio is null)
                return NotFound(new { mensaje = "Laboratorio no encontrado." });

            
            var errores = new List<string>();

            if (fin <= inicio)
                errores.Add("La hora final debe ser posterior a la hora inicial.");

            if (fecha < DateTime.Today)
                errores.Add("No se pueden realizar reservas en fechas anteriores a la fecha actual.");

            if (inicio < laboratorio.HoraApertura || fin > laboratorio.HoraCierre)
                errores.Add($"El laboratorio solo puede reservarse entre {laboratorio.HoraApertura.ToString(@"hh\:mm")} y {laboratorio.HoraCierre.ToString(@"hh\:mm")}.");

            if (fin > inicio && fin - inicio > TimeSpan.FromHours(DuracionMaximaHoras))
                errores.Add($"Una reserva no puede superar las {DuracionMaximaHoras} horas.");

            if (errores.Count > 0)
                return BadRequest(new { mensaje = string.Join(" ", errores), errores });

            
            if (laboratorio.Estado == "FueraDeServicio")
                return Conflict(new { mensaje = "El laboratorio se encuentra fuera de servicio." });

            // verificación de traslape y el insert van en la misma transacción
            // para que dos personas no puedan reservar el mismo espacio a la vez
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var hayConflicto = await _context.Reservas.AnyAsync(r =>
                r.LaboratorioId == labId &&
                r.Estado == "Activa" &&
                r.Fecha.Date == fecha &&
                inicio < r.HoraFin &&
                fin > r.HoraInicio);

            if (hayConflicto)
                return Conflict(new { mensaje = "El laboratorio no se encuentra disponible en el horario solicitado." });

            var reserva = new Reserva
            {
                LaboratorioId = labId,
                UsuarioId = usuarioId,
                Fecha = fecha,
                HoraInicio = inicio,
                HoraFin = fin,
                Estado = "Activa",
                FechaRegistro = DateTime.Now,
                Active = true
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new ReservaResponse
            {
                ReservaId = reserva.ReservaId,
                LaboratorioId = labId,
                Laboratorio = laboratorio.Nombre,
                Fecha = reserva.Fecha,
                InicioReserva = reserva.HoraInicio,
                FinReserva = reserva.HoraFin,
                Estado = reserva.Estado,
                Mensaje = "Reserva registrada correctamente."
            });
        }
    }
}