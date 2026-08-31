import {
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import {
    addCartItem,
    clearCart,
    removeCartItem,
    setCartItemQuantity,
} from '../api/cartApi';

import { cartQueryKey } from './useCart';

export function useCartMutations() {
    const queryClient = useQueryClient();

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey: cartQueryKey,
        });
    };

    const add = useMutation({
        mutationFn: addCartItem,
        onSuccess: refresh,
    });

    const setQuantity = useMutation({
        mutationFn: setCartItemQuantity,
        onSuccess: refresh,
    });

    const remove = useMutation({
        mutationFn: removeCartItem,
        onSuccess: refresh,
    });

    const clear = useMutation({
        mutationFn: clearCart,
        onSuccess: refresh,
    });

    return {
        add,
        setQuantity,
        remove,
        clear,
    };
}