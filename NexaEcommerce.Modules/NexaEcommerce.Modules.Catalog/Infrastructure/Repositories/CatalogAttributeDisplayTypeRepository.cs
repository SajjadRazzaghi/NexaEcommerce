using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;

namespace NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;

public sealed class CatalogAttributeDisplayTypeRepository
    : ICatalogAttributeDisplayTypeRepository
{
    private readonly CatalogDbContext _context;

    public CatalogAttributeDisplayTypeRepository(
        CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogAttributeDisplayType>>
        GetAllActiveAsync(
            CancellationToken cancellationToken = default)
    {
        return await _context.CatalogAttributeDisplayTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogAttributeDisplayType?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.CatalogAttributeDisplayTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Code == code,
                cancellationToken);
    }
}