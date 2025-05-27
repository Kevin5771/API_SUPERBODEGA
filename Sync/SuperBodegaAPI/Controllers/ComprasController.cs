using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;
using ClosedXML.Excel;
using System.IO;

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
        public async Task<ActionResult<IEnumerable<Compra>>> GetCompras()
        {
            return await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Producto)
                .ToListAsync();
        }

        // GET: api/Compras/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Compra>> GetCompra(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Producto)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (compra == null)
                return NotFound();

            return compra;
        }

        // POST: api/Compras
        [HttpPost]
        public async Task<ActionResult<Compra>> PostCompra(Compra compra)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Buscar el producto
            var producto = await _context.Products.FindAsync(compra.ProductoId);
            if (producto == null)
                return BadRequest("Producto no encontrado.");

            // Sumar stock
            producto.Stock += compra.Cantidad;

            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCompra), new { id = compra.Id }, compra);
        }

        // PUT: api/Compras/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCompra(int id, Compra compra)
        {
            if (id != compra.Id)
                return BadRequest();

            _context.Entry(compra).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Compras.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/Compras/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompra(int id)
        {
            var compra = await _context.Compras.FindAsync(id);
            if (compra == null)
                return NotFound();

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
            var query = _context.Compras.Include(c => c.Producto).AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(c => c.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(c => c.Fecha <= fechaFin.Value);

            var reporte = await query
                .GroupBy(c => new { c.ProductoId, c.Producto.Nombre })
                .Select(g => new
                {
                    ProductoId = g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    TotalComprado = g.Sum(c => c.Cantidad),
                    TotalGastado = g.Sum(c => c.Cantidad * c.PrecioUnitario)
                })
                .ToListAsync();

            return Ok(reporte);
        }

        // GET: api/Compras/ReportePorProducto/Excel
        [HttpGet("ReportePorProducto/Excel")]
        public async Task<IActionResult> GetReportePorProductoExcel(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var query = _context.Compras.Include(c => c.Producto).AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(c => c.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(c => c.Fecha <= fechaFin.Value);

            var reporte = await query
                .GroupBy(c => new { c.ProductoId, c.Producto.Nombre })
                .Select(g => new
                {
                    ProductoId = g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    TotalComprado = g.Sum(c => c.Cantidad),
                    TotalGastado = g.Sum(c => c.Cantidad * c.PrecioUnitario)
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ReportePorProducto");
            worksheet.Cell(1, 1).Value = "ProductoId";
            worksheet.Cell(1, 2).Value = "Producto";
            worksheet.Cell(1, 3).Value = "TotalComprado";
            worksheet.Cell(1, 4).Value = "TotalGastado";

            for (int i = 0; i < reporte.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = reporte[i].ProductoId;
                worksheet.Cell(i + 2, 2).Value = reporte[i].Producto;
                worksheet.Cell(i + 2, 3).Value = reporte[i].TotalComprado;
                worksheet.Cell(i + 2, 4).Value = reporte[i].TotalGastado;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ReportePorProducto.xlsx");
        }

        // GET: api/Compras/ReportePorProveedor
        [HttpGet("ReportePorProveedor")]
        public async Task<ActionResult<IEnumerable<object>>> GetReportePorProveedor()
        {
            var reporte = await _context.Compras
                .Include(c => c.Proveedor)
                .GroupBy(c => new { c.ProveedorId, c.Proveedor.Nombre })
                .Select(g => new
                {
                    ProveedorId = g.Key.ProveedorId,
                    Proveedor = g.Key.Nombre,
                    TotalComprado = g.Sum(c => c.Cantidad),
                    TotalGastado = g.Sum(c => c.Cantidad * c.PrecioUnitario)
                })
                .ToListAsync();

            return Ok(reporte);
        }

        // GET: api/Compras/ReportePorFecha
        [HttpGet("ReportePorFecha")]
        public async Task<ActionResult<IEnumerable<object>>> GetReportePorFecha()
        {
            var reporte = await _context.Compras
                .GroupBy(c => c.Fecha.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    TotalComprado = g.Sum(c => c.Cantidad),
                    TotalGastado = g.Sum(c => c.Cantidad * c.PrecioUnitario)
                })
                .OrderBy(r => r.Fecha)
                .ToListAsync();

            return Ok(reporte);
        }
    }
}