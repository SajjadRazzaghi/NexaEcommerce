import { api } from '@/lib/api/client';

export interface WarehouseDto {
    id: string;
    code: string;
    name: string;
    addressLine?: string | null;
    city?: string | null;
    postalCode?: string | null;
    phone?: string | null;
    isDefault: boolean;
    isActive: boolean;
    createdAt: string;
    updatedAt?: string | null;
}

export interface WarehouseListResponse {
    items: WarehouseDto[];
}

export interface CreateWarehouseRequest {
    code: string;
    name: string;
    addressLine?: string | null;
    city?: string | null;
    postalCode?: string | null;
    phone?: string | null;
    isDefault?: boolean;
}

export interface UpdateWarehouseRequest {
    code: string;
    name: string;
    addressLine?: string | null;
    city?: string | null;
    postalCode?: string | null;
    phone?: string | null;
}

export interface SetWarehouseStatusRequest {
    isActive: boolean;
}

export const warehousesApi = {
    list: (
        includeInactive = true,
        signal?: AbortSignal,
    ) =>
        api.get<WarehouseListResponse>(
            '/inventory/warehouses/',
            {
                params: {
                    includeInactive,
                },
                signal,
            },
        ),

    create: (
        body: CreateWarehouseRequest,
    ) =>
        api.post<WarehouseDto>(
            '/inventory/warehouses/',
            body,
        ),

    update: (
        id: string,
        body: UpdateWarehouseRequest,
    ) =>
        api.put<WarehouseDto>(
            `/inventory/warehouses/${id}`,
            body,
        ),

    setStatus: (
        id: string,
        isActive: boolean,
    ) =>
        api.put<WarehouseDto>(
            `/inventory/warehouses/${id}/status`,
            {
                isActive,
            } satisfies SetWarehouseStatusRequest,
        ),

    setDefault: (
        id: string,
    ) =>
        api.post<WarehouseDto>(
            `/inventory/warehouses/${id}/set-default`,
        ),
};