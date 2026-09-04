import api from '@/services/api';

export interface ShippingMethodDto {
    id: string;
    code: string;
    name: string;
    carrier: string;
    price: number;
    sortOrder: number;
    isActive: boolean;
}

export async function getShippingMethods(): Promise<
    ShippingMethodDto[]
> {
    const { data } =
        await api.get<ShippingMethodDto[]>(
            '/api/shipping-methods',
        );

    return data;
}