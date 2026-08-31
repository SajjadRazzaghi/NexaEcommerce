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
    const { data } = await api.get<OrderListDto>(
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
    const { data } = await api.get<OrderDto>(
        `/api/orders/${id}`,
    );

    return data;
}