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
            '/api/shipping-methods',
        );

    return data;
}

export async function getAdminShippingMethods(): Promise<
    ShippingMethod[]
> {
    const { data } =
        await api.get<ShippingMethod[]>(
            '/api/shipping-methods/admin',
        );

    return data;
}

export async function createShippingMethod(
    request: CreateShippingMethodRequest,
): Promise<ShippingMethod> {
    const { data } =
        await api.post<ShippingMethod>(
            '/api/shipping-methods',
            request,
        );

    return data;
}

export async function updateShippingMethod(
    id: string,
    request: UpdateShippingMethodRequest,
): Promise<ShippingMethod> {
    const { data } =
        await api.put<ShippingMethod>(
            `/ api / shipping - methods / ${ id } `,
            request,
        );

    return data;
}

export async function setShippingMethodActive(
    id: string,
    active: boolean,
): Promise<void> {
    await api.put(
        `/ api / shipping - methods / ${ id }/active`,
{
    active,
        },
    );
}

export async function deleteShippingMethod(
    id: string,
): Promise<void> {
    await api.delete(
        `/api/shipping-methods/${id}`,
    );
}

export async function getShippingQuote(
    id: string,
): Promise<ShippingQuote> {
    const { data } =
        await api.get<ShippingQuote>(
            `/api/shipping-methods/${id}/quote`,
        );

    return data;
}

