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

export async function startPayment(
    request: StartPaymentRequest,
    idempotencyKey: string,
): Promise<CreatePaymentResultDto> {
    const { data } =
        await api.post<CreatePaymentResultDto>(
            '/api/orders/payment/start',
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
            `/api/orders/payment-attempts/${id}`,
        );

    
return data;


}

export async function verifyPayment(
    request: VerifyPaymentRequest,
): Promise<PaymentAttemptDto> {
    const { data } =
        await api.post<PaymentAttemptDto>(
            '/api/orders/payment/verify',
            request,
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
