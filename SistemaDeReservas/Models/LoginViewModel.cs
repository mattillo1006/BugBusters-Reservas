using System.ComponentModel.DataAnnotations;

namespace SistemaDeReservas.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}