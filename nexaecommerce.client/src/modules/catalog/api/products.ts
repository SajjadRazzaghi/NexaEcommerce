import { api } from '@/lib/api/client';

export type ProductVariant = {
    id: string;
    sku: string;
    color?: string | null;
    size?: string | null;
    priceOverride?: number | null;
    stockQuantity: number;
    isActive: boolean;
};

export type ProductImage = {
    id: string;
    imageUrl: string;
    altText?: string | null;
    displayOrder: number;
    isMain: boolean;
};

export type Product = {
    id: string;
    name: string;
    sku: string;
    slug: string;

    description?: string | null;
    shortDescription?: string | null;

    price: number;
    comparePrice?: number | null;
    finalPrice: number;
    discountPercentage: number;

    discountStartDate?: string | null;
    discountEndDate?: string | null;

    currency: string;

    brandId?: string | null;
    brandName?: string | null;

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

    manufacturerId?: string | null;
    manufacturerName?: string | null;

    createdAt: string;
    updatedAt?: string | null;
};

export type ProductListItem = {
    id: string;
    name: string;
    sku: string;
    slug: string;
    price: number;
    comparePrice?: number | null;
    finalPrice: number;
    discountPercentage: number;
    currency: string;
    brandId?: string | null;
    brandName?: string | null;
    isActive: boolean;
    isFeatured: boolean;
    isPublished: boolean;
    isInStock: boolean;
    stockQuantity: number;
    mainImage?: string | null;
    categoryNames: string[];
    categoryIds: string[];
    createdAt: string;
};

export type ProductListResponse = {
    items: ProductListItem[];
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
};

export type ProductFilter = {
    page?: number;
    pageSize?: number;
    search?: string;
    categoryId?: string;
    brandId?: string;
    isActive?: boolean;
    isFeatured?: boolean;
    isInStock?: boolean;
    minPrice?: number;
    maxPrice?: number;
    sortBy?:
    | 'newest'
    | 'price_asc'
    | 'price_desc'
    | 'name'
    | 'popular';
    desc?: boolean;
};

export type CreateProductVariantDto = {
    sku: string;
    color?: string;
    size?: string;
    priceOverride?: number;
    stockQuantity: number;
};

export type CreateProductDto = {
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
};

export type UpdateProductDto = {
    name: string;
    price: number;
    currency?: string;
    description?: string;
    shortDescription?: string;
    comparePrice?: number | null;
    discountPercentage?: number | null;
    brandId?: string | null;
    categoryIds: string[];
    isActive: boolean;
    isFeatured: boolean;
    isPublished: boolean;
};

export const productsApi = {
    getAll: (params?: ProductFilter) =>
        api.get<ProductListResponse>(
            '/products',
            { params },
        ),

    getAdminAll: (
        params?: ProductFilter & {
            isPublished?: boolean;
        },
    ) =>
        api.get<ProductListResponse>(
            '/products/admin',
            { params },
        ),

    getFeatured: (count = 8) =>
        api.get<Product[]>(
            '/products/featured',
            {
                params: { count },
            },
        ),

    getById: (id: string) =>
        api.get<Product>(
            `/products/${id}`,
        ),

    getBySlug: (slug: string) =>
        api.get<Product>(
            `/products/slug/${encodeURIComponent(slug)}`,
        ),

    search: (
        query: string,
        params?: ProductFilter,
    ) =>
        api.get<Product[]>(
            '/products/search',
            {
                params: {
                    q: query,
                    ...params,
                },
            },
        ),

    getByCategory: (
        categoryId: string,
        params?: ProductFilter,
    ) =>
        api.get<Product[]>(
            `/products/category/${categoryId}`,
            { params },
        ),

    create: (
        data: CreateProductDto,
    ) =>
        api.post<Product>(
            '/products',
            data,
        ),

    update: (
        id: string,
        data: UpdateProductDto,
    ) =>
        api.put<void>(
            `/products/${id}`,
            data,
        ),

    updateStock: (
        id: string,
        quantity: number,
    ) =>
        api.patch<void>(
            `/products/${id}/stock`,
            { quantity },
        ),

    toggleActive: (
        id: string,
        isActive: boolean,
    ) =>
        api.patch<void>(
            `/products/${id}/active`,
            { value: isActive },
        ),

    toggleFeatured: (
        id: string,
        isFeatured: boolean,
    ) =>
        api.patch<void>(
            `/products/${id}/featured`,
            { value: isFeatured },
        ),

    delete: (id: string) =>
        api.del<void>(
            `/products/${id}`,
        ),
};