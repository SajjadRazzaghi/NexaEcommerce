import { useQuery } from '@tanstack/react-query';

import {
    productsApi,
    type ProductFilter,
} from '../api/products';

export function useProducts(
    filters: ProductFilter = {},
) {
    return useQuery({
        queryKey: [
            'products',
            filters,
        ],
        queryFn: async () => {
            const response =
                await productsApi.getAll(
                    filters,
                );

            return response;
        },
        staleTime: 30_000,
    });
}

export function useProduct(
    id?: string,
) {
    return useQuery({
        queryKey: [
            'product',
            id,
        ],
        queryFn: async () => {
            const response =
                await productsApi.getById(
                    id!,
                );

            return response;
        },
        enabled: Boolean(id),
        staleTime: 60_000,
    });
}

export function useProductBySlug(
    slug?: string,
) {
    return useQuery({
        queryKey: [
            'product',
            'slug',
            slug,
        ],
        queryFn: async () => {
            const response =
                await productsApi.getBySlug(
                    slug!,
                );

            return response;
        },
        enabled: Boolean(slug),
        staleTime: 60_000,
    });
}

export function useFeaturedProducts(
    count = 8,
) {
    return useQuery({
        queryKey: [
            'products',
            'featured',
            count,
        ],
        queryFn: async () => {
            const response =
                await productsApi.getFeatured(
                    count,
                );

            return response;
        },
        staleTime: 60_000,
    });
}