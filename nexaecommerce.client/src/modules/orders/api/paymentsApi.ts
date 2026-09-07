import api from '@/services/api';

import type {
    PaymentAttemptDto,
} from '../types';

export interface StartPaymentRequest {
    orderId: string;
    gatewayName: string;
    callbackUrl: string;
}

export interface CreatePaymentResultDto {
    paymentAttemptId: string;
    orderId: string;
    gatewayName: string;
    status: string;
    amount: number;
    currency: string;
    paymentUrl?: string | null;
    gatewayReference?: string | null;
}

export interface CreatePaymentAttemptRequest {
    orderId: string;
}

export interface CompletePaymentRequest {
    paymentAttemptId: string;
    gatewayName: string;
    gatewayReference: string;
}

export interface VerifyPaymentRequest {
    paymentAttemptId: string;
    gatewayReference: string;
}

export interface FailPaymentRequest {
    paymentAttemptId: string;
    failureCode?: string | null;
    failureMessage?: string | null;
}

function requireId(
    value: string,
    fieldName: string,
): string {
    const normalized =
        value.trim();

    if (!normalized) {
        throw new Error(
            `${fieldName} is required.`,
        );
    }

    return normalized;
}

function requireIdempotencyKey(
    value: string,
): string {
    const normalized =
        value.trim();

    if (!normalized) {
        throw new Error(
            'Idempotency key is required.',
        );
    }

    return normalized;
}

export async function startPayment(
    request: StartPaymentRequest,
    idempotencyKey: string,
): Promise<CreatePaymentResultDto> {
    const key =
        requireIdempotencyKey(
            idempotencyKey,
        );

    const { data } =
        await api.post<CreatePaymentResultDto>(
            '/orders/payment/start',
            request,
            {
                headers: {
                    'Idempotency-Key':
                        key,
                },
            },
        );

    return data;
}

export async function createPaymentAttempt(
    request: CreatePaymentAttemptRequest,
    idempotencyKey: string,
): Promise<PaymentAttemptDto> {
    const key =
        requireIdempotencyKey(
            idempotencyKey,
        );

    const { data } =
        await api.post<PaymentAttemptDto>(
            '/orders/payment-attempts',
            request,
            {
                headers: {
                    'Idempotency-Key':
                        key,
                },
            },
        );

    return data;
}

export async function getPaymentAttempt(
    id: string,
): Promise<PaymentAttemptDto> {
    const normalizedId =
        requireId(
            id,
            'Payment attempt id',
        );

    const { data } =
        await api.get<PaymentAttemptDto>(
            `/orders/payment-attempts/${normalizedId}`,
        );

    return data;
}

export async function verifyPayment(
    request: VerifyPaymentRequest,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.post<PaymentAttemptDto>(
            '/orders/payment/verify',
            request,
        );

    return data;
}

export async function completePayment(
    request: CompletePaymentRequest,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.post<PaymentAttemptDto>(
            '/orders/payment/complete',
            request,
        );

    return data;
}

export async function failPayment(
    request: FailPaymentRequest,
): Promise<unknown> {
    const { data } =
        await api.post(
            '/orders/payment/fail',
            request,
        );

    return data;
}

export async function retryPayment(
    orderId: string,
    idempotencyKey: string,
): Promise<PaymentAttemptDto> {
    const normalizedOrderId =
        requireId(
            orderId,
            'Order id',
        );

    const key =
        requireIdempotencyKey(
            idempotencyKey,
        );

    const { data } =
        await api.post<PaymentAttemptDto>(
            '/orders/payment/retry',
            {
                orderId:
                    normalizedOrderId,
            },
            {
                headers: {
                    'Idempotency-Key':
                        key,
                },
            },
        );

    return data;
}