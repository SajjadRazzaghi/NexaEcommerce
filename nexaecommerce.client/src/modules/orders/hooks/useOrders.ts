import { useQuery } from '@tanstack/react-query';

import {
    getMyOrders,
    getOrder,
} from '../api/ordersApi';

export function useOrders(
    page = 1,
    pageSize = 20,
    status?: string,
) {
    return useQuery({
        queryKey: [
            'orders',
            {
                page,
                pageSize,
                status,
            },
        ],
        queryFn: () =>
            getMyOrders(
                page,
                pageSize,
                status,
            ),
    });
}

export function useOrder(id?: string) {
    return useQuery({
        queryKey: ['order', id],
        queryFn: () => getOrder(id!),
        enabled: Boolean(id),
    });
}