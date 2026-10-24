import { api } from '@/lib/api/client';

import type { OrderListDto } from '@/modules/orders/types';
import type {
    AdminCustomer,
    CustomerAddress,
} from '../types';

export const adminCustomersApi = {
    get: (id: string) =>
        api.get<AdminCustomer>(
            `/customers/${encodeURIComponent(id)}`,
        ),

    getOrders: (
        id: string,
        page = 1,
        pageSize = 20,
    ) =>
        api.get<OrderListDto>(
            `/customers/${encodeURIComponent(id)}/orders`,
            {
                params: {
                    page,
                    pageSize,
                },
            },
        ),

    getAddresses: (id: string) =>
        api.get<CustomerAddress[]>(
            `/customers/${encodeURIComponent(id)}/addresses`,
        ),
};