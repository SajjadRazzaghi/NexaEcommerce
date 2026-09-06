// ============================================================
// src/modules/catalog/catalogAttributes/hooks.ts
// ============================================================

import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    catalogAttributesApi,
    type CatalogAttribute,
} from '@/modules/catalog/api/catalogAttributes';

export const catalogAttributeKeys = {
    all: ['catalog-attributes'] as const,

    list: (search?: string) =>
        [...catalogAttributeKeys.all, 'list', search ?? ''] as const,

    detail: (id: string) =>
        [...catalogAttributeKeys.all, 'detail', id] as const,
};

export function useCatalogAttributes(
    search?: string,
) {
    return useQuery({
        queryKey: catalogAttributeKeys.list(search),
        queryFn: ({ signal }) =>
            catalogAttributesApi.list(
                search,
                signal,
            ),
        staleTime: 60_000,
    });
}

export function useCatalogAttribute(
    id?: string,
) {
    return useQuery({
        queryKey: catalogAttributeKeys.detail(id ?? ''),
        queryFn: () =>
            catalogAttributesApi.get(id!),
        enabled: !!id,
    });
}

export function useCreateCatalogAttribute() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: catalogAttributesApi.create,
        onSuccess: () => {
            queryClient.invalidateQueries({
                queryKey: catalogAttributeKeys.all,
            });
        },
    });
}

export function useUpdateCatalogAttribute() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            id,
            body,
        }: {
            id: string;
            body: Parameters<
                typeof catalogAttributesApi.update
            >[1];
        }) =>
            catalogAttributesApi.update(
                id,
                body,
            ),

        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({
                queryKey: catalogAttributeKeys.all,
            });

            queryClient.invalidateQueries({
                queryKey:
                    catalogAttributeKeys.detail(
                        variables.id,
                    ),
            });
        },
    });
}

export function useDeleteCatalogAttribute() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: catalogAttributesApi.remove,

        onSuccess: () => {
            queryClient.invalidateQueries({
                queryKey: catalogAttributeKeys.all,
            });
        },
    });
}

export function useCatalogAttributeValueMutations() {
    const queryClient = useQueryClient();

    const add = useMutation({
        mutationFn: ({
            attributeId,
            body,
        }: {
            attributeId: string;
            body: Parameters<
                typeof catalogAttributesApi.addValue
            >[1];
        }) =>
            catalogAttributesApi.addValue(
                attributeId,
                body,
            ),

        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({
                queryKey:
                    catalogAttributeKeys.detail(
                        variables.attributeId,
                    ),
            });

            queryClient.invalidateQueries({
                queryKey: catalogAttributeKeys.all,
            });
        },
    });

    const update = useMutation({
        mutationFn: ({
            attributeId,
            valueId,
            body,
        }: {
            attributeId: string;
            valueId: string;
            body: Parameters<
                typeof catalogAttributesApi.updateValue
            >[2];
        }) =>
            catalogAttributesApi.updateValue(
                attributeId,
                valueId,
                body,
            ),

        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({
                queryKey:
                    catalogAttributeKeys.detail(
                        variables.attributeId,
                    ),
            });

            queryClient.invalidateQueries({
                queryKey: catalogAttributeKeys.all,
            });
        },
    });

    const remove = useMutation({
        mutationFn: ({
            attributeId,
            valueId,
        }: {
            attributeId: string;
            valueId: string;
        }) =>
            catalogAttributesApi.removeValue(
                attributeId,
                valueId,
            ),

        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({
                queryKey:
                    catalogAttributeKeys.detail(
                        variables.attributeId,
                    ),
            });

            queryClient.invalidateQueries({
                queryKey: catalogAttributeKeys.all,
            });
        },
    });

    return {
        add,
        update,
        remove,
    };
}

export function getAttributeByCode(
    attributes: CatalogAttribute[] | undefined,
    code: string,
) {
    return attributes?.find(
        (attribute) =>
            attribute.code.toLowerCase() ===
            code.toLowerCase(),
    );
}

