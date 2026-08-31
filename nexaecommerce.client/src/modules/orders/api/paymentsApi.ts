import api from '@/services/api';

import type {
    PaymentAttemptDto,
} from '../types';

export interface CreatePaymentAttemptRequest {
    orderId: string;
}

export interface CompletePaymentRequest {
    paymentAttemptId: string;
    gatewayName: string;
    gatewayReference: string;
}

export async function createPaymentAttempt(
    request: CreatePaymentAttemptRequest,
    idempotencyKey: string,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.post<PaymentAttemptDto>(
            '/api/orders/payment-attempts',
            request,
            {
                headers: {
                    'Idempotency-Key': idempotencyKey,
                },
            },
        );

    return data;
}

export async function getPaymentAttempt(
    id: string,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.get<PaymentAttemptDto>(
            `/api/orders/payment-attempts/${id}`,
        );

    return data;
}

export async function completePayment(
    request: CompletePaymentRequest,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.post<PaymentAttemptDto>(
            '/api/orders/payment/complete',
            request,
        );

    return data;
}