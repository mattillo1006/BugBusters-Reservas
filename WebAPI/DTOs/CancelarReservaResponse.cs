namespace WebAPI.DTOs
{
    public class CancelarReservaResponse
    {
        public int ReservaId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCancelacion { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}