import api from '@/services/api';

import type {
    ShipmentDto,
} from '../types';

export async function getShipment(
    orderId: string,
): Promise<ShipmentDto | null> {
    const normalizedId =
        orderId.trim();

    if (!normalizedId) {
        throw new Error(
            'Order id is required.',
        );
    }

    try {
        const { data } =
            await api.get<ShipmentDto>(
                `/orders/${normalizedId}/shipment`,
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

export async function getAdminShipment(
    orderId: string,
): Promise<ShipmentDto | null> {
    const normalizedId =
        orderId.trim();

    if (!normalizedId) {
        throw new Error(
            'Order id is required.',
        );
    }

    try {
        const { data } =
            await api.get<ShipmentDto>(
                `/orders/admin/${normalizedId}/shipment`,
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
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post<ShipmentDto>(
            `/orders/${normalizedId}/shipment`,
            {
                orderId:
                    normalizedId,
                shippingMethod,
                carrier,
                trackingNumber:
                    trackingNumber?.trim() ||
                    null,
            },
        );

    return data;
}

export async function updateShipmentTrackingNumber(
    orderId: string,
    trackingNumber: string,
): Promise<ShipmentDto> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.put<ShipmentDto>(
            `/orders/${normalizedId}/shipment/tracking`,
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
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post<ShipmentDto>(
            `/orders/${normalizedId}/shipment/ship`,
        );

    return data;
}

export async function deliverOrder(
    orderId: string,
): Promise<ShipmentDto> {
    const normalizedId =
        orderId.trim();

    const { data } =
        await api.post<ShipmentDto>(
            `/orders/${normalizedId}/shipment/deliver`,
        );

    return data;
}