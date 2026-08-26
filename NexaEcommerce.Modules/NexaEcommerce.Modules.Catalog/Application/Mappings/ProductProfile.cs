using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Application.Mappings;

public sealed class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(
                dest => dest.FinalPrice,
                opt => opt.MapFrom(src => src.GetFinalPrice()))
            .ForMember(
                dest => dest.BrandName,
                opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
            .ForMember(
                dest => dest.Images,
                opt => opt.MapFrom(src => src.Images.OrderBy(i => i.DisplayOrder)))
            .ForMember(
                dest => dest.Categories,
                opt => opt.MapFrom(src => src.ProductCategories
                    .Where(pc => pc.Category != null)
                    .Select(pc => pc.Category!.Name)))
            .ForMember(
                dest => dest.CategoryIds,
                opt => opt.MapFrom(src => src.ProductCategories.Select(pc => pc.CategoryId)))
            .ForMember(
                dest => dest.Variants,
                opt => opt.MapFrom(src => src.Variants))
            .ForMember(
                dest => dest.StockQuantity,
                opt => opt.MapFrom(src => src.Variants
                    .Where(v => v.IsActive)
                    .Sum(v => v.StockQuantity)))
            .ForMember(
                dest => dest.IsInStock,
                opt => opt.MapFrom(src => src.Variants.Any(v => v.IsActive && v.StockQuantity > 0)))
            .ForMember(
                dest => dest.AverageRating,
                opt => opt.MapFrom(src => src.Reviews
                    .Where(r => r.IsApproved)
                    .Select(r => (double?)r.Rating)
                    .Average() ?? 0))
            .ForMember(
                dest => dest.ReviewCount,
                opt => opt.MapFrom(src => src.Reviews.Count(r => r.IsApproved)));

        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(
                dest => dest.Color,
                opt => opt.MapFrom(src => src.AttributeValues
                    .Where(x => x.AttributeValue != null && x.AttributeValue.ProductAttribute != null)
                    .Where(x => x.AttributeValue!.ProductAttribute.Code == "color")
                    .Select(x => x.AttributeValue!.DisplayValue ?? x.AttributeValue!.Value)
                    .FirstOrDefault()))
            .ForMember(
                dest => dest.Size,
                opt => opt.MapFrom(src => src.AttributeValues
                    .Where(x => x.AttributeValue != null && x.AttributeValue.ProductAttribute != null)
                    .Where(x => x.AttributeValue!.ProductAttribute.Code == "size")
                    .Select(x => x.AttributeValue!.DisplayValue ?? x.AttributeValue!.Value)
                    .FirstOrDefault()));
    }
}
