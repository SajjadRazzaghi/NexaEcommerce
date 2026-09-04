import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    getAdminOrder,
    getAdminOrders,
    updateOrderStatus,
} from '../api/adminOrdersApi';

import type {
    AdminOrdersQuery,
    UpdateOrderStatusRequest,
} from '../api/adminOrdersApi';

export const adminOrdersQueryKey = (
    query: AdminOrdersQuery,
) => [
    'admin',
    'orders',
    {
        page:
            query.page ?? 1,
        pageSize:
            query.pageSize ?? 20,
        status:
            query.status ?? '',
        search:
            query.search ?? '',
    },
] as const;

export function useAdminOrders(
    query: AdminOrdersQuery,
) {
    return useQuery({
        queryKey:
            adminOrdersQueryKey(
                query,
            ),
        queryFn: () =>
            getAdminOrders(
                query,
            ),
        placeholderData:
            previous =>
                previous,
    });
}

export function useAdminOrder(
    id?: string,
) {
    return useQuery({
        queryKey: [
            'admin',
            'orders',
            id,
        ],
        queryFn: () =>
            getAdminOrder(
                id!,
            ),
        enabled:
            Boolean(id),
    });
}

export function useAdminOrderMutations() {
    const queryClient =
        useQueryClient();

    const updateStatus =
        useMutation({
            mutationFn: ({
                id,
                status,
            }: {
                id: string;
                status: NonNullable<
                    UpdateOrderStatusRequest['status']
                >;
            }) =>
                updateOrderStatus(
                    id,
                    {
                        status,
                    },
                ),
            onSuccess:
                async result => {
                    await Promise.all([
                        queryClient.invalidateQueries({
                            queryKey: [
                                'admin',
                                'orders',
                            ],
                        }),
                        queryClient.invalidateQueries({
                            queryKey: [
                                'admin',
                                'orders',
                                result.orderId,
                            ],
                        }),
                        queryClient.invalidateQueries({
                            queryKey: [
                                'order',
                                result.orderId,
                            ],
                        }),
                    ]);
                },
        });

    return {
        updateStatus,
    };
}