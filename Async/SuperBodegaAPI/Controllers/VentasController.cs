using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Events;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VentasController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public VentasController(
            AppDbContext context,
            ILogger<VentasController> logger,
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        // GET: api/Ventas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VentaAdminDto>>> GetVentas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(v => v.Fecha.Date >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                query = query.Where(v => v.Fecha.Date <= fechaFin.Value.Date);

            var list = await query
                .OrderByDescending(v => v.Fecha)
                .Select(v => new VentaAdminDto
                {
                    Id                = v.Id,
                    Cliente           = v.Cliente!.Nombre,
                    Fecha             = v.Fecha,
                    Estado            = v.Estado,
                    Total             = v.Total,
                    CodigoSeguimiento = v.CodigoSeguimiento,
                    ItemsCount        = v.DetalleVentas.Count
                })
                .ToListAsync();

            return Ok(list);
        }

        // POST: api/Ventas
        [HttpPost]
        public async Task<ActionResult<object>> CrearVenta([FromBody] VentaInputDto input)
        {
            var cliente = await _context.Clientes.FindAsync(input.ClienteId);
            if (cliente == null)
                return BadRequest("Cliente no válido.");

            if (input.Items == null || !input.Items.Any())
                return BadRequest("Debe enviar al menos un item en la venta.");

            var nuevaVenta = new Venta
            {
                ClienteId         = input.ClienteId,
                Fecha             = DateTime.Now,
                Estado            = "Recibido",
                CodigoSeguimiento = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Total             = input.Items.Sum(i => i.Cantidad * i.PrecioUnitario)
            };
            _context.Ventas.Add(nuevaVenta);
            await _context.SaveChangesAsync();

            foreach (var item in input.Items)
            {
                var producto = await _context.Products.FindAsync(item.ProductoId);
                if (producto == null)
                    return BadRequest($"Producto con ID {item.ProductoId} no existe.");

                _context.DetalleVentas.Add(new DetalleVenta
                {
                    VentaId        = nuevaVenta.Id,
                    ProductoId     = item.ProductoId,
                    Cantidad       = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                });
            }

            _context.CambiosEstadoVentas.Add(new CambioEstadoVenta
            {
                VentaId        = nuevaVenta.Id,
                EstadoAnterior = "-",
                EstadoNuevo    = "Recibido",
                FechaCambio    = DateTime.Now
            });

            await _context.SaveChangesAsync();

            var detallesParaMail = await _context.DetalleVentas
                .Where(d => d.VentaId == nuevaVenta.Id)
                .Include(d => d.Producto)
                .Select(d => new VentaItemMailDto(
                    d.Producto!.Nombre,
                    d.Cantidad,
                    d.PrecioUnitario
                ))
                .ToListAsync();

            if (!string.IsNullOrEmpty(cliente.Email))
            {
                await _publishEndpoint.Publish(new EstadoPedidoActualizado(
                    nuevaVenta.Id,
                    nuevaVenta.Estado,
                    cliente.Email,
                    nuevaVenta.CodigoSeguimiento,
                    detallesParaMail
                ));
                _logger.LogInformation("📧 Correo de pedido {PedidoId} publicado a {Email}",
                                       nuevaVenta.Id, cliente.Email);
            }

            return Ok(new
            {
                Mensaje           = "Pedido creado con éxito.",
                PedidoId          = nuevaVenta.Id,
                CodigoSeguimiento = nuevaVenta.CodigoSeguimiento
            });
        }

        // PUT: api/Ventas/CambiarEstado/{id}
        [HttpPut("CambiarEstado/{id}")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null) return NotFound();
            if (venta.Estado == nuevoEstado) return NoContent();

            var anterior = venta.Estado;
            venta.Estado = nuevoEstado;

            _context.CambiosEstadoVentas.Add(new CambioEstadoVenta
            {
                VentaId        = venta.Id,
                EstadoAnterior = anterior,
                EstadoNuevo    = nuevoEstado,
                FechaCambio    = DateTime.Now
            });

            await _context.SaveChangesAsync();

            var detallesParaMail = await _context.DetalleVentas
                .Where(d => d.VentaId == venta.Id)
                .Include(d => d.Producto)
                .Select(d => new VentaItemMailDto(
                    d.Producto!.Nombre,
                    d.Cantidad,
                    d.PrecioUnitario
                ))
                .ToListAsync();

            if (!string.IsNullOrEmpty(venta.Cliente!.Email))
            {
                await _publishEndpoint.Publish(new EstadoPedidoActualizado(
                    venta.Id,
                    venta.Estado,
                    venta.Cliente.Email,
                    venta.CodigoSeguimiento,
                    detallesParaMail
                ));
                _logger.LogInformation("📧 Correo de cambio de estado para pedido {PedidoId} a {Email}",
                                       venta.Id, venta.Cliente.Email);
            }

            return NoContent();
        }

        // GET: api/Ventas/Seguimiento/{codigoSeguimiento}
        [HttpGet("Seguimiento/{codigoSeguimiento}")]
        public async Task<ActionResult<SeguimientoVentaDto>> ObtenerSeguimiento(string codigoSeguimiento)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.CodigoSeguimiento == codigoSeguimiento);
            if (venta == null) return NotFound("No se encontró un pedido con ese código.");

            var detalles = await _context.DetalleVentas
                .Where(d => d.VentaId == venta.Id)
                .Include(d => d.Producto)
                .Select(d => new VentaItemDto
                {
                    NombreProducto  = d.Producto!.Nombre,
                    Cantidad        = d.Cantidad,
                    PrecioUnitario  = d.PrecioUnitario
                })
                .ToListAsync();

            var cambios = await _context.CambiosEstadoVentas
                .Where(c => c.VentaId == venta.Id)
                .OrderBy(c => c.FechaCambio)
                .Select(c => new CambioEstadoDto
                {
                    EstadoAnterior = c.EstadoAnterior,
                    EstadoNuevo    = c.EstadoNuevo,
                    Fecha          = c.FechaCambio
                })
                .ToListAsync();

            return Ok(new SeguimientoVentaDto
            {
                Cliente           = venta.Cliente!.Nombre,
                FechaVenta        = venta.Fecha,
                EstadoActual      = venta.Estado,
                CodigoSeguimiento = venta.CodigoSeguimiento,
                Items             = detalles,
                Cambios           = cambios
            });
        }

        // GET: api/Ventas/Excel
        [HttpGet("Excel")]
        public async Task<IActionResult> ReportePorPeriodoExcel(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.DetalleVentas)
                    .ThenInclude(dv => dv.Producto)
                .Where(v => !fechaInicio.HasValue || v.Fecha.Date >= fechaInicio.Value.Date)
                .Where(v => !fechaFin.HasValue    || v.Fecha.Date <= fechaFin.Value.Date)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            // Hoja de Resumen
            var wsResumen = workbook.Worksheets.Add("Resumen");
            var headersResumen = new[] { "VentaId", "Fecha", "Cliente", "ItemsCount", "Total", "Código" };
            for (int i = 0; i < headersResumen.Length; i++)
                wsResumen.Cell(1, i + 1).Value = headersResumen[i];
            wsResumen.Row(1).Style.Font.Bold = true;
            wsResumen.SheetView.FreezeRows(1);

            for (int i = 0; i < ventas.Count; i++)
            {
                var v = ventas[i];
                int row = i + 2;
                wsResumen.Cell(row, 1).Value = v.Id;
                wsResumen.Cell(row, 2).Value = v.Fecha;
                wsResumen.Cell(row, 3).Value = v.Cliente?.Nombre;
                wsResumen.Cell(row, 4).Value = v.DetalleVentas.Count;
                wsResumen.Cell(row, 5).Value = v.Total;
                wsResumen.Cell(row, 6).Value = v.CodigoSeguimiento;
            }
            wsResumen.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            wsResumen.Column(5).Style.NumberFormat.Format = "\"Q\" #,##0.00";
            wsResumen.Columns().AdjustToContents();
            wsResumen.RangeUsed().SetAutoFilter();

            // Hoja de Detalle
            var wsDetalle = workbook.Worksheets.Add("Detalle");
            var headersDetalle = new[]
            {
                "VentaId", "Fecha", "Cliente", "Producto",
                "Cantidad", "PrecioUnitario", "Subtotal"
            };
            for (int i = 0; i < headersDetalle.Length; i++)
                wsDetalle.Cell(1, i + 1).Value = headersDetalle[i];
            wsDetalle.Row(1).Style.Font.Bold = true;
            wsDetalle.SheetView.FreezeRows(1);

            int rowDetalle = 2;
            foreach (var v in ventas)
            {
                foreach (var dv in v.DetalleVentas)
                {
                    wsDetalle.Cell(rowDetalle, 1).Value = v.Id;
                    wsDetalle.Cell(rowDetalle, 2).Value = v.Fecha;
                    wsDetalle.Cell(rowDetalle, 3).Value = v.Cliente?.Nombre;
                    wsDetalle.Cell(rowDetalle, 4).Value = dv.Producto?.Nombre;
                    wsDetalle.Cell(rowDetalle, 5).Value = dv.Cantidad;
                    wsDetalle.Cell(rowDetalle, 6).Value = dv.PrecioUnitario;
                    wsDetalle.Cell(rowDetalle, 7).FormulaA1 = $"E{rowDetalle}*F{rowDetalle}";
                    rowDetalle++;
                }
            }
            wsDetalle.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            wsDetalle.Column(6).Style.NumberFormat.Format = "\"Q\" #,##0.00";
            wsDetalle.Column(7).Style.NumberFormat.Format = "\"Q\" #,##0.00";
            wsDetalle.Columns().AdjustToContents();
            wsDetalle.RangeUsed().SetAutoFilter();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var fileName = $"Ventas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx";
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // GET: api/Ventas/Count
        [HttpGet("Count")]
        public async Task<ActionResult<int>> CountVentas()
            => await _context.Ventas.CountAsync();


        // ----- DTOs -----

        public class VentaItemInputDto
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class VentaInputDto
        {
            public int ClienteId { get; set; }
            public List<VentaItemInputDto> Items { get; set; } = new();
        }

        public class VentaItemDto
        {
            public string NombreProducto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class VentaAdminDto
        {
            public int Id { get; set; }
            public string Cliente { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
            public string Estado { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public string CodigoSeguimiento { get; set; } = string.Empty;
            public int ItemsCount { get; set; }
        }

        public class SeguimientoVentaDto
        {
            public string Cliente { get; set; } = string.Empty;
            public DateTime FechaVenta { get; set; }
            public string EstadoActual { get; set; } = string.Empty;
            public string CodigoSeguimiento { get; set; } = string.Empty;
            public List<VentaItemDto> Items { get; set; } = new();
            public List<CambioEstadoDto> Cambios { get; set; } = new();
        }

        public class CambioEstadoDto
        {
            public string EstadoAnterior { get; set; } = string.Empty;
            public string EstadoNuevo { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
        }
    }
}
