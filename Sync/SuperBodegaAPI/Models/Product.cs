using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperBodegaAPI.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Required]
        public int Stock { get; set; }

        [Required]
        public int ProveedorId { get; set; }
        public Proveedor Proveedor { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string Categoria { get; set; } = string.Empty;

        [MaxLength(255)]
        public string ImagenUrl { get; set; } = string.Empty;

        // Navegación a compras
        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
        // Navegación a ventas
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        // Navegación a detalles de compra
        public ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();
        // Navegación a detalles de venta
        public ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
        // Navegación a items de carrito
        public ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();
    }
}