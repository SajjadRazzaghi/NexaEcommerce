import { api } from '@/lib/api/client';

import type {
    OrderDto,
    OrderListDto,
} from '../types';

export async function getMyOrders(
    page = 1,
    pageSize = 20,
    status?: string,
): Promise<OrderListDto> {
    return api.get<OrderListDto>(
        '/orders',
        {
            params: {
                page,
                pageSize,
                status,
            },
        },
    );
}

export async function getOrder(
    id: string,
): Promise<OrderDto> {
    const normalizedId =
        id.trim();

    if (!normalizedId) {
        throw new Error(
            'Order id is required.',
        );
    }

    return api.get<OrderDto>(
        `/orders/${normalizedId}`,
    );
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
        taxRateId?: string | null;
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

    return api.post<OrderDto>(
        '/orders/checkout',
        request,
        {
            headers: {
                'Idempotency-Key':
                    normalizedKey,
            },
        },
    );
}