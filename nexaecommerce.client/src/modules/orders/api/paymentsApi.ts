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

export interface FailPaymentRequest {
    paymentAttemptId: string;
    failureCode?: string | null;
    failureMessage?: string | null;
}

export interface RetryPaymentResponse {
    id: string;
    orderId: string;
    status: string;
    amount: number;
    currency: string;
    gatewayName?: string | null;
    gatewayReference?: string | null;
    failureCode?: string | null;
    failureMessage?: string | null;
    createdAt: string;
    completedAt?: string | null;
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
                    'Idempotency-Key':
                        idempotencyKey,
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
            `/ api / orders / payment - attempts / ${ id } `,
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

export async function failPayment(
    request: FailPaymentRequest,
): Promise<unknown> {
    const { data } =
        await api.post(
            '/api/orders/payment/fail',
            request,
        );

    return data;
}

export async function retryPayment(
    orderId: string,
    idempotencyKey: string,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.post<PaymentAttemptDto>(
            '/api/orders/payment/retry',
            {
                orderId,
            },
            {
                headers: {
                    'Idempotency-Key':
                        idempotencyKey,
                },
            },
        );

    return data;
}

