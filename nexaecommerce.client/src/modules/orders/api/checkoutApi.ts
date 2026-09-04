import api from '@/services/api';

import type {
    CheckoutRequest,
    OrderDto,
} from '../types';

export async function createCheckout(
    request: CheckoutRequest,
    idempotencyKey: string,
): Promise<OrderDto> {
    if (!idempotencyKey.trim()) {
        throw new Error(
            'Checkout idempotency key is required.',
        );
    }

    const { data } =
        await api.post<OrderDto>(
            '/api/orders/checkout',
            request,
            {
                headers: {
                    'Idempotency-Key':
                        idempotencyKey,
                },
            },
        );

    return data;
}

