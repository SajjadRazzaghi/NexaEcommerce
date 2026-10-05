using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Models;

namespace NexaEcommerce.Modules.Catalog.Application.Mappings;

public sealed class CatalogDataIntegrityProfile
    : Profile
{
    public CatalogDataIntegrityProfile()
    {
        CreateMap<
            CatalogDataIntegrityReport,
            CatalogDataIntegrityReportDto>();
    }
}