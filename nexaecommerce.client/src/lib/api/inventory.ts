import { api } from './client';

export const INVENTORY_PERM = {
    read: 'inventory.read',
    manage: 'inventory.manage',
} as const;
export interface WarehouseStockReservation {
    id: string;

    warehouseId: string;
    locationId: string;
    productVariantId: string;

    orderId: string;

    quantity: number;

    status: string;

    expiresAt: string;
    createdAt: string;

    releasedAt: string | null;
    committedAt: string | null;
}
export const warehouseStockReservationApi = {
    reserve: (
        body: ReserveWarehouseStockRequest,
    ) =>
        api.post<WarehouseStockReservation>(
            '/inventory/reservations',
            body,
        ),

    release: (
        reservationId: string,
    ) =>
        api.post<WarehouseStockReservation>(
            `/inventory/reservations/${reservationId}/release`,
        ),

    commit: (
        reservationId: string,
    ) =>
        api.post<WarehouseStockReservation>(
            `/inventory/reservations/${reservationId}/commit`,
        ),
};
export interface ReserveWarehouseStockRequest {
    warehouseId: string;
    locationId: string;
    productVariantId: string;
    orderId: string;
    quantity: number;
    durationMinutes?: number;
}
export interface Warehouse {
    id: string;
    code: string;
    name: string;
    addressLine: string | null;
    city: string | null;
    postalCode: string | null;
    phone: string | null;
    isDefault: boolean;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export interface WarehouseLocation {
    id: string;
    warehouseId: string;
    code: string;
    name: string;
    zone: string | null;
    rack: string | null;
    shelf: string | null;
    bin: string | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export interface WarehouseStock {
    id: string;
    warehouseId: string;
    locationId: string;
    productVariantId: string;
    onHandQuantity: number;
    reservedQuantity: number;
    availableQuantity: number;
    incomingQuantity: number;
    damagedQuantity: number;
    reorderPoint: number;
    isLowStock: boolean;
    version: number;
    createdAt: string;
    updatedAt: string | null;
}

export interface WarehouseStockMovement {
    id: string;
    warehouseId: string;
    locationId: string;
    productVariantId: string;
    type: string;
    quantityDelta: number;
    balanceAfter: number;
    referenceType: string | null;
    referenceId: string | null;
    reason: string | null;
    occurredAt: string;
}

export interface WarehouseTransfer {
    id: string;
    sourceWarehouseId: string;
    sourceLocationId: string;
    destinationWarehouseId: string;
    destinationLocationId: string;
    productVariantId: string;
    quantity: number;
    status: string;
    reason: string | null;
    requestedAt: string;
    completedAt: string | null;
}

export interface WarehouseListResponse {
    items: Warehouse[];
}

export interface WarehouseLocationsResponse {
    items: WarehouseLocation[];
}

export interface WarehouseStockListResponse {
    items: WarehouseStock[];
}

export interface WarehouseStockMovementListResponse {
    warehouseId: string;
    locationId: string;
    productVariantId: string;
    skip: number;
    take: number;
    items: WarehouseStockMovement[];
}

export interface WarehouseTransferListResponse {
    skip: number;
    take: number;
    items: WarehouseTransfer[];
}

export interface SaveWarehouseRequest {
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

export interface SaveWarehouseLocationRequest {
    warehouseId: string;
    code: string;
    name: string;
    zone?: string | null;
    rack?: string | null;
    shelf?: string | null;
    bin?: string | null;
}

export interface UpdateWarehouseLocationRequest {
    code: string;
    name: string;
    zone?: string | null;
    rack?: string | null;
    shelf?: string | null;
    bin?: string | null;
}

export interface SetWarehouseStockRequest {
    warehouseId: string;
    locationId: string;
    productVariantId: string;
    onHandQuantity: number;
    reservedQuantity: number;
    incomingQuantity: number;
    damagedQuantity: number;
    reorderPoint: number;
}

export interface CreateWarehouseTransferRequest {
    sourceWarehouseId: string;
    sourceLocationId: string;
    destinationWarehouseId: string;
    destinationLocationId: string;
    productVariantId: string;
    quantity: number;
    reason?: string | null;
}

export const warehousesApi = {
    list: (
        includeInactive = true,
    ) =>
        api.get<WarehouseListResponse>(
            '/inventory/warehouses/',
            {
                params: {
                    includeInactive,
                },
            },
        ),

    create: (
        body: SaveWarehouseRequest,
    ) =>
        api.post<Warehouse>(
            '/inventory/warehouses/',
            body,
        ),

    update: (
        id: string,
        body: UpdateWarehouseRequest,
    ) =>
        api.put<Warehouse>(
            `/inventory/warehouses/${id}`,
            body,
        ),

    setStatus: (
        id: string,
        isActive: boolean,
    ) =>
        api.put<Warehouse>(
            `/inventory/warehouses/${id}/status`,
            {
                isActive,
            },
        ),

    setDefault: (
        id: string,
    ) =>
        api.post<Warehouse>(
            `/inventory/warehouses/${id}/set-default`,
        ),

    locations: (
        warehouseId: string,
        includeInactive = true,
    ) =>
        api.get<WarehouseLocationsResponse>(
            `/inventory/warehouses/${warehouseId}/locations`,
            {
                params: {
                    includeInactive,
                },
            },
        ),

    createLocation: (
        body: SaveWarehouseLocationRequest,
    ) =>
        api.post<WarehouseLocation>(
            '/inventory/warehouses/locations',
            body,
        ),

    updateLocation: (
        warehouseId: string,
        locationId: string,
        body: UpdateWarehouseLocationRequest,
    ) =>
        api.put<WarehouseLocation>(
            `/inventory/warehouses/${warehouseId}/locations/${locationId}`,
            body,
        ),

    setLocationStatus: (
        warehouseId: string,
        locationId: string,
        isActive: boolean,
    ) =>
        api.put<WarehouseLocation>(
            `/inventory/warehouses/${warehouseId}/locations/${locationId}/status`,
            {
                isActive,
            },
        ),
};
export const warehouseStockApi = {
    listByWarehouse: (
        warehouseId: string,
        productVariantId?: string,
        includeZeroStock = true,
    ) =>
        api.get<WarehouseStockListResponse>(
            '/inventory/warehouse-stock/',
            {
                params: {
                    warehouseId,
                    productVariantId,
                    includeZeroStock,
                },
            },
        ),

    set: (
        body: SetWarehouseStockRequest,
    ) =>
        api.put<WarehouseStock>(
            '/inventory/warehouse-stock/',
            body,
        ),

    movements: (
        warehouseId: string,
        locationId: string,
        productVariantId: string,
        skip = 0,
        take = 20,
    ) =>
        api.get<WarehouseStockMovementListResponse>(
            `/inventory/transfers/movements/${warehouseId}/${locationId}/${productVariantId}`,
            {
                params: {
                    skip,
                    take,
                },
            },
        ),
};

export const warehouseTransferApi = {
    list: (
        skip = 0,
        take = 20,
    ) =>
        api.get<WarehouseTransferListResponse>(
            '/inventory/transfers/',
            {
                params: {
                    skip,
                    take,
                },
            },
        ),

    create: (
        body: CreateWarehouseTransferRequest,
    ) =>
        api.post<WarehouseTransfer>(
            '/inventory/transfers/',
            body,
        ),
};