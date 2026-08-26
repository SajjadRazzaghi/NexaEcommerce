// src/modules/catalog/brands/hooks.ts
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/hooks/use-auth';
import { brandsApi, type BrandFilter } from '@/modules/catalog/api/brands';

export const brandKeys = {
    all: ['brands'] as const,
    lists: () => [...brandKeys.all, 'list'] as const,
    list: (filter: BrandFilter) => [...brandKeys.lists(), filter] as const,
    details: () => [...brandKeys.all, 'detail'] as const,
    detail: (id: string) => [...brandKeys.details(), id] as const,
    lookup: () => [...brandKeys.all, 'lookup'] as const,
};

export function useBrands(filter: BrandFilter) {
    const { user, isLoading } = useAuth();

    return useQuery({
        queryKey: brandKeys.list(filter),
        queryFn: ({ signal }) => brandsApi.list(filter, signal),
        enabled: !isLoading && !!user,
    });
}

export function useBrand(id?: string) {
    const { user, isLoading } = useAuth();

    return useQuery({
        queryKey: brandKeys.detail(id ?? ''),
        queryFn: () => brandsApi.get(id!),
        enabled: !isLoading && !!user && !!id,
    });
}

export function useCreateBrand() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: brandsApi.create,
        onSuccess: () => qc.invalidateQueries({ queryKey: brandKeys.all }),
    });
}

export function useUpdateBrand() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ id, body }: { id: string; body: Parameters<typeof brandsApi.update>[1] }) =>
            brandsApi.update(id, body),
        onSuccess: (_, variables) => {
            qc.invalidateQueries({ queryKey: brandKeys.all });
            qc.invalidateQueries({ queryKey: brandKeys.detail(variables.id) });
        },
    });
}

export function useBrandAction(action: keyof Pick<typeof brandsApi, 'remove' | 'restore' | 'activate' | 'deactivate' | 'publish' | 'unpublish' | 'feature' | 'unfeature'>) {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => brandsApi[action](id) as Promise<void>,
        onSuccess: (_, id) => {
            qc.invalidateQueries({ queryKey: brandKeys.all });
            qc.invalidateQueries({ queryKey: brandKeys.detail(id) });
        },
    });
}