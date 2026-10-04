namespace SistemaDeReservas.Models
{
    public class Laboratorio
    {
        public int LaboratorioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public bool Disponible { get; set; }
        public TimeSpan HoraApertura { get; set; }
        public TimeSpan HoraCierre { get; set; }
    }
}