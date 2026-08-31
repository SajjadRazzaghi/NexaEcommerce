import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    createCustomerAddress,
    deleteCustomerAddress,
    getCustomerAddresses,
    setDefaultCustomerAddress,
    updateCustomerAddress,
} from '../api/customerAddressApi';

import type {
    CreateAddressRequest,
    UpdateAddressRequest,
} from '../types';

export const customerAddressesQueryKey = [
    'customer',
    'addresses',
] as const;

export function useCustomerAddresses() {
    return useQuery({
        queryKey: customerAddressesQueryKey,
        queryFn: getCustomerAddresses,
    });
}

export function useCustomerAddressMutations() {
    const queryClient =
        useQueryClient();

    const refresh = async () => {
        await queryClient.invalidateQueries({
            queryKey:
                customerAddressesQueryKey,
        });
    };

    const create =
        useMutation({
            mutationFn:
                (
                    request: CreateAddressRequest,
                ) =>
                    createCustomerAddress(
                        request,
                    ),
            onSuccess: refresh,
        });

    const update =
        useMutation({
            mutationFn:
                ({
                    id,
                    request,
                }: {
                    id: string;
                    request: UpdateAddressRequest;
                }) =>
                    updateCustomerAddress(
                        id,
                        request,
                    ),
            onSuccess: refresh,
        });

    const remove =
        useMutation({
            mutationFn:
                deleteCustomerAddress,
            onSuccess: refresh,
        });

    const setDefault =
        useMutation({
            mutationFn:
                setDefaultCustomerAddress,
            onSuccess: refresh,
        });

    return {
        create,
        update,
        remove,
        setDefault,
    };
}