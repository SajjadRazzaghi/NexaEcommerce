import api from '@/services/api';

export type FulfillmentStatus =
    | 'Pending'
    | 'Picking'
    | 'Picked'
    | 'Packing'
    | 'Packed'
    | 'ReadyToShip'
    | 'Shipped'
    | 'Delivered'
    | 'Cancelled';

export interface FulfillmentDto {
    id: string;
    orderId: string;
    status: FulfillmentStatus;
    warehouseId?: string | null;
    pickingLocationId?: string | null;
    pickingStartedAt?: string | null;
    pickedAt?: string | null;
    packingStartedAt?: string | null;
    packedAt?: string | null;
    readyToShipAt?: string | null;
    shippedAt?: string | null;
    deliveredAt?: string | null;
    createdAt: string;
    updatedAt?: string | null;
}

export async function getFulfillment(
    orderId: string,
): Promise<FulfillmentDto | null> {
    const normalizedId =
        orderId.trim();

    if (!normalizedId) {
        throw new Error(
            'Order id is required.',
        );
    }

    try {
        const { data } =
            await api.get<FulfillmentDto>(
                `/fulfillment/orders/${normalizedId}`,
            );

        return data;
    } catch (error) {
        const status =
            (
                error as {
                    response?: {
                        status?: number;
                    };
                }
            ).response?.status;

        if (status === 404) {
            return null;
        }

        throw error;
    }
}

export async function startFulfillment(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/start`,
        );

    return data;
}

export async function allocateWarehouse(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/allocate`,
        );

    return data;
}

export async function reserveStock(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/reserve-stock`,
        );

    return data;
}

export async function startPicking(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/start-picking`,
        );

    return data;
}

export async function markPicked(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/picked`,
        );

    return data;
}

export async function startPacking(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/start-packing`,
        );

    return data;
}

export async function markPacked(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/packed`,
        );

    return data;
}

export async function markReadyToShip(
    orderId: string,
): Promise<unknown> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post(
            `/fulfillment/orders/${normalizedId}/ready-to-ship`,
        );

    return data;
}

export async function rollbackFulfillment(
    orderId: string,
): Promise<FulfillmentDto> {
    const normalizedId =
        orderId.trim();

    if (!normalizedId) {
        throw new Error(
            'Order id is required.',
        );
    }

    const { data } =
        await api.post<FulfillmentDto>(
            `/fulfillment/orders/${normalizedId}/rollback`,
        );

    return data;
}