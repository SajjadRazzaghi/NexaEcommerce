import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    warehousesApi,
    type SaveWarehouseLocationRequest,
    type SaveWarehouseRequest,
    type UpdateWarehouseLocationRequest,
    type UpdateWarehouseRequest,
} from '@/lib/api/inventory';

export const warehouseQueryKeys = {
    all: ['inventory', 'warehouses'] as const,
    list: (includeInactive: boolean) =>
        ['inventory', 'warehouses', 'list', includeInactive] as const,
    locations: (warehouseId: string, includeInactive: boolean) =>
        ['inventory', 'warehouses', warehouseId, 'locations', includeInactive] as const,
};

export function useWarehouses(includeInactive = true) {
    return useQuery({
        queryKey: warehouseQueryKeys.list(includeInactive),
        queryFn: async () => (await warehousesApi.list(includeInactive)).items,
    });
}

export function useWarehouseLocations(
    warehouseId?: string,
    includeInactive = true,
) {
    return useQuery({
        queryKey: warehouseId
            ? warehouseQueryKeys.locations(warehouseId, includeInactive)
            : ['inventory', 'warehouses', 'locations', 'empty'],
        queryFn: async () => {
            if (!warehouseId) return [];
            return (await warehousesApi.locations(warehouseId, includeInactive)).items;
        },
        enabled: Boolean(warehouseId),
    });
}

function invalidateWarehouses(
    queryClient: ReturnType<typeof useQueryClient>,
) {
    return queryClient.invalidateQueries({
        queryKey: warehouseQueryKeys.all,
    });
}

export function useCreateWarehouse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (body: SaveWarehouseRequest) => warehousesApi.create(body),
        onSuccess: () => invalidateWarehouses(queryClient),
    });
}

export function useUpdateWarehouse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ id, body }: { id: string; body: UpdateWarehouseRequest }) =>
            warehousesApi.update(id, body),
        onSuccess: () => invalidateWarehouses(queryClient),
    });
}

export function useSetWarehouseStatus() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
            warehousesApi.setStatus(id, isActive),
        onSuccess: () => invalidateWarehouses(queryClient),
    });
}

export function useSetDefaultWarehouse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => warehousesApi.setDefault(id),
        onSuccess: () => invalidateWarehouses(queryClient),
    });
}

export function useCreateWarehouseLocation() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (body: SaveWarehouseLocationRequest) =>
            warehousesApi.createLocation(body),
        onSuccess: (_, body) =>
            queryClient.invalidateQueries({
                queryKey: ['inventory', 'warehouses', body.warehouseId, 'locations'],
            }),
    });
}

export function useUpdateWarehouseLocation() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({
            warehouseId,
            locationId,
            body,
        }: {
            warehouseId: string;
            locationId: string;
            body: UpdateWarehouseLocationRequest;
        }) => warehousesApi.updateLocation(warehouseId, locationId, body),
        onSuccess: (_, variables) =>
            queryClient.invalidateQueries({
                queryKey: [
                    'inventory',
                    'warehouses',
                    variables.warehouseId,
                    'locations',
                ],
            }),
    });
}

export function useSetWarehouseLocationStatus() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({
            warehouseId,
            locationId,
            isActive,
        }: {
            warehouseId: string;
            locationId: string;
            isActive: boolean;
        }) => warehousesApi.setLocationStatus(warehouseId, locationId, isActive),
        onSuccess: (_, variables) =>
            queryClient.invalidateQueries({
                queryKey: [
                    'inventory',
                    'warehouses',
                    variables.warehouseId,
                    'locations',
                ],
            }),
    });
}
