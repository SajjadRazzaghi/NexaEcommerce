import api from '@/services/api';

import type {
    ShipmentDto,
} from '../types';

export async function getShipment(
    orderId: string,
): Promise<ShipmentDto | null> {
    try {
        const { data } =
            await api.get<ShipmentDto>(
                `/api/orders/${orderId}/shipment`,
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

export async function createShipment(
        orderId: string,
        shippingMethod: string,
        carrier: string,
        trackingNumber?: string | null,
    ): Promise<ShipmentDto> {
        const { data } =
            await api.post<ShipmentDto>(
                `/api/orders/${orderId}/shipment`,
                {
                    orderId,
                    shippingMethod,
                    carrier,
                    trackingNumber:
                        trackingNumber?.trim() || null,
                },
            );

        
return data;


    }

    export async function updateShipmentTrackingNumber(
        orderId: string,
        trackingNumber: string,
    ): Promise<ShipmentDto> {
        const { data } =
            await api.put<ShipmentDto>(
                `/api/orders/${orderId}/shipment/tracking`,
                {
                    trackingNumber:
                        trackingNumber.trim(),
                },
            );

        
return data;


    }

    export async function shipOrder(
        orderId: string,
    ): Promise<ShipmentDto> {
        const { data } =
            await api.post<ShipmentDto>(
                `/api/orders/${orderId}/shipment/ship`,
            );

        
return data;


    }

    export async function deliverOrder(
        orderId: string,
    ): Promise<ShipmentDto> {
        const { data } =
            await api.post<ShipmentDto>(
                `/api/orders/${orderId}/shipment/deliver`,
            );

        
return data;


    }
