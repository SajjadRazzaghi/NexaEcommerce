using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Application.Mappings;

public sealed class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>()
            .ForMember(
                dest => dest.ParentCategoryName,
                opt => opt.MapFrom(
                    src => src.ParentCategory != null
                        ? src.ParentCategory.Name
                        : null))
            .ForMember(
                dest => dest.ProductCount,
                opt => opt.MapFrom(
                    src => src.ProductCategories.Count));
    }
}