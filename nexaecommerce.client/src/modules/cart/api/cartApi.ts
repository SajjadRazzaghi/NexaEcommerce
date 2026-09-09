import api from '@/services/api';

import type {
    AddCartItemRequest,
    CartResponse,
    SetCartItemQuantityRequest,
} from '../types';

export async function getCart(): Promise<CartResponse> {
    const { data } =
        await api.get<CartResponse>(
            '/cart',
        );

    return data;
}

export async function addCartItem(
    request: AddCartItemRequest,
): Promise<CartResponse> {
    const { data } =
        await api.post<CartResponse>(
            '/cart/items',
            request,
        );

    return data;
}

export async function setCartItemQuantity(
    request: SetCartItemQuantityRequest,
): Promise<CartResponse> {
    const { data } =
        await api.put<CartResponse>(
            '/cart/items',
            request,
        );

    return data;
}

export async function removeCartItem(
    productVariantId: string,
): Promise<CartResponse> {
    const { data } =
        await api.delete<CartResponse>(
            `/cart/items/${productVariantId}`,
        );

    return data;
}

export async function clearCart(): Promise<CartResponse> {
    const { data } =
        await api.delete<CartResponse>(
            '/cart',
        );

    return data;
}