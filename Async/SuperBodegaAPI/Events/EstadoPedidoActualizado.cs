using System.Collections.Generic;

namespace SuperBodegaAPI.Events
{
    // DTO para incluir cada producto en el correo
    public record VentaItemMailDto(
        string NombreProducto,
        int Cantidad,
        decimal PrecioUnitario
    );

    // Evento que ahora lleva la lista de items
    public record EstadoPedidoActualizado(
        int VentaId,
        string NuevoEstado,
        string EmailCliente,
        string CodigoSeguimiento,
        List<VentaItemMailDto> Items
    );
}
