namespace WebAPI.DTOs;

public class CrearReservaRequest
{
    public int? LaboratorioId { get; set; }
    public DateTime? Fecha { get; set; }
    public TimeSpan? HoraInicio { get; set; }
    public TimeSpan? HoraFin { get; set; }
}