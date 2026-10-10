namespace WebAPI.DTOs
{
    public class ReservaResponse
    {
        public int ReservaId { get; set; }
        public int LaboratorioId { get; set; }
        public string Laboratorio { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public TimeSpan InicioReserva { get; set; }
        public TimeSpan FinReserva { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
