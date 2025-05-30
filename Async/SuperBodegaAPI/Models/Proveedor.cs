using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SuperBodegaAPI.Models
{
    public class Proveedor
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

        // Navegación a Compras realizadas por este proveedor
        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    }
}