import {
    useEffect,
    useState,
} from 'react';

import {
    Link,
    useNavigate,
    useParams,
} from 'react-router-dom';

import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    useTranslation,
} from 'react-i18next';

import {
    AlertCircle,
    CheckCircle2,
    CreditCard,
    RotateCcw,
} from 'lucide-react';

import {
    getOrder,
} from '@/modules/orders/api/ordersApi';

import {
    createPaymentAttempt,
    completePayment,
    failPayment,
    retryPayment,
} from '@/modules/orders/api/paymentsApi';

import {
    getPaymentAttempt,
} from '@/modules/orders/api/paymentsApi';

export default function PaymentPage() {
    const { id: orderId } =
        useParams();

    const navigate =
        useNavigate();

    const queryClient =
        useQueryClient();

    const { i18n } =
        useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const [
        attemptId,
        setAttemptId,
    ] =
        useState<
            string | null
        >(null);

    const [
        gatewayReference,
        setGatewayReference,
    ] =
        useState('');

    const [
        errorMessage,
        setErrorMessage,
    ] =
        useState<string | null>(
            null,
        );

    const text = isFa
        ? {
              loading:
                  'در حال بارگذاری پرداخت...',
              notFound:
                  'سفارش پیدا نشد.',
              payment:
                  'پرداخت سفارش',
              order:
                  'سفارش',
              amount:
                  'مبلغ',
              start:
                  'شروع پرداخت',
              preparing:
                  'در حال آماده‌سازی...',
              reference:
                  'شناسه تراکنش',
              complete:
                  'تکمیل پرداخت',
              completing:
                  'در حال تکمیل...',
              success:
                  'پرداخت با موفقیت انجام شد.',
              viewOrder:
                  'مشاهده سفارش',
              failed:
                  'پرداخت ناموفق بود.',
              retry:
                  'تلاش مجدد برای پرداخت',
              retrying:
                  'در حال آماده‌سازی پرداخت جدید...',
              failureCode:
                  'کد خطا',
              paymentFailed:
                  'این پرداخت ناموفق بوده است.',
              testGateway:
                  'درگاه آزمایشی',
              required:
                  'شناسه تراکنش را وارد کنید.',
              cancel:
                  'لغو',
          }
        : {
              loading:
                  'Loading payment...',
              notFound:
                  'Order not found.',
              payment:
                  'Order payment',
              order:
                  'Order',
              amount:
                  'Amount',
              start:
                  'Start payment',
              preparing:
                  'Preparing payment...',
              reference:
                  'Gateway reference',
              complete:
                  'Complete payment',
              completing:
                  'Completing...',
              success:
                  'Payment completed successfully.',
              viewOrder:
                  'View order',
              failed:
                  'Payment failed.',
              retry:
                  'Retry payment',
              retrying:
                  'Preparing a new payment attempt...',
              failureCode:
                  'Failure code',
              paymentFailed:
                  'This payment attempt failed.',
              testGateway:
                  'Test gateway',
              required:
                  'Enter a gateway reference.',
              cancel:
                  'Cancel',
          };

    const {
        data: order,
        isLoading,
    } =
        useQuery({
            queryKey: [
                'order',
                orderId,
            ],
            queryFn: () =>
                getOrder(
                    orderId!,
                ),
            enabled:
                Boolean(
                    orderId,
                ),
        });

    const {
        data: paymentAttempt,
    } =
        useQuery({
            queryKey: [
                'payment-attempt',
                attemptId,
            ],
            queryFn: () =>
                getPaymentAttempt(
                    attemptId!,
                ),
            enabled:
                Boolean(
                    attemptId,
                ),
            refetchInterval:
                attemptId
                    ? 5000
                    : false,
        });

    useEffect(() => {
        if (
            paymentAttempt?.status ===
            'Failed'
        ) {
            setErrorMessage(
                paymentAttempt.failureMessage ??
                    text.failed,
            );
        }
    }, [
        paymentAttempt,
        text.failed,
    ]);

    const createAttempt =
        useMutation({
            mutationFn:
                () =>
                    createPaymentAttempt(
                        {
                            orderId:
                                orderId!,
                        },
                        crypto.randomUUID(),
                    ),
            onSuccess:
                attempt => {
                    setErrorMessage(
                        null,
                    );

                    setAttemptId(
                        attempt.id,
                    );
                },
            onError:
                error => {
                    setErrorMessage(
                        error instanceof
                        Error
                            ? error.message
                            : text.failed,
                    );
                },
        });

    const complete =
        useMutation({
            mutationFn:
                () =>
                    completePayment(
                        {
                            paymentAttemptId:
                                attemptId!,
                            gatewayName:
                                text.testGateway,
                            gatewayReference:
                                gatewayReference.trim(),
                        },
                    ),
            onSuccess:
                async () => {
                    setErrorMessage(
                        null,
                    );

                    await queryClient.invalidateQueries(
                        {
                            queryKey: [
                                'order',
                                orderId,
                            ],
                        },
                    );

                    navigate(
                        `/ orders / ${ orderId } `,
                    );
                },
            onError:
                error => {
                    setErrorMessage(
                        error instanceof
                        Error
                            ? error.message
                            : text.failed,
                    );
                },
        });

    const fail =
        useMutation({
            mutationFn:
                () =>
                    failPayment(
                        {
                            paymentAttemptId:
                                attemptId!,
                            failureCode:
                                'TEST_DECLINED',
                            failureMessage:
                                text.failed,
                        },
                    ),
            onSuccess:
                async () => {
                    await queryClient.invalidateQueries(
                        {
                            queryKey: [
                                'payment-attempt',
                                attemptId,
                            ],
                        },
                    );

                    setErrorMessage(
                        text.failed,
                    );
                },
            onError:
                error => {
                    setErrorMessage(
                        error instanceof
                        Error
                            ? error.message
                            : text.failed,
                    );
                },
        });

    const retry =
        useMutation({
            mutationFn:
                () =>
                    retryPayment(
                        orderId!,
                        crypto.randomUUID(),
                    ),
            onSuccess:
                attempt => {
                    setErrorMessage(
                        null,
                    );

                    setAttemptId(
                        attempt.id,
                    );

                    setGatewayReference(
                        '',
                    );
                },
            onError:
                error => {
                    setErrorMessage(
                        error instanceof
                        Error
                            ? error.message
                            : text.failed,
                    );
                },
        });

    if (isLoading) {
        return (
            <div
                className="mx-auto max-w-xl p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                {
                    text.loading
                }
            </div>
        );
    }

    if (!order) {
        return (
            <div
                className="mx-auto max-w-xl p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                {
                    text.notFound
                }
            </div>
        );
    }

    const failed =
        paymentAttempt?.status ===
        'Failed';

    const succeeded =
        paymentAttempt?.status ===
        'Succeeded';

    return (
        <div
            className="mx-auto max-w-xl p-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <div className="rounded-2xl border p-6">
                <div className="flex items-center gap-3">
                    <CreditCard className="size-6" />

                    <h1 className="text-2xl font-bold">
                        {text.payment}
                    </h1>
                </div>

                <div className="mt-5 rounded-xl border p-4">
                    <div className="font-medium">
                        {
                            text.order
                        }:{' '}
                        {
                            order.orderNumber
                        }
                    </div>

                    <div className="text-muted-foreground mt-1">
                        {
                            text.amount
                        }:{' '}
                        {order.totalAmount.toLocaleString()}{' '}
                        {
                            order.currency
                        }
                    </div>
                </div>

                {errorMessage && (
                    <div className="mt-5 flex gap-3 rounded-xl border border-destructive/40 p-4 text-sm text-destructive">
                        <AlertCircle className="size-5 shrink-0" />

                        <div>
                            {
                                errorMessage
                            }

                            {paymentAttempt?.failureCode && (
                                <div className="mt-1">
                                    {
                                        text.failureCode
                                    }
                                    :{' '}
                                    {
                                        paymentAttempt.failureCode
                                    }
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {succeeded && (
                    <div className="mt-5 rounded-xl border p-5">
                        <div className="flex items-center gap-2 font-medium">
                            <CheckCircle2 className="size-5" />

                            {
                                text.success
                            }
                        </div>

                        <Link
                            to={`/ orders / ${ order.id } `}
                            className="mt-4 inline-flex rounded-lg border px-4 py-2"
                        >
                            {
                                text.viewOrder
                            }
                        </Link>
                    </div>
                )}

                {!succeeded &&
                    !failed &&
                    !attemptId && (
                        <button
                            type="button"
                            disabled={
                                createAttempt.isPending
                            }
                            onClick={() =>
                                createAttempt.mutate()
                            }
                            className="mt-8 w-full rounded-xl border px-5 py-3 font-semibold"
                        >
                            {createAttempt.isPending
                                ? text.preparing
                                : text.start}
                        </button>
                    )}

                {!succeeded &&
                    attemptId &&
                    !failed && (
                        <div className="mt-8 space-y-4">
                            <input
                                value={
                                    gatewayReference
                                }
                                onChange={event =>
                                    setGatewayReference(
                                        event
                                            .target
                                            .value,
                                    )
                                }
                                placeholder={
                                    text.reference
                                }
                                className="w-full rounded-lg border px-4 py-3"
                            />

                            <div className="grid gap-3 sm:grid-cols-2">
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
                                    className="bg-primary text-primary-foreground rounded-xl px-5 py-3 font-semibold"
                                >
                                    {complete.isPending
                                        ? text.completing
                                        : text.complete}
                                </button>

                                <button
                                    type="button"
                                    disabled={
                                        fail.isPending
                                    }
                                    onClick={() =>
                                        fail.mutate()
                                    }
                                    className="rounded-xl border px-5 py-3 font-semibold"
                                >
                                    {text.failed}
                                </button>
                            </div>
                        </div>
                    )}

                {failed && (
                    <div className="mt-8 rounded-xl border p-5">
                        <div className="flex items-center gap-2 font-medium">
                            <AlertCircle className="size-5" />

                            {
                                text.paymentFailed
                            }
                        </div>

                        <button
                            type="button"
                            disabled={
                                retry.isPending
                            }
                            onClick={() =>
                                retry.mutate()
                            }
                            className="bg-primary text-primary-foreground mt-5 inline-flex w-full items-center justify-center gap-2 rounded-xl px-5 py-3 font-semibold"
                        >
                            <RotateCcw className="size-4" />

                            {retry.isPending
                                ? text.retrying
                                : text.retry}
                        </button>
                    </div>
                )}

                <Link
                    to={`/ orders / ${ order.id } `}
                    className="text-muted-foreground mt-6 inline-block text-sm underline"
                >
                    {text.cancel}
                </Link>
            </div>
        </div>
    );
}
