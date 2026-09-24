import {
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import {
    completePayment,
    retryPayment,
    startPayment,
    verifyPayment,
} from '../api/paymentsApi';

export function useStartPayment() {
    return useMutation({
        mutationFn: ({
            orderId,
            gatewayName,
            idempotencyKey,
        }: {
            orderId: string;
            gatewayName: string;
            idempotencyKey: string;
        }) =>
            startPayment(
                {
                    orderId,
                    gatewayName,
                },
                idempotencyKey,
            ),
    });
}

export function useVerifyPayment() {
    return useMutation({
        mutationFn: verifyPayment,
    });
}

export function useRetryPayment() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn: ({
            orderId,
            idempotencyKey,
        }: {
            orderId: string;
            idempotencyKey: string;
        }) =>
            retryPayment(
                orderId,
                idempotencyKey,
            ),

        onSuccess:
            async result => {
                await Promise.all([
                    queryClient.invalidateQueries({
                        queryKey: [
                            'order',
                            result.orderId,
                        ],
                    }),

                    queryClient.invalidateQueries({
                        queryKey: [
                            'orders',
                        ],
                    }),
                ]);
            },
    });
}

export function useCompletePayment() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn:
            completePayment,

        onSuccess:
            async result => {
                await Promise.all([
                    /*
                     * Order details must immediately show the
                     * new Paid state.
                     */
                    queryClient.invalidateQueries({
                        queryKey: [
                            'order',
                            result.orderId,
                        ],
                    }),

                    /*
                     * My Orders list must immediately reflect
                     * the new payment/order state.
                     */
                    queryClient.invalidateQueries({
                        queryKey: [
                            'orders',
                        ],
                    }),

                    /*
                     * The storefront cart indicator depends on
                     * this query. After successful payment the cart
                     * must be refreshed so the header changes from
                     * "filled" to "empty" whenever the backend has
                     * consumed the cart.
                     */
                    queryClient.invalidateQueries({
                        queryKey: [
                            'cart',
                        ],
                    }),
                ]);
            },
    });
}