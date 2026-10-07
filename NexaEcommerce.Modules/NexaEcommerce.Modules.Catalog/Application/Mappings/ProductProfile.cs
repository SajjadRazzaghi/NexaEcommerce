using AutoMapper;

using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Application.Mappings;

public sealed class ProductProfile : Profile
{
    public ProductProfile()
    {
        // =========================================================
        // Product
        // =========================================================

        CreateMap<Product, ProductDto>()
            .ForMember(
                dest => dest.FinalPrice,
                opt => opt.MapFrom(
                    src => src.GetFinalPrice()))

            .ForMember(
                dest => dest.BrandName,
                opt => opt.MapFrom(
                    src => src.Brand != null
                        ? src.Brand.Name
                        : null))

            .ForMember(
                dest => dest.ManufacturerName,
                opt => opt.MapFrom(
                    src => src.Manufacturer != null
                        ? src.Manufacturer.Name
                        : null))

            .ForMember(
                dest => dest.Images,
                opt => opt.MapFrom(
                    src => src.Images
                        .OrderBy(x => x.DisplayOrder)))

            .ForMember(
                dest => dest.Categories,
                opt => opt.MapFrom(
                    src => src.ProductCategories
                        .Where(x => x.Category != null)
                        .Select(x => x.Category!.Name)))

            .ForMember(
                dest => dest.CategoryIds,
                opt => opt.MapFrom(
                    src => src.ProductCategories
                        .Select(x => x.CategoryId)))

            .ForMember(
                dest => dest.Variants,
                opt => opt.MapFrom(
                    src => src.Variants
                        .OrderBy(x => x.Sku)))

            .ForMember(
                dest => dest.Attributes,
                opt => opt.MapFrom(
                    src => src.Attributes
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.Name)))

            /*
             * Legacy compatibility.
             *
             * Inventory will become the source of truth in the next
             * phase, so these mappings are intentionally temporary.
             */
            .ForMember(
                dest => dest.StockQuantity,
                opt => opt.MapFrom(
                    src => src.Variants
                        .Where(x => x.IsActive)
                        .Sum(x => x.StockQuantity)))

            .ForMember(
                dest => dest.IsInStock,
                opt => opt.MapFrom(
                    src => src.Variants
                        .Any(
                            x =>
                                x.IsActive &&
                                x.StockQuantity > 0)))

            .ForMember(
                dest => dest.AverageRating,
                opt => opt.MapFrom(
                    src => src.Reviews
                        .Where(x => x.IsApproved)
                        .Select(x => (double?)x.Rating)
                        .Average() ?? 0))

            .ForMember(
                dest => dest.ReviewCount,
                opt => opt.MapFrom(
                    src => src.Reviews
                        .Count(x => x.IsApproved)));

        // =========================================================
        // Product Image
        // =========================================================

        CreateMap<ProductImage, ProductImageDto>();

        // =========================================================
        // Product Attribute
        // =========================================================

        CreateMap<ProductAttribute, ProductAttributeDto>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(
                    src => src.Role.ToString()))

            .ForMember(
                dest => dest.RoleValue,
                opt => opt.MapFrom(
                    src => (int)src.Role));

        // =========================================================
        // Product Attribute Value
        // =========================================================

        CreateMap<AttributeValue, ProductAttributeValueDto>();

        // =========================================================
        // Product Variant
        // =========================================================

        CreateMap<ProductVariant, ProductVariantDto>()
         .ForMember(
             dest => dest.Attributes,
             opt => opt.MapFrom(
                 src => src.AttributeValues
                     .Where(
                         x =>
                             x.AttributeValue != null &&
                             x.AttributeValue.ProductAttribute != null)
                     .Select(
                         x =>
                             new ProductVariantAttributeDto
                             {
                                 AttributeValueId =
                                     x.AttributeValueId,

                                 ProductAttributeId =
                                     x.AttributeValue!
                                         .ProductAttributeId,

                                 CatalogAttributeId =
                                     x.AttributeValue!
                                         .ProductAttribute
                                         .CatalogAttributeId,

                                 CatalogAttributeValueId =
                                     x.AttributeValue!
                                         .CatalogAttributeValueId,

                                 AttributeCode =
                                     x.AttributeValue!
                                         .ProductAttribute
                                         .Code,

                                 AttributeName =
                                     x.AttributeValue!
                                         .ProductAttribute
                                         .Name,

                                 Role =
                                     x.AttributeValue!
                                         .ProductAttribute
                                         .Role
                                         .ToString(),

                                 RoleValue =
                                     (int)
                                     x.AttributeValue!
                                         .ProductAttribute
                                         .Role,

                                 Value =
                                     x.AttributeValue!
                                         .Value,

                                 DisplayValue =
                                     x.AttributeValue!
                                         .DisplayValue,
                                 ColorHex =
                                     x.AttributeValue!
                                         .ColorHex,
                             })
                     .OrderBy(
                         x => x.AttributeName)
                     .ThenBy(
                         x => x.Value)
                     .ToList()
             )
         );

        // =========================================================
        // Product Variant Image
        // =========================================================

        CreateMap<ProductVariantImage, ProductVariantImageDto>();
    }
}