using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
            => await _context.Clientes.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var c = await _context.Clientes.FindAsync(id);
            if (c == null) return NotFound();
            return c;
        }

        [HttpGet("PorEmail/{email}")]
        public async Task<ActionResult<Cliente>> GetPorEmail(string email)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

            if (cliente == null)
                return NotFound();

            return cliente;
        }

        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.Id)
                return BadRequest("El Id de la URL no coincide con el de la entidad.");

            _context.Entry(cliente).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Clientes.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var c = await _context.Clientes.FindAsync(id);
            if (c == null) return NotFound();
            _context.Clientes.Remove(c);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("Export")]
        public async Task<IActionResult> Export()
        {
            var items = await _context.Clientes.ToListAsync();
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Clientes");
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Nombre";
            sheet.Cell(1, 3).Value = "Email";
            sheet.Cell(1, 4).Value = "Teléfono";

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                var c = items[i];
                sheet.Cell(row, 1).Value = c.Id;
                sheet.Cell(row, 2).Value = c.Nombre;
                sheet.Cell(row, 3).Value = c.Email;
                sheet.Cell(row, 4).Value = c.Telefono;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Clientes.xlsx");
        }

        [HttpGet("Count")]
        public async Task<ActionResult<int>> CountClientes()
            => await _context.Clientes.CountAsync();
    }
}
