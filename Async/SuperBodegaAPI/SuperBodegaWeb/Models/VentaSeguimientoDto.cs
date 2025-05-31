namespace SuperBodegaWeb.Models
{
    public class VentaSeguimientoDto
    {
        public string CodigoSeguimiento { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
