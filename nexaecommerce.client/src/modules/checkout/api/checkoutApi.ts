import api from '@/services/api';
import type {
    CheckoutRequest,
    OrderDto,
} from '@/modules/orders/types';

export async function checkout(
    request: CheckoutRequest,
    idempotencyKey: string,
): Promise<OrderDto> {
    const { data } = await api.post<OrderDto>(
        '/api/orders/checkout',
        request,
        {
            headers: {
                'Idempotency-Key': idempotencyKey,
            },
        },
    );

    return data;
}
