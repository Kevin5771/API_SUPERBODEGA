using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProveedoresController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Proveedor>>> Get()
            => await _context.Proveedores.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Proveedor>> Get(int id)
        {
            var pr = await _context.Proveedores.FindAsync(id);
            if (pr == null) return NotFound();
            return pr;
        }

        [HttpPost]
        public async Task<ActionResult<Proveedor>> Post(Proveedor pr)
        {
            _context.Proveedores.Add(pr);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = pr.Id }, pr);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Proveedor pr)
        {
            if (id != pr.Id) return BadRequest("Id de URL distinto al del body.");
            _context.Entry(pr).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Proveedores.AnyAsync(x => x.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pr = await _context.Proveedores.FindAsync(id);
            if (pr == null) return NotFound();

            _context.Proveedores.Remove(pr);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ Exportar a Excel sin la propiedad 'Contacto'
        [HttpGet("Export")]
        public async Task<IActionResult> Export()
        {
            var items = await _context.Proveedores.ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Proveedores");

            // Encabezados
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Nombre";
            sheet.Cell(1, 3).Value = "Email";
            sheet.Cell(1, 4).Value = "Teléfono";

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                var p = items[i];
                sheet.Cell(row, 1).Value = p.Id;
                sheet.Cell(row, 2).Value = p.Nombre;
                sheet.Cell(row, 3).Value = p.Email;
                sheet.Cell(row, 4).Value = p.Telefono;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);

            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Proveedores.xlsx");
        }
    }
}
