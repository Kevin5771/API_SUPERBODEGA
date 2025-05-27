// ✅ CarritoController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarritoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CarritoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("DelCliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<CarritoItemDto>>> GetDelCliente(int clienteId)
        {
            var items = await _context.CarritoItems
                .Include(ci => ci.Producto)
                .Where(ci => ci.ClienteId == clienteId)
                .Select(ci => new CarritoItemDto
                {
                    Id = ci.Id,
                    ProductoId = ci.ProductoId,
                    NombreProducto = ci.Producto.Nombre,
                    Precio = ci.Producto.Precio,
                    Cantidad = ci.Cantidad,
                    Categoria = ci.Producto.Categoria,
                    ImagenUrl = string.IsNullOrWhiteSpace(ci.Producto.ImagenUrl)
                        ? "/images/placeholder.png"
                        : $"/images/productos/{ci.Producto.ImagenUrl}"
                })
                .ToListAsync();

            return items;
        }

        [HttpPost]
        public async Task<ActionResult<CarritoItemDto>> AddToCarrito(CarritoItem item)
        {
            var producto = await _context.Products.FindAsync(item.ProductoId);
            if (producto == null) return BadRequest("Producto no encontrado.");
            if (producto.Stock < item.Cantidad) return BadRequest("Stock insuficiente.");

            var existente = await _context.CarritoItems
                .FirstOrDefaultAsync(ci => ci.ClienteId == item.ClienteId && ci.ProductoId == item.ProductoId);

            if (existente != null)
                existente.Cantidad = item.Cantidad;
            else
                _context.CarritoItems.Add(item);

            await _context.SaveChangesAsync();

            var dto = new CarritoItemDto
            {
                Id = existente?.Id ?? item.Id,
                ProductoId = item.ProductoId,
                NombreProducto = producto.Nombre,
                Precio = producto.Precio,
                Cantidad = item.Cantidad,
                Categoria = producto.Categoria,
                ImagenUrl = string.IsNullOrWhiteSpace(producto.ImagenUrl)
                    ? "/images/placeholder.png"
                    : $"/images/productos/{producto.ImagenUrl}"
            };

            return CreatedAtAction(nameof(GetDelCliente), new { clienteId = item.ClienteId }, dto);
        }

        [HttpPost("ActualizarCantidad")]
        public async Task<IActionResult> ActualizarCantidad([FromBody] CarritoItem item)
        {
            var existente = await _context.CarritoItems
                .FirstOrDefaultAsync(ci => ci.ClienteId == item.ClienteId && ci.ProductoId == item.ProductoId);

            if (existente == null) return NotFound();

            if (item.Cantidad <= 0)
                _context.CarritoItems.Remove(existente);
            else
                existente.Cantidad = item.Cantidad;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{clienteId}/{productoId}")]
        public async Task<IActionResult> RemoveItem(int clienteId, int productoId)
        {
            var item = await _context.CarritoItems
                .FirstOrDefaultAsync(ci => ci.ClienteId == clienteId && ci.ProductoId == productoId);

            if (item == null) return NotFound();

            _context.CarritoItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("Comprar/{clienteId}")]
        public async Task<IActionResult> Comprar(int clienteId)
        {
            var items = await _context.CarritoItems
                .Include(c => c.Producto)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();

            if (!items.Any()) return BadRequest("El carrito está vacío.");

            foreach (var item in items)
            {
                if (item.Producto == null)
                    item.Producto = await _context.Products.FindAsync(item.ProductoId);

                if (item.Producto == null || item.Producto.Stock < item.Cantidad)
                    return BadRequest($"Stock insuficiente para el producto ID: {item.ProductoId}");

                item.Producto.Stock -= item.Cantidad;

                _context.Ventas.Add(new Venta
                {
                    Fecha = DateTime.Now,
                    ClienteId = item.ClienteId,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto.Precio,
                    Estado = "Recibido"
                });
            }

            _context.CarritoItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok("Compra realizada con éxito.");
        }

        public class CarritoItemDto
        {
            public int Id { get; set; }
            public int ProductoId { get; set; }
            public string NombreProducto { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public string ImagenUrl { get; set; } = "/images/placeholder.png";
            public decimal Subtotal => Precio * Cantidad;
        }
    }
}
