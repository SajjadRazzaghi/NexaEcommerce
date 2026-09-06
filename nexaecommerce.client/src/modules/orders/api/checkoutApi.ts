
import api from '@/services/api';

import type {
    CheckoutRequest,
    OrderDto,
} from '../types';


export async function createCheckout(
    request: CheckoutRequest,
    idempotencyKey: string,
): Promise<OrderDto> {
    const normalizedKey =
        idempotencyKey.trim();

    if (!normalizedKey) {
        throw new Error(
            'Checkout idempotency key is required.',
        );
    }

    const { data } =
        await api.post<OrderDto>(
            '/orders/checkout',
            request,
            {
                headers: {
                    'Idempotency-Key':
                        normalizedKey,
                },
            },
        );

    return data;
}

