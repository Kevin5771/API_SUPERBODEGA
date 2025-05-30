using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperBodegaAPI.Models
{
    public class CambioEstadoVenta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }

        [ForeignKey("VentaId")]
        public Venta Venta { get; set; } = null!;

        [Required]
        public string EstadoAnterior { get; set; } = string.Empty;

        [Required]
        public string EstadoNuevo { get; set; } = string.Empty;

        public DateTime FechaCambio { get; set; } = DateTime.Now;
    }
}
