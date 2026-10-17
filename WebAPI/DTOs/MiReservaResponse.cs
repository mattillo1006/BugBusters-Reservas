namespace WebAPI.DTOs
{
    // Una reserva del usuario autenticado, con los datos del laboratorio ya resueltos
    public class MiReservaResponse
    {
        public int ReservaId { get; set; }
        public int LaboratorioId { get; set; }
        public string Laboratorio { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}
