import api from '@/services/api';

export interface ShippingMethod {
    id: string;
    code: string;
    name: string;
    carrier: string;
    price: number;
    sortOrder: number;
    isActive: boolean;
}

export interface ShippingQuote {
    shippingMethodId: string;
    code: string;
    name: string;
    carrier: string;
    price: number;
}

export interface CreateShippingMethodRequest {
    code: string;
    name: string;
    carrier: string;
    price: number;
    sortOrder?: number;
}

export interface UpdateShippingMethodRequest {
    name: string;
    carrier: string;
    price: number;
    sortOrder: number;
}

export async function getShippingMethods(): Promise<
    ShippingMethod[]
> {
    const { data } =
        await api.get<ShippingMethod[]>(
            '/shipping-methods',
        );

    return data;
}

export async function getAdminShippingMethods(): Promise<
    ShippingMethod[]
> {
    const { data } =
        await api.get<ShippingMethod[]>(
            '/shipping-methods/admin',
        );

    return data;
}

export async function createShippingMethod(
    request: CreateShippingMethodRequest,
): Promise<ShippingMethod> {
    const { data } =
        await api.post<ShippingMethod>(
            '/shipping-methods',
            request,
        );

    return data;
}

export async function updateShippingMethod(
    id: string,
    request: UpdateShippingMethodRequest,
): Promise<ShippingMethod> {
    const normalizedId =
        id.trim();

    if (!normalizedId) {
        throw new Error(
            'Shipping method id is required.',
        );
    }

    const { data } =
        await api.put<ShippingMethod>(
            `/shipping-methods/${normalizedId}`,
            request,
        );

    return data;
}

export async function setShippingMethodActive(
    id: string,
    active: boolean,
): Promise<void> {
    const normalizedId =
        id.trim();

    if (!normalizedId) {
        throw new Error(
            'Shipping method id is required.',
        );
    }

    await api.put(
        `/shipping-methods/${normalizedId}/active`,
        {
            active,
        },
    );
}

export async function deleteShippingMethod(
    id: string,
): Promise<void> {
    const normalizedId =
        id.trim();

    if (!normalizedId) {
        throw new Error(
            'Shipping method id is required.',
        );
    }

    await api.delete(
        `/shipping-methods/${normalizedId}`,
    );
}

export async function getShippingQuote(
    id: string,
): Promise<ShippingQuote> {
    const normalizedId =
        id.trim();

    if (!normalizedId) {
        throw new Error(
            'Shipping method id is required.',
        );
    }

    const { data } =
        await api.get<ShippingQuote>(
            `/shipping-methods/${normalizedId}/quote`,
        );

    return data;
}