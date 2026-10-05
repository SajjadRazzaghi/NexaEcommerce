using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;

namespace NexaEcommerce.Modules.Catalog.Features.CatalogDataIntegrity;

public static class CatalogDataIntegrityEndpoints
{
    public static void MapCatalogDataIntegrityEndpoints(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup(
                "/api/catalog/data-integrity");

        group.MapGet(
            "/audit",
            async (
                ICatalogDataIntegrityService service,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                var report =
                    await service.AuditAsync(
                        cancellationToken);

                return Results.Ok(
                    mapper.Map<CatalogDataIntegrityReportDto>(
                        report));
            });

        group.MapPost(
            "/backfill",
            async (
                ICatalogDataIntegrityService service,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                var report =
                    await service.BackfillAsync(
                        cancellationToken);

                return Results.Ok(
                    mapper.Map<CatalogDataIntegrityReportDto>(
                        report));
            });
    }
}