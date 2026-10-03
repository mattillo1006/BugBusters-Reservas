using System.Reflection.Metadata.Ecma335;

namespace WebAPI.Models
{
    public class Reserva
    {
        public int ReservaId { get; set; }
        public int LaboratorioId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Active { get; set; }
    }
}
