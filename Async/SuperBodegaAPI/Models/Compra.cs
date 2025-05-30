using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperBodegaAPI.Models
{
    public class Compra
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public int ProveedorId { get; set; }
        [ForeignKey(nameof(ProveedorId))]
        public Proveedor? Proveedor { get; set; }

        // Total acumulado de todos los ítems de la compra
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        // Detalles de línea de la compra
        public ICollection<DetalleCompra> DetalleCompras { get; set; }
            = new List<DetalleCompra>();
    }
}
