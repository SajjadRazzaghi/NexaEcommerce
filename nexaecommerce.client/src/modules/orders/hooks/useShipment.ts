import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    createShipment,
    deliverOrder,
    getShipment,
    shipOrder,
    updateShipmentTrackingNumber,
} from '../api/shipmentApi';

export function shipmentQueryKey(
    orderId: string,
) {
    return [
        'orders',
        orderId,
        'shipment',
    ] as const;
}

export function useShipment(
    orderId?: string,
) {
    return useQuery({
        queryKey:
            shipmentQueryKey(
                orderId ?? '',
            ),
        queryFn: () =>
            getShipment(
                orderId!,
            ),
        enabled:
            Boolean(orderId),
    });
}

export function useShipmentMutations(
    orderId: string,
) {
    const queryClient =
        useQueryClient();

    async function invalidate() {
        await Promise.all([
            queryClient.invalidateQueries({
                queryKey:
                    shipmentQueryKey(
                        orderId,
                    ),
            }),
            queryClient.invalidateQueries({
                queryKey: [
                    'order',
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
                    'orders',
                ],
            }),
        ]);
    }

    const create =
        useMutation({
            mutationFn: ({
                shippingMethod,
                carrier,
                trackingNumber,
            }: {
                shippingMethod: string;
                carrier: string;
                trackingNumber?: string | null;
            }) =>
                createShipment(
                    orderId,
                    shippingMethod,
                    carrier,
                    trackingNumber,
                ),
            onSuccess:
                invalidate,
        });

    const updateTracking =
        useMutation({
            mutationFn: ({
                trackingNumber,
            }: {
                trackingNumber: string;
                }) =>
                updateShipmentTrackingNumber(
                    orderId,
                    trackingNumber,
                ),
            onSuccess:
                invalidate,
        });

    const ship =
        useMutation({
            mutationFn: () =>
                shipOrder(
                    orderId,
                ),
            onSuccess:
                invalidate,
        });

    const deliver =
        useMutation({
            mutationFn: () =>
                deliverOrder(
                    orderId,
                ),
            onSuccess:
                invalidate,
        });

    return {
        create,
        updateTracking,
        ship,
        deliver,
    };
}