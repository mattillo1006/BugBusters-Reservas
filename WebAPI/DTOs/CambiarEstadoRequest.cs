namespace WebAPI.DTOs;

public class CambiarEstadoRequest
{
    public string? Estado { get; set; } // "Habilitado" | "FueraDeServicio"
}