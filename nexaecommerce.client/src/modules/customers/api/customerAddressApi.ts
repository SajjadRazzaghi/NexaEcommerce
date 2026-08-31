import api from '@/services/api';

import type {
    CreateAddressRequest,
    CustomerAddress,
    UpdateAddressRequest,
} from '../types';

const BASE_PATH = '/customer/addresses';

export async function getCustomerAddresses(): Promise<
    CustomerAddress[]
> {
    const { data } = await api.get<CustomerAddress[]>(
        BASE_PATH,
    );

    return data;
}

export async function getCustomerAddress(
    id: string,
): Promise<CustomerAddress> {
    const { data } = await api.get<CustomerAddress>(
        `${BASE_PATH}/${id}`,
    );

    return data;
}

export async function createCustomerAddress(
    request: CreateAddressRequest,
): Promise<CustomerAddress> {
    const { data } = await api.post<CustomerAddress>(
        BASE_PATH,
        request,
    );

    return data;
}

export async function updateCustomerAddress(
    id: string,
    request: UpdateAddressRequest,
): Promise<CustomerAddress> {
    const { data } = await api.put<CustomerAddress>(
        `${BASE_PATH}/${id}`,
        request,
    );

    return data;
}

export async function deleteCustomerAddress(
    id: string,
): Promise<void> {
    await api.delete(
        `${BASE_PATH}/${id}`,
    );
}

export async function setDefaultCustomerAddress(
    id: string,
): Promise<CustomerAddress> {
    const { data } = await api.post<CustomerAddress>(
        `${BASE_PATH}/${id}/default`,
    );

    return data;
}