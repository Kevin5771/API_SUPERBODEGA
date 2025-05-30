using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SuperBodegaAPI.Models
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string Telefono { get; set; } = string.Empty;

        // Navegación a ventas
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        // Navegación a items de carrito
        public ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();
    }
}