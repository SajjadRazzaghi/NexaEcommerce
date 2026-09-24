import {
    useQuery,
} from '@tanstack/react-query';

import {
    getAdminShippingMethods,
    getShippingMethods,
} from '../api/shippingMethodsApi';

export const shippingMethodsQueryKey =
    ['shipping-methods'] as const;

export const adminShippingMethodsQueryKey =
    ['admin', 'shipping-methods'] as const;

export function useShippingMethods() {
    return useQuery({
        queryKey:
            shippingMethodsQueryKey,

        queryFn:
            getShippingMethods,

        staleTime:
            5 * 60 * 1000,
    });
}

export function useAdminShippingMethods() {
    return useQuery({
        queryKey:
            adminShippingMethodsQueryKey,

        queryFn:
            getAdminShippingMethods,

        staleTime:
            30 * 1000,
    });
}