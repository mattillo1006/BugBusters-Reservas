namespace WebAPI.DTOs
{
    public class ReservaOcupadaResponse
    {
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }
}
