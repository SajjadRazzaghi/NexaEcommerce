import { useQuery } from '@tanstack/react-query';
import { getCart } from '../api/cartApi';

export const cartQueryKey = ['cart'];

export function useCart() {
    return useQuery({
        queryKey: cartQueryKey,
        queryFn: getCart,
        staleTime: 30_000,
    });
}