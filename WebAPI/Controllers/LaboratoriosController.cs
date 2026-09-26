using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
    }
}
