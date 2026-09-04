import api from '@/services/api';

import type {
    CheckoutRequest,
    OrderDto,
    OrderListDto,
} from '../types';

export async function getMyOrders(
    page = 1,
    pageSize = 20,
    status?: string,
): Promise<OrderListDto> {
    const { data } =
        await api.get<OrderListDto>(
            '/api/orders',
            {
                params: {
                    page,
                    pageSize,
                    status,
                },
            },
        );

    return data;
}

export async function getOrder(
    id: string,
): Promise<OrderDto> {
    const { data } =
        await api.get<OrderDto>(
            `/ api / orders / ${ id } `,
        );

    return data;
}

export async function createCheckout(
    request: CheckoutRequest,
    idempotencyKey: string,
): Promise<OrderDto> {
    if (!idempotencyKey.trim()) {
        throw new Error(
            'Idempotency key is required.',
        );
    }

    const { data } =
        await api.post<OrderDto>(
            '/api/orders/checkout',
            request,
            {
                headers: {
                    'Idempotency-Key':
                        idempotencyKey.trim(),
                },
            },
        );

    return data;
}
