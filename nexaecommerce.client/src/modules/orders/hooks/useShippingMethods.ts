import { useQuery } from '@tanstack/react-query';

import {
    getShippingMethods,
} from '../api/shippingMethodsApi';

export const shippingMethodsQueryKey =
    ['shipping-methods'] as const;

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

