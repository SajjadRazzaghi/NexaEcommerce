using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using System.Runtime.InteropServices;

namespace NexaEcommerce.Modules.Inventory.Presentation.Controllers;

[ApiController]
[Route("inventory/reservations")]
[Authorize]
public sealed class WarehouseStockReservationController(
    IWarehouseStockReservationService service,
    ITenantContext tenantContext)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "inventory.manage")]
    public async Task<ActionResult<WarehouseStockReservationDto>>
        Reserve(
            [FromBody] ReserveWarehouseStockRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await service.ReserveAsync(
                tenantContext.TenantId,
                request,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost("{reservationId:guid}/release")]
    [Authorize(Policy = "inventory.manage")]
    public async Task<ActionResult<WarehouseStockReservationDto>>
        Release(
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        var result =
            await service.ReleaseAsync(
                tenantContext.TenantId,
                reservationId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost("{reservationId:guid}/commit")]
    [Authorize(Policy = "inventory.manage")]
    public async Task<ActionResult<WarehouseStockReservationDto>>
        Commit(
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        var result =
            await service.CommitAsync(
                tenantContext.TenantId,
                reservationId,
                cancellationToken);

        return Ok(result);
    }
}