namespace WebAPI.Models
{
    public class Cancelacion
    {
        public int CancelacionId { get; set; }
        public int ReservaId { get; set; }
        public int CanceladaPorId { get; set; }
        public DateTime FechaCancelacion { get; set; }
        public string? Motivo { get; set; }
        public bool Active { get; set; }
    }
}