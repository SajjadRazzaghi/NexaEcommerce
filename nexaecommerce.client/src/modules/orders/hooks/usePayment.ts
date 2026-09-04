import {
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import {
    completePayment,
    startPayment,
    verifyPayment,
} from '../api/paymentsApi';

export function useStartPayment() {
    return useMutation({
        mutationFn: ({
            orderId,
            gatewayName,
            callbackUrl,
            idempotencyKey,
        }: {
            orderId: string;
            gatewayName: string;
            callbackUrl: string;
            idempotencyKey: string;
        }) =>
            startPayment(
                {
                    orderId,
                    gatewayName,
                    callbackUrl,
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

export function useCompletePayment() {
    const queryClient =
        useQueryClient();

    
return useMutation({
    mutationFn: completePayment,

    onSuccess: async result => {
        await Promise.all([
            queryClient.invalidateQueries({
                queryKey: [
                    'order',
                    result.orderId,
                ],
            }),

            queryClient.invalidateQueries({
                queryKey: ['orders'],
            }),
        ]);
    },
});


}
