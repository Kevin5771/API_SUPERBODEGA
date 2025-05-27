// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using SuperBodegaAPI.Models;

namespace SuperBodegaAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<DetalleCompra> DetalleCompras { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<CarritoItem> CarritoItems { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Compra → Proveedor
            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Proveedor)
                .WithMany(p => p.Compras)
                .HasForeignKey(c => c.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Compra → Producto
            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Producto)
                .WithMany(p => p.Compras)
                .HasForeignKey(c => c.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // DetalleCompra → Compra
            modelBuilder.Entity<DetalleCompra>()
                .HasOne(dc => dc.Compra)
                .WithMany(c => c.DetalleCompras)
                .HasForeignKey(dc => dc.CompraId)
                .OnDelete(DeleteBehavior.Cascade);

            // Venta → Cliente
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta → Producto
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Producto)
                .WithMany(p => p.Ventas)
                .HasForeignKey(v => v.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // DetalleVenta → Venta
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Venta)
                .WithMany(v => v.DetalleVentas)
                .HasForeignKey(dv => dv.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            // DetalleVenta → Producto
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Producto)
                .WithMany(p => p.DetalleVentas)
                .HasForeignKey(dv => dv.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // CarritoItem → Producto
            modelBuilder.Entity<CarritoItem>()
                .HasOne(ci => ci.Producto)
                .WithMany(p => p.CarritoItems)
                .HasForeignKey(ci => ci.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // CarritoItem → Cliente
            modelBuilder.Entity<CarritoItem>()
                .HasOne(ci => ci.Cliente)
                .WithMany(c => c.CarritoItems)
                .HasForeignKey(ci => ci.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}