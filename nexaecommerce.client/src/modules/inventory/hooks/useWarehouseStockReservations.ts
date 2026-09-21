import {
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import {
    warehouseStockReservationApi,
    type ReserveWarehouseStockRequest,
} from '@/lib/api/inventory';

export const warehouseReservationQueryKeys = {
    all: [
        'inventory',
        'reservations',
    ] as const,
};

export function useReserveWarehouseStock() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn: (
            request: ReserveWarehouseStockRequest,
        ) =>
            warehouseStockReservationApi.reserve(
                request,
            ),

        onSuccess: async () => {
            await Promise.all([
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

export function useReleaseWarehouseStock() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn: (
            reservationId: string,
        ) =>
            warehouseStockReservationApi.release(
                reservationId,
            ),

        onSuccess: async () => {
            await queryClient.invalidateQueries({
                queryKey: [
                    'inventory',
                ],
            });
        },
    });
}

export function useCommitWarehouseStock() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn: (
            reservationId: string,
        ) =>
            warehouseStockReservationApi.commit(
                reservationId,
            ),

        onSuccess: async () => {
            await Promise.all([
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