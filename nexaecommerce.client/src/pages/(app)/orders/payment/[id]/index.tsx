import { useState } from 'react';

import {
    useMutation,
    useQuery,
} from '@tanstack/react-query';

import { Link, useParams } from 'react-router-dom';

import {
    createPaymentAttempt,
    completePayment,
} from '@/modules/orders/api/paymentsApi';

import { getOrder } from '@/modules/orders/api/ordersApi';

export default function PaymentPage() {
    const { id: orderId } = useParams();

    const [attemptId, setAttemptId] =
        useState<string | null>(null);

    const [gatewayReference, setGatewayReference] =
        useState('');

    const {
        data: order,
        isLoading,
    } = useQuery({
        queryKey: ['order', orderId],
        queryFn: () => getOrder(orderId!),
        enabled: Boolean(orderId),
    });

    const createAttempt =
        useMutation({
            mutationFn: () =>
                createPaymentAttempt(
                    {
                        orderId: orderId!,
                    },
                    crypto.randomUUID(),
                ),
            onSuccess: (attempt) => {
                setAttemptId(attempt.id);
            },
        });

    const complete =
        useMutation({
            mutationFn: () =>
                completePayment({
                    paymentAttemptId: attemptId!,
                    gatewayName: 'TestGateway',
                    gatewayReference:
                        gatewayReference.trim(),
                }),
        });

    if (isLoading) {
        return (
            <div className="mx-auto max-w-xl p-6">
                Loading payment...
            </div>
        );
    }

    if (!order) {
        return (
            <div className="mx-auto max-w-xl p-6">
                Order not found.
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-xl p-6">
            <div className="rounded-2xl border p-6">
                <h1 className="text-2xl font-bold">
                    Payment
                </h1>

                <div className="mt-2">
                    Order: {order.orderNumber}
                </div>

                <div className="mt-1 text-muted-foreground">
                    Amount:{' '}
                    {order.totalAmount.toLocaleString()}{' '}
                    {order.currency}
                </div>

                {!attemptId && (
                    <button
                        type="button"
                        disabled={createAttempt.isPending}
                        onClick={() =>
                            createAttempt.mutate()
                        }
                        className="mt-8 w-full rounded-xl border px-5 py-3 font-semibold"
                    >
                        {createAttempt.isPending
                            ? 'Preparing payment...'
                            : 'Start payment'}
                    </button>
                )}

                {attemptId && (
                    <div className="mt-8 space-y-4">
                        <input
                            value={gatewayReference}
                            onChange={(e) =>
                                setGatewayReference(
                                    e.target.value,
                                )
                            }
                            placeholder="Gateway reference"
                            className="w-full rounded-lg border px-4 py-3"
                        />

                        <button
                            type="button"
                            disabled={
                                complete.isPending ||
                                gatewayReference.trim()
                                    .length === 0
                            }
                            onClick={() =>
                                complete.mutate()
                            }
                            className="w-full rounded-xl border px-5 py-3 font-semibold"
                        >
                            {complete.isPending
                                ? 'Completing payment...'
                                : 'Complete test payment'}
                        </button>

                        {complete.isSuccess && (
                            <div className="rounded-lg border p-4">
                                Payment completed successfully.

                                <Link
                                    to={`/orders/${order.id}`}
                                    className="mt-4 inline-block underline"
                                >
                                    View order
                                </Link>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}