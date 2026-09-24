import api from '@/services/api';

import type {
    CheckoutRequest,
    OrderDto,
} from '@/modules/orders/types';

export async function checkout(
    request: CheckoutRequest,
    idempotencyKey: string,
): Promise<OrderDto> {
    const normalizedKey =
        idempotencyKey.trim();

    if (!normalizedKey) {
        throw new Error(
            'Idempotency key is required.',
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