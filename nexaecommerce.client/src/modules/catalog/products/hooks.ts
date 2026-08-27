import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/hooks/use-auth';
import { productsApi } from '../api/products';
import type {
    CreateProductDto,
    ProductFilter,
    UpdateProductDto,
} from '../api/products';

const PRODUCTS_KEY = 'products';

export const useProducts = (params?: ProductFilter) =>
    useQuery({
        queryKey: [PRODUCTS_KEY, 'public', params],
        queryFn: async () => (await productsApi.getAll(params)),
    });

export const useAdminProducts = (params?: ProductFilter & { isPublished?: boolean }) => {
    const { user, isLoading } = useAuth();

    return useQuery({
        queryKey: [PRODUCTS_KEY, 'admin', params],
        queryFn: async () => productsApi.getAdminAll(params),
        enabled: !isLoading && !!user,
    });
};

export const useFeaturedProducts = (count = 8) =>
    useQuery({
        queryKey: [PRODUCTS_KEY, 'featured', count],
        queryFn: async () => (await productsApi.getFeatured(count)),
    });

export const useProduct = (id?: string) =>
    useQuery({
        queryKey: [PRODUCTS_KEY, id],
        queryFn: async () => {
            if (!id) throw new Error('Product ID is required.');
            return (await productsApi.getById(id));
        },
        enabled: Boolean(id),
    });

export const useSearchProducts = (query: string, params?: ProductFilter) =>
    useQuery({
        queryKey: [PRODUCTS_KEY, 'search', query, params],
        queryFn: async () => (await productsApi.search(query, params)),
        enabled: query.trim().length > 0,
    });

export const useCreateProduct = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (data: CreateProductDto) =>
            (await productsApi.create(data)),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY] });
        },
    });
};

export const useUpdateProduct = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({ id, data }: { id: string; data: UpdateProductDto }) => {
            await productsApi.update(id, data);
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY] });
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY, variables.id] });
        },
    });
};
export const useProductBySlug = (slug?: string) =>
    useQuery({
        queryKey: [PRODUCTS_KEY, 'slug', slug],
        queryFn: async () => {
            if (!slug) {
                throw new Error('Product slug is required.');
            }

            return await productsApi.getBySlug(slug);
        },
        enabled: Boolean(slug),
    });
export const useUpdateStock = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({ id, quantity }: { id: string; quantity: number }) => {
            await productsApi.updateStock(id, quantity);
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY] });
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY, variables.id] });
        },
    });
};

export const useDeleteProduct = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (id: string) => {
            await productsApi.delete(id);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY] });
        },
    });
};

export const useToggleActive = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
            await productsApi.toggleActive(id, isActive);
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY] });
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY, variables.id] });
        },
    });
};

export const useToggleFeatured = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({ id, isFeatured }: { id: string; isFeatured: boolean }) => {
            await productsApi.toggleFeatured(id, isFeatured);
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY] });
            queryClient.invalidateQueries({ queryKey: [PRODUCTS_KEY, variables.id] });
        },
    });
};
