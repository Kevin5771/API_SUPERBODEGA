using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperBodegaAPI.Models
{
    public class DetalleCompra
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompraId { get; set; }
        [ForeignKey(nameof(CompraId))]
        public Compra Compra { get; set; } = null!;

        [Required]
        public int ProductoId { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Product Producto { get; set; } = null!;

        [Required]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }
    }
}
