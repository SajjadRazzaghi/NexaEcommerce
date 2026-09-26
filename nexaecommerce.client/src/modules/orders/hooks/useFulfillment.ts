import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    allocateWarehouse,
    getFulfillment,
    markPacked,
    markPicked,
    markReadyToShip,
    reserveStock,
    rollbackFulfillment,
    startFulfillment,
    startPacking,
    startPicking,
} from '../api/fulfillmentApi';

export function fulfillmentQueryKey(
    orderId: string,
) {
    return [
        'fulfillment',
        orderId,
    ] as const;
}

export function useFulfillment(
    orderId?: string,
) {
    return useQuery({
        queryKey:
            fulfillmentQueryKey(
                orderId ?? '',
            ),

        queryFn: () =>
            getFulfillment(
                orderId!,
            ),

        enabled:
            Boolean(orderId),
    });
}

export function useFulfillmentMutations(
    orderId: string,
) {
    const queryClient =
        useQueryClient();

    const invalidate =
        async () => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey:
                        fulfillmentQueryKey(
                            orderId,
                        ),
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'admin',
                        'orders',
                        orderId,
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'admin',
                        'orders',
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'order',
                        orderId,
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'orders',
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'shipment',
                        orderId,
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'orders',
                        orderId,
                        'shipment',
                    ],
                }),

                queryClient.invalidateQueries({
                    queryKey: [
                        'admin',
                        'orders',
                        orderId,
                        'shipment',
                    ],
                }),
            ]);
        };

    const start =
        useMutation({
            mutationFn: () =>
                startFulfillment(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const allocate =
        useMutation({
            mutationFn: () =>
                allocateWarehouse(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const reserve =
        useMutation({
            mutationFn: () =>
                reserveStock(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const picking =
        useMutation({
            mutationFn: () =>
                startPicking(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const picked =
        useMutation({
            mutationFn: () =>
                markPicked(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const packing =
        useMutation({
            mutationFn: () =>
                startPacking(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const packed =
        useMutation({
            mutationFn: () =>
                markPacked(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const readyToShip =
        useMutation({
            mutationFn: () =>
                markReadyToShip(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    const rollback =
        useMutation({
            mutationFn: () =>
                rollbackFulfillment(
                    orderId,
                ),

            onSuccess:
                invalidate,
        });

    return {
        start,
        allocate,
        reserve,
        picking,
        picked,
        packing,
        packed,
        readyToShip,
        rollback,
    };
}