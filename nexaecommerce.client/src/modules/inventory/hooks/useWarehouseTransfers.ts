import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    warehouseTransferApi,
    type CreateWarehouseTransferRequest,
} from '@/lib/api/inventory';

export const warehouseTransferQueryKeys = {
    all: [
        'inventory',
        'warehouse-transfers',
    ] as const,

    lists: () => [
        'inventory',
        'warehouse-transfers',
        'list',
    ] as const,

    list: (
        skip: number,
        take: number,
    ) => [
        'inventory',
        'warehouse-transfers',
        'list',
        skip,
        take,
    ] as const,
};

export function useWarehouseTransfers(
    skip = 0,
    take = 20,
) {
    return useQuery({
        queryKey:
            warehouseTransferQueryKeys.list(
                skip,
                take,
            ),

        queryFn: async () => {
            return await warehouseTransferApi.list(
                skip,
                take,
            );
        },

        staleTime: 5_000,
    });
}

export function useCreateWarehouseTransfer() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn: async (
            request: CreateWarehouseTransferRequest,
        ) => {
            return await warehouseTransferApi.create(
                request,
            );
        },

        onSuccess: async () => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey:
                        warehouseTransferQueryKeys.all,
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'inventory',
                        'warehouse-stock',
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'inventory',
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'products',
                    ],
                }),
            ]);
        },
    });
}