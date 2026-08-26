export type ProductVariant = {
    id: string;
    sku: string;
    color?: string;
    size?: string;
    priceOverride?: number;
    stockQuantity: number;
    isActive: boolean;
};

export type ProductImage = {
    id: string;
    imageUrl: string;
    altText?: string;
    displayOrder: number;
    isMain: boolean;
};

export interface Product {
    id: string;
    name: string;
    sku: string;
    slug: string;
    description?: string;
    shortDescription?: string;
    price: number;
    currency: string;
    comparePrice?: number | null;
    finalPrice: number;
    discountPercentage: number;
    brandName?: string | null;
    brandId?: string | null;
    manufacturerId?: string | null;
    manufacturer?: { id: string; name: string } | null;
    isActive: boolean;
    isFeatured: boolean;
    isPublished: boolean;
    isInStock: boolean;
    stockQuantity: number;
    images: ProductImage[];
    variants: ProductVariant[];
    categories: string[];
    categoryIds: string[];
    averageRating: number;
    reviewCount: number;
    createdAt: string;
}

export interface Category {
    id: string;
    name: string;
    slug?: string;
    description?: string;
    imageUrl?: string;
    parentCategoryId?: string;
    isActive: boolean;
    displayOrder: number;
    subCategories?: Category[];
}

export interface CreateProductVariantDto {
    sku: string;
    color?: string;
    size?: string;
    priceOverride?: number;
    stockQuantity: number;
}

export interface CreateProductDto {
    name: string;
    price: number;
    currency?: string;
    sku?: string;
    description?: string;
    shortDescription?: string;
    brandId?: string;
    categoryIds: string[];
    variants: CreateProductVariantDto[];
    images: string[];
}

export interface ProductFilter {
    categoryId?: string;
    brandId?: string;
    minPrice?: number;
    maxPrice?: number;
    search?: string;
    sortBy?: 'newest' | 'price_asc' | 'price_desc' | 'name' | 'popular';
    desc?: boolean;
    isActive?: boolean;
    isFeatured?: boolean;
    isInStock?: boolean;
    page?: number;
    pageSize?: number;
}
