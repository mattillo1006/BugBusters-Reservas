namespace WebAPI.DTOs;
public class LaboratorioResponse
{
    public int LaboratorioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string Estado { get; set; } = string.Empty;
}