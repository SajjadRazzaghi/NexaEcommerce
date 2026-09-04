import api from '@/services/api';

import type {
    OrderDto,
    OrderListDto,
    OrderStatus,
} from '../types';

export interface AdminOrdersQuery {
    page?: number;
    pageSize?: number;
    status?: OrderStatus | '';
    search?: string;
}

export interface UpdateOrderStatusRequest {
    status: OrderStatus;
}

export interface UpdateOrderStatusResponse {
    orderId: string;
    orderNumber: string;
    previousStatus: string;
    currentStatus: string;
}

export async function getAdminOrders(
    query: AdminOrdersQuery = {},
): Promise<OrderListDto> {
    const {
        page = 1,
        pageSize = 20,
        status = '',
        search = '',
    } = query;

    const { data } =
        await api.get<OrderListDto>(
            '/api/orders/admin',
            {
                params: {
                    page,
                    pageSize,
                    status:
                        status || undefined,
                    search:
                        search.trim() ||
                        undefined,
                },
            },
        );

    return data;
}

export async function getAdminOrder(
    id: string,
): Promise<OrderDto> {
    const { data } =
        await api.get<OrderDto>(
            `/api/orders/${id}`,
        );

    return data;
}

export async function updateOrderStatus(
    id: string,
    request: UpdateOrderStatusRequest,
): Promise<UpdateOrderStatusResponse> {
    const { data } =
        await api.put<UpdateOrderStatusResponse>(
            `/api/orders/${id}/status`,
            request,
        );

    return data;
}