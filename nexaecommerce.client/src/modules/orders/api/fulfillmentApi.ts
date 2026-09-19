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
    try {
        const { data } =
            await api.get<FulfillmentDto>(
                `/api/fulfillment/orders/${orderId}`,
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
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/start`,
            );

        
return data;


    }

    export async function allocateWarehouse(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/allocate`,
            );

        
return data;


    }

    export async function reserveStock(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/reserve-stock`,
            );

        
return data;


    }

    export async function startPicking(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/start-picking`,
            );

        
return data;


    }

    export async function markPicked(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/picked`,
            );

        
return data;


    }

    export async function startPacking(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/start-packing`,
            );

        
return data;


    }

    export async function markPacked(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/packed`,
            );

        
return data;


    }

    export async function markReadyToShip(
        orderId: string,
    ): Promise<unknown> {
        const { data } =
            await api.post(
                `/api/fulfillment/orders/${orderId}/ready-to-ship`,
            );

        
return data;


    }
