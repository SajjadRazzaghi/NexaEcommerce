import {
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import {
    createCheckout,
} from '../api/checkoutApi';

import type {
    CheckoutRequest,
} from '../types';

export function useCheckout() {
    const queryClient =
        useQueryClient();

    return useMutation({
        mutationFn: ({
            request,
            idempotencyKey,
        }: {
            request: CheckoutRequest;
            idempotencyKey: string;
        }) =>
            createCheckout(
                request,
                idempotencyKey,
            ),

        onSuccess: async order => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: ['cart'],
                }),

                queryClient.invalidateQueries({
                    queryKey: ['order', order.id],
                }),

                queryClient.invalidateQueries({
                    queryKey: ['orders'],
                }),
            ]);
        },
    });
}

