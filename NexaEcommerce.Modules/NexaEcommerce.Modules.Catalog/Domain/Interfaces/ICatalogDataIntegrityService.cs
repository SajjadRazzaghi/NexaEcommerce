using NexaEcommerce.Modules.Catalog.Domain.Models;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface ICatalogDataIntegrityService
{
    Task<CatalogDataIntegrityReport> AuditAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogDataIntegrityReport> BackfillAsync(
        CancellationToken cancellationToken = default);
}