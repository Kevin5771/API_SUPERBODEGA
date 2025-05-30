using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> Get()
            => await _context.Products.Include(p => p.Proveedor).ToListAsync();

        [HttpGet("Catalogo")]
        public async Task<ActionResult<CatalogoResponse>> Catalogo()
        {
            var lista = await _context.Products.Include(p => p.Proveedor)
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    Stock = p.Stock,
                    Categoria = p.Categoria,
                    Proveedor = p.Proveedor.Nombre,
                    ImagenUrl = string.IsNullOrEmpty(p.ImagenUrl)
                        ? "/images/placeholder.png"
                        : $"/images/productos/{p.ImagenUrl}"
                }).ToListAsync();

            return Ok(new CatalogoResponse { Productos = lista });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductInputModel>> Get(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();

            return new ProductInputModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Precio = p.Precio,
                Stock = p.Stock,
                Categoria = p.Categoria,
                ImagenUrl = p.ImagenUrl, // solo nombre del archivo
                ProveedorId = p.ProveedorId
            };
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Post([FromBody] ProductInputModel input)
        {
            if (!await _context.Proveedores.AnyAsync(p => p.Id == input.ProveedorId))
                return BadRequest("Proveedor no válido.");

            var product = new Product
            {
                Nombre = input.Nombre,
                Precio = input.Precio,
                Stock = input.Stock,
                Categoria = input.Categoria,
                ImagenUrl = Path.GetFileName(input.ImagenUrl), // solo nombre
                ProveedorId = input.ProveedorId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ProductInputModel input)
        {
            if (id != input.Id) return BadRequest("Id de URL distinto al del body.");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Nombre = input.Nombre;
            product.Precio = input.Precio;
            product.Stock = input.Stock;
            product.Categoria = input.Categoria;
            product.ImagenUrl = Path.GetFileName(input.ImagenUrl);
            product.ProveedorId = input.ProveedorId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("Export")]
        public async Task<IActionResult> Export()
        {
            var items = await _context.Products.Include(p => p.Proveedor).ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Productos");
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Nombre";
            sheet.Cell(1, 3).Value = "Precio";
            sheet.Cell(1, 4).Value = "Stock";
            sheet.Cell(1, 5).Value = "Categoria";
            sheet.Cell(1, 6).Value = "Proveedor";

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                var p = items[i];
                sheet.Cell(row, 1).Value = p.Id;
                sheet.Cell(row, 2).Value = p.Nombre;
                sheet.Cell(row, 3).Value = p.Precio;
                sheet.Cell(row, 4).Value = p.Stock;
                sheet.Cell(row, 5).Value = p.Categoria;
                sheet.Cell(row, 6).Value = p.Proveedor.Nombre;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Productos.xlsx");
        }

        [HttpGet("Count")]
        public async Task<ActionResult<int>> CountProductos()
            => await _context.Products.CountAsync();

        public class ProductInputModel
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public decimal Precio { get; set; }
            public int Stock { get; set; }
            public string Categoria { get; set; } = string.Empty;
            public string ImagenUrl { get; set; } = string.Empty; // solo el nombre de archivo
            public int ProveedorId { get; set; }
        }

        public class ProductoDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = default!;
            public decimal Precio { get; set; }
            public int Stock { get; set; }
            public string Categoria { get; set; } = default!;
            public string Proveedor { get; set; } = default!;
            public string ImagenUrl { get; set; } = default!;
        }

        public class CatalogoResponse
        {
            public List<ProductoDto> Productos { get; set; } = new();
        }
    }
}
