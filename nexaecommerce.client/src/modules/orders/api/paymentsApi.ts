import { api } from '@/lib/api/client';

import type {
    PaymentAttemptDto,
} from '../types';

export interface StartPaymentRequest {
    orderId: string;
    gatewayName: string;
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

    return api.post<CreatePaymentResultDto>(
        '/orders/payment/start',
        request,
        {
            headers: {
                'Idempotency-Key':
                    key,
            },
        },
    );
}

export async function createPaymentAttempt(
    request: CreatePaymentAttemptRequest,
    idempotencyKey: string,
): Promise<PaymentAttemptDto> {
    const key =
        requireIdempotencyKey(
            idempotencyKey,
        );

    return api.post<PaymentAttemptDto>(
        '/orders/payment-attempts',
        request,
        {
            headers: {
                'Idempotency-Key':
                    key,
            },
        },
    );
}

export async function getPaymentAttempt(
    id: string,
): Promise<PaymentAttemptDto> {
    const normalizedId =
        requireId(
            id,
            'Payment attempt id',
        );

    return api.get<PaymentAttemptDto>(
        `/orders/payment-attempts/${normalizedId}`,
    );
}

export async function verifyPayment(
    request: VerifyPaymentRequest,
): Promise<PaymentAttemptDto> {
    return api.post<PaymentAttemptDto>(
        '/orders/payment/verify',
        request,
    );
}

export async function completePayment(
    request: CompletePaymentRequest,
): Promise<PaymentAttemptDto> {
    return api.post<PaymentAttemptDto>(
        '/orders/payment/complete',
        request,
    );
}

export async function failPayment(
    request: FailPaymentRequest,
): Promise<unknown> {
    return api.post(
        '/orders/payment/fail',
        request,
    );
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

    return api.post<PaymentAttemptDto>(
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
}