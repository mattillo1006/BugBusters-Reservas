namespace SistemaDeReservas.Models
{
    public enum EstadoReserva
    {
        Proxima,
        EnCurso,
        Finalizada,
        Cancelada
    }

    // Una reserva tal como la devuelve api/Reservas/mias
    public class MiReserva
    {
        public int ReservaId { get; set; }
        public int LaboratorioId { get; set; }
        public string Laboratorio { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }

        public DateTime Inicio => Fecha.Date + HoraInicio;
        public DateTime Fin => Fecha.Date + HoraFin;
        public double Horas => (HoraFin - HoraInicio).TotalHours;
        public bool EstaActiva => string.Equals(Estado, "Activa", StringComparison.OrdinalIgnoreCase);

        // La BD solo guarda Activa/Cancelada; "próxima", "en curso" y "finalizada" se deducen de la hora
        public EstadoReserva ObtenerEstado(DateTime ahora)
        {
            if (!EstaActiva) return EstadoReserva.Cancelada;
            if (Fin <= ahora) return EstadoReserva.Finalizada;
            if (Inicio <= ahora) return EstadoReserva.EnCurso;
            return EstadoReserva.Proxima;
        }
    }

    public class MisReservasViewModel
    {
        public const string FiltroProximas = "proximas";
        public const string FiltroHistorial = "historial";

        public string Filtro { get; set; } = FiltroProximas;
        public DateTime Ahora { get; set; }

        // Activas que aún no terminan (incluye la que está en curso), la más cercana primero
        public List<MiReserva> Proximas { get; set; } = new();

        // Finalizadas y canceladas, la más reciente primero
        public List<MiReserva> Historial { get; set; } = new();

        public IReadOnlyList<MiReserva> Mostradas =>
            Filtro == FiltroHistorial ? Historial : Proximas;
    }
}
