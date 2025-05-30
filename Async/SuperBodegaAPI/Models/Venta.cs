using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperBodegaAPI.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Required]
        public string Estado { get; set; } = "Recibido";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        public string CodigoSeguimiento { get; set; }
            = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        // Detalles de línea de la venta
        public ICollection<DetalleVenta> DetalleVentas { get; set; }
            = new List<DetalleVenta>();

        // **NUEVO** Historial de cambios de estado
        public ICollection<CambioEstadoVenta> CambiosEstadoVentas { get; set; }
            = new List<CambioEstadoVenta>();
    }
}
