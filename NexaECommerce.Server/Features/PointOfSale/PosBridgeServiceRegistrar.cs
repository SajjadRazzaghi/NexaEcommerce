using Microsoft.Extensions.DependencyInjection;
using NexaECommerce.Server.Platform.Features;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosBridgeServiceRegistrar : IServiceRegistrar
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<IPosBridgeService, PosBridgeService>();
    }
}
