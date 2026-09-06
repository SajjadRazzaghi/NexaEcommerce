import api from '@/services/api';

import type {
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
            '/orders',
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
    if (!id.trim()) {
        throw new Error(
            'Order id is required.',
        );
    }

    const { data } =
        await api.get<OrderDto>(
            `/ orders / ${ id.trim() } `,
        );

    return data;
}


export async function createCheckout(
    request: {
        items: Array<{
            productVariantId: string;
            quantity: number;
        }>;
        shippingFullName: string;
        shippingPhone: string;
        shippingAddress: string;
        shippingCity: string;
        shippingPostalCode?: string | null;
        shippingMethodId: string;
        couponCode?: string | null;
    },
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

