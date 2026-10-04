namespace SistemaDeReservas.Models
{
    public class Laboratorio
    {
        public int LaboratorioId { get; set; }
        public string Nombre { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public int Capacidad { get; set; }
        public string Estado { get; set; } = "";

        public bool Disponible => Estado != "FueraDeServicio";
    }
}