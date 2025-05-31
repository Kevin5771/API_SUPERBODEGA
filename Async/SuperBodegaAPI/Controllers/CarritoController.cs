// ✅ CarritoController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Events;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarritoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CarritoController> _logger;

        public CarritoController(
            AppDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<CarritoController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
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
                    NombreProducto = ci.Producto!.Nombre,
                    Precio = ci.Producto.Precio,
                    Cantidad = ci.Cantidad,
                    Categoria = ci.Producto.Categoria,
                    ImagenUrl = string.IsNullOrWhiteSpace(ci.Producto.ImagenUrl)
                        ? "/images/placeholder.png"
                        : $"/images/productos/{ci.Producto.ImagenUrl}"
                })
                .ToListAsync();

            return Ok(items);
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

        // Compra: ahora agrupa todos los ítems en una sola Venta + DetalleVenta
        [HttpPost("Comprar/{clienteId}")]
        public async Task<IActionResult> Comprar(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null) return BadRequest("Cliente no encontrado.");

            var items = await _context.CarritoItems
                .Include(ci => ci.Producto)
                .Where(ci => ci.ClienteId == clienteId)
                .ToListAsync();

            if (!items.Any()) return BadRequest("El carrito está vacío.");

            // 1) Crear la venta (cabecera)
            var nuevaVenta = new Venta
            {
                Fecha = DateTime.Now,
                ClienteId = clienteId,
                Estado = "Recibido",
                CodigoSeguimiento = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Total = items.Sum(ci => ci.Producto!.Precio * ci.Cantidad)
            };
            _context.Ventas.Add(nuevaVenta);
            await _context.SaveChangesAsync(); // para obtener nuevaVenta.Id

            // 2) Registrar detalles y actualizar stock
            foreach (var ci in items)
            {
                var producto = ci.Producto!;
                if (producto.Stock < ci.Cantidad)
                    return BadRequest($"Stock insuficiente para el producto ID: {producto.Id}");

                producto.Stock -= ci.Cantidad;

                _context.DetalleVentas.Add(new DetalleVenta
                {
                    VentaId = nuevaVenta.Id,
                    ProductoId = producto.Id,
                    Cantidad = ci.Cantidad,
                    PrecioUnitario = producto.Precio
                });
            }

            // 3) Vaciar el carrito
            _context.CarritoItems.RemoveRange(items);

            await _context.SaveChangesAsync();

            // 4) Publicar evento de pedido único
            if (!string.IsNullOrEmpty(cliente.Email))
            {
                var detallesParaMail = await _context.DetalleVentas
                    .Where(dv => dv.VentaId == nuevaVenta.Id)
                    .Include(dv => dv.Producto)
                    .Select(dv => new VentaItemMailDto(
                        dv.Producto!.Nombre,
                        dv.Cantidad,
                        dv.PrecioUnitario
                    ))
                    .ToListAsync();

                await _publishEndpoint.Publish(new EstadoPedidoActualizado(
                    nuevaVenta.Id,
                    nuevaVenta.Estado,
                    cliente.Email,
                    nuevaVenta.CodigoSeguimiento,
                    detallesParaMail
                ));
                _logger.LogInformation(
                    "📧 Correo de pedido {PedidoId} publicado a {Email}",
                    nuevaVenta.Id, cliente.Email
                );
            }

            return Ok(new
            {
                Mensaje = "Compra realizada con éxito.",
                PedidoId = nuevaVenta.Id,
                Codigo = nuevaVenta.CodigoSeguimiento
            });
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
