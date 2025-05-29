using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace SuperBodegaWeb.Pages
{
    public class FacturaService
    {
        public FacturaService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerarFactura(
            CarritoModel.ClienteDto cliente,
            List<CarritoModel.CarritoItemDto> items,
            decimal total,
            string estado)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // Encabezado
                    page.Header()
                        .Background(Colors.Blue.Medium)
                        .Padding(15)
                        .Row(row =>
                        {
                            row.RelativeItem().AlignLeft()
                               .Text("SUPERBODEGA")
                               .FontSize(18)
                               .FontColor(Colors.White)
                               .Bold();

                            row.RelativeItem().AlignRight()
                               .Text("FACTURA ELECTRÓNICA")
                               .FontSize(16)
                               .FontColor(Colors.White);
                        });

                    // Contenido
                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            col.Item().Text("DATOS DEL CLIENTE").Bold().FontSize(12);
                            col.Item().Text($"Nombre: {cliente.Nombre}");
                            col.Item().Text($"Email: {cliente.Email}");
                            col.Item().Text($"Teléfono: {cliente.Telefono}");
                            col.Item().Text($"Estado del pedido: {estado}").Bold();

                            col.Item().PaddingVertical(10).LineHorizontal(1);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.ConstantColumn(60);
                                    cols.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Producto").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Cant.").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Subtotal").Bold();
                                });

                                foreach (var item in items)
                                {
                                    table.Cell().BorderBottom(1).Padding(5).Text(item.NombreProducto);
                                    table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(item.Cantidad.ToString());
                                    table.Cell().BorderBottom(1).Padding(5).AlignRight().Text($"Q {item.Subtotal:0.00}");
                                }

                                table.Footer(footer =>
                                {
                                    footer.Cell().ColumnSpan(2).AlignRight().PaddingTop(5).Text("Total:").Bold();
                                    footer.Cell().AlignRight().PaddingTop(5).Text($"Q {total:0.00}").Bold();
                                });
                            });
                        });

                    // Pie de página
                    page.Footer()
                        .PaddingTop(10)
                        .BorderTop(1)
                        .AlignCenter()
                        .Text("¡Gracias por su compra en SuperBodegaWeb!").FontSize(10).Italic();
                });
            }).GeneratePdf();
        }
    }
}
