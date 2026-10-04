namespace SistemaDeReservas.Models
{
    public class DisponibilidadViewModel
    {
        public Laboratorio Laboratorio { get; set; } = null!;
        public TimeSpan HoraApertura { get; set; }
        public TimeSpan HoraCierre { get; set; }
        public DateTime InicioSemana { get; set; }
        public bool PuedeRetroceder { get; set; }
        public List<DateTime> Dias { get; set; } = new();
        public List<TimeSpan> Horas { get; set; } = new();

        // Pares (fecha, hora) ya reservados. Se llenará cuando existan reservas.
        public HashSet<(DateTime Fecha, TimeSpan Hora)> Ocupados { get; set; } = new();

        public bool EstaLibre(DateTime fecha, TimeSpan hora) =>
            !Ocupados.Contains((fecha.Date, hora));
    }
}