import api from '@/services/api';

export type FulfillmentStatus =
    | 'Pending'
    | 'Picking'
    | 'Picked'
    | 'Packing'
    | 'Packed'
    | 'ReadyToShip';

export interface FulfillmentQueueDto {
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

export interface FulfillmentQueueItemDto {
    fulfillment: FulfillmentQueueDto;
    orderId: string;
    orderNumber: string;
    shippingFullName: string;
    shippingPhone: string;
    shippingAddress: string;
    shippingCity: string;
    shippingPostalCode?: string | null;
    totalAmount: number;
    currency: string;
    itemCount: number;
}

export interface FulfillmentQueueResponse {
    skip: number;
    take: number;
    items: FulfillmentQueueItemDto[];
}

export async function getFulfillmentQueue(
    skip = 0,
    take = 50,
): Promise<FulfillmentQueueResponse> {
    const { data } =
        await api.get<FulfillmentQueueResponse>(
            '/api/fulfillment/queue',
            {
                params: {
                    skip,
                    take,
                },
            },
        );

    return data;
}