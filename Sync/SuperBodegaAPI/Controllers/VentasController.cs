using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;
using ClosedXML.Excel;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace SuperBodegaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VentasController> _logger;

        public VentasController(AppDbContext context, ILogger<VentasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VentaAdminDto>>> GetVentas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var query = _context.Ventas
                .Include(v => v.Producto)
                .Include(v => v.Cliente)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(v => v.Fecha.Date >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                query = query.Where(v => v.Fecha.Date <= fechaFin.Value.Date);

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .Select(v => new VentaAdminDto
                {
                    Id             = v.Id,
                    Cliente        = v.Cliente != null ? v.Cliente.Nombre : string.Empty,
                    Producto       = v.Producto != null ? v.Producto.Nombre : string.Empty,
                    Cantidad       = v.Cantidad,
                    PrecioUnitario = v.PrecioUnitario,
                    Estado         = v.Estado,
                    Fecha          = v.Fecha
                })
                .ToListAsync();

            return Ok(ventas);
        }

        [HttpPut("CambiarEstado/{id}")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
        {
            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null)
                return NotFound();

            venta.Estado = nuevoEstado;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("Excel")]
        public IActionResult ReportePorPeriodoExcel(
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

            var datos = query
                .Select(v => new
                {
                    v.Id,
                    v.Fecha,
                    Cliente = v.Cliente != null ? v.Cliente.Nombre : string.Empty,
                    Estado  = v.Estado,
                    Total   = v.Cantidad * v.PrecioUnitario
                })
                .OrderByDescending(x => x.Fecha)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Ventas");

            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "Fecha";
            ws.Cell(1, 3).Value = "Cliente";
            ws.Cell(1, 4).Value = "Estado";
            ws.Cell(1, 5).Value = "Total";

            for (int i = 0; i < datos.Count; i++)
            {
                int fila = i + 2;
                ws.Cell(fila, 1).Value = datos[i].Id;
                ws.Cell(fila, 2).Value = datos[i].Fecha;
                ws.Cell(fila, 3).Value = datos[i].Cliente;
                ws.Cell(fila, 4).Value = datos[i].Estado;
                ws.Cell(fila, 5).Value = datos[i].Total;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var fileName = $"ReporteVentas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx";
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // ✅ Corrección: usar "Count" como en los otros controladores
        [HttpGet("Count")]
        public async Task<ActionResult<int>> CountVentas()
            => await _context.Ventas.CountAsync();

        public class VentaDto
        {
            public int Id { get; set; }
            public string Producto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public string Estado { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
        }

        public class VentaAdminDto : VentaDto
        {
            public string Cliente { get; set; } = string.Empty;
        }
    }
}
