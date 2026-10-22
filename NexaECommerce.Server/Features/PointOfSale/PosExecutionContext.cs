using NexaEcommerce.Modules.Orders.Application.Pos;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.MultiTenancy;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosExecutionContext(
    ICurrentTenant currentTenant,
    IHttpContextAccessor httpContextAccessor) : IPosExecutionContext
{
    public string TenantId => currentTenant.Id;

    public string UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return string.Empty;

            return principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
                ?? string.Empty;
        }
    }
}