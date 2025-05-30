using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComprasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Compras
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompraAdminDto>>> GetCompras(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var query = _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.DetalleCompras)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(c => c.Fecha.Date >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                query = query.Where(c => c.Fecha.Date <= fechaFin.Value.Date);

            var list = await query
                .OrderByDescending(c => c.Fecha)
                .Select(c => new CompraAdminDto
                {
                    Id           = c.Id,
                    Fecha        = c.Fecha,
                    Proveedor    = c.Proveedor!.Nombre,
                    Total        = c.Total,
                    ItemsCount   = c.DetalleCompras.Count
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/Compras/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CompraDetailDto>> GetCompra(int id)
        {
            var c = await _context.Compras
                .Include(cmp => cmp.Proveedor)
                .Include(cmp => cmp.DetalleCompras)
                    .ThenInclude(dc => dc.Producto)
                .FirstOrDefaultAsync(cmp => cmp.Id == id);

            if (c == null) return NotFound();

            return Ok(new CompraDetailDto
            {
                Id         = c.Id,
                Fecha      = c.Fecha,
                Proveedor  = c.Proveedor!.Nombre,
                Total      = c.Total,
                Items      = c.DetalleCompras.Select(dc => new CompraItemDto
                {
                    NombreProducto  = dc.Producto!.Nombre,
                    Cantidad        = dc.Cantidad,
                    PrecioUnitario  = dc.PrecioUnitario
                }).ToList()
            });
        }

        // POST: api/Compras
        [HttpPost]
        public async Task<ActionResult<object>> PostCompra([FromBody] CompraInputDto input)
        {
            if (input.Items == null || !input.Items.Any())
                return BadRequest("Debe especificar al menos un ítem.");

            var proveedor = await _context.Proveedores.FindAsync(input.ProveedorId);
            if (proveedor == null)
                return BadRequest("Proveedor no válido.");

            // 1) Cabecera
            var compra = new Compra
            {
                ProveedorId = input.ProveedorId,
                Fecha       = DateTime.Now,
                Total       = input.Items.Sum(i => i.Cantidad * i.PrecioUnitario)
            };
            _context.Compras.Add(compra);
            await _context.SaveChangesAsync(); // para obtener compra.Id

            // 2) Detalles y ajuste de stock
            foreach (var item in input.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductoId);
                if (prod == null)
                    return BadRequest($"Producto {item.ProductoId} no encontrado.");

                prod.Stock += item.Cantidad;

                _context.DetalleCompras.Add(new DetalleCompra
                {
                    CompraId      = compra.Id,
                    ProductoId    = item.ProductoId,
                    Cantidad      = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                });
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompra),
                new { id = compra.Id },
                new { Mensaje = "Compra creada exitosamente", CompraId = compra.Id });
        }

        // DELETE: api/Compras/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompra(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.DetalleCompras)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (compra == null) return NotFound();

            // Revertir stock
            foreach (var dc in compra.DetalleCompras)
            {
                var prod = await _context.Products.FindAsync(dc.ProductoId);
                if (prod != null)
                    prod.Stock -= dc.Cantidad;
            }

            _context.DetalleCompras.RemoveRange(compra.DetalleCompras);
            _context.Compras.Remove(compra);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/Compras/ReportePorProducto
        [HttpGet("ReportePorProducto")]
        public async Task<ActionResult<IEnumerable<object>>> GetReportePorProducto(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var query = _context.DetalleCompras
                .Include(dc => dc.Producto)
                .Include(dc => dc.Compra)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(dc => dc.Compra.Fecha.Date >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                query = query.Where(dc => dc.Compra.Fecha.Date <= fechaFin.Value.Date);

            var reporte = await query
                .GroupBy(dc => new { dc.ProductoId, dc.Producto!.Nombre })
                .Select(g => new
                {
                    ProductoId     = g.Key.ProductoId,
                    Producto       = g.Key.Nombre,
                    TotalComprado = g.Sum(x => x.Cantidad),
                    TotalGastado  = g.Sum(x => x.Cantidad * x.PrecioUnitario)
                })
                .ToListAsync();

            return Ok(reporte);
        }

        // GET: api/Compras/ReportePorProveedor
        [HttpGet("ReportePorProveedor")]
        public async Task<ActionResult<IEnumerable<object>>> GetReportePorProveedor()
        {
            var query = _context.DetalleCompras
                .Include(dc => dc.Compra)
                    .ThenInclude(c => c.Proveedor)
                .AsQueryable();

            var reporte = await query
                .GroupBy(dc => new { dc.Compra!.ProveedorId, dc.Compra.Proveedor!.Nombre })
                .Select(g => new
                {
                    ProveedorId    = g.Key.ProveedorId,
                    Proveedor      = g.Key.Nombre,
                    TotalComprado  = g.Sum(x => x.Cantidad),
                    TotalGastado   = g.Sum(x => x.Cantidad * x.PrecioUnitario)
                })
                .ToListAsync();

            return Ok(reporte);
        }

        // GET: api/Compras/ReportePorFecha
        [HttpGet("ReportePorFecha")]
        public async Task<ActionResult<IEnumerable<object>>> GetReportePorFecha()
        {
            var reporte = await _context.DetalleCompras
                .Include(dc => dc.Compra)
                .GroupBy(dc => dc.Compra!.Fecha.Date)
                .Select(g => new
                {
                    Fecha          = g.Key,
                    TotalComprado = g.Sum(x => x.Cantidad),
                    TotalGastado  = g.Sum(x => x.Cantidad * x.PrecioUnitario)
                })
                .OrderBy(r => r.Fecha)
                .ToListAsync();

            return Ok(reporte);
        }

        // GET: api/Compras/ReportePorProducto/Excel
        [HttpGet("ReportePorProducto/Excel")]
        public async Task<IActionResult> GetReportePorProductoExcel(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var resp = await GetReportePorProducto(fechaInicio, fechaFin);
            var data = (resp.Result as OkObjectResult)?.Value as IEnumerable<dynamic>;

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("PorProducto");
            ws.Cell(1, 1).Value = "ProductoId";
            ws.Cell(1, 2).Value = "Producto";
            ws.Cell(1, 3).Value = "TotalComprado";
            ws.Cell(1, 4).Value = "TotalGastado";

            int row = 2;
            foreach (var r in data!)
            {
                ws.Cell(row, 1).Value = r.ProductoId;
                ws.Cell(row, 2).Value = r.Producto;
                ws.Cell(row, 3).Value = r.TotalComprado;
                ws.Cell(row, 4).Value = r.TotalGastado;
                row++;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ReportePorProducto.xlsx"
            );
        }

        // ----- DTOs -----
        public class CompraItemInputDto
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class CompraInputDto
        {
            public int ProveedorId { get; set; }
            public List<CompraItemInputDto> Items { get; set; } = new();
        }

        public class CompraAdminDto
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public string Proveedor { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public int ItemsCount { get; set; }
        }

        public class CompraItemDto
        {
            public string NombreProducto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class CompraDetailDto
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public string Proveedor { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public List<CompraItemDto> Items { get; set; } = new();
        }
    }
}
