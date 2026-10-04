namespace SistemaDeReservas.Models
{
    public class DisponibilidadViewModel
    {
        public Laboratorio Laboratorio { get; set; } = new();
        public TimeSpan HoraApertura { get; set; }
        public TimeSpan HoraCierre { get; set; }
        public DateTime InicioSemana { get; set; }
        public bool PuedeRetroceder { get; set; }

        public List<DateTime> Dias { get; set; } = new();
        public List<TimeSpan> Horas { get; set; } = new();
        public HashSet<(DateTime Dia, TimeSpan Hora)> Ocupados { get; set; } = new();

        // Libre = laboratorio en servicio + sin reserva + horario que no ha pasado
        public bool EstaLibre(DateTime dia, TimeSpan hora) =>
            Laboratorio.Disponible
            && !Ocupados.Contains((dia.Date, hora))
            && dia.Date + hora > DateTime.Now;
    }
}