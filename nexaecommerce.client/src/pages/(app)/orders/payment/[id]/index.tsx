import {
    ArrowLeft,
    CheckCircle2,
    CreditCard,
    Loader2,
    LockKeyhole,
    ShieldCheck,
} from 'lucide-react';

import {
    Link,
    useNavigate,
    useParams,
} from 'react-router-dom';

import {
    useRef,
    useState,
} from 'react';

import {
    useTranslation,
} from 'react-i18next';

import {
    useQuery,
} from '@tanstack/react-query';

import {
    getOrder,
} from '@/modules/orders/api/ordersApi';

import {
    useCompletePayment,
    useStartPayment,
    useVerifyPayment,
} from '@/modules/orders/hooks/usePayment';

function formatMoney(
    amount: number,
    currency: string,
) {
    return (
        new Intl.NumberFormat(
            undefined,
            {
                maximumFractionDigits: 0,
            },
        ).format(amount) +
        ` ${currency}`
    );
}

export default function PaymentPage() {
    const { id } =
        useParams();

    
const navigate =
    useNavigate();

const {
    t,
    i18n,
} =
    useTranslation();

const isFa =
    i18n.language
        ?.toLowerCase()
        .startsWith('fa');

const getText = (
    key: string,
    fallback: string,
) =>
    t(
        key,
        {
            defaultValue:
                fallback,
        },
    );

const paymentKeyRef =
    useRef<string | null>(null);

const [payment, setPayment] =
    useState<{
        paymentAttemptId: string;
        gatewayName: string;
        gatewayReference: string;
        amount: number;
        currency: string;
        status: string;
    } | null>(null);

const [error, setError] =
    useState<string | null>(null);

const orderQuery =
    useQuery({
        queryKey: [
            'order',
            id,
        ],
        queryFn: () =>
            getOrder(id!),
        enabled:
            Boolean(id),
    });

const startPayment =
    useStartPayment();

const verifyPayment =
    useVerifyPayment();

const completePayment =
    useCompletePayment();

const order =
    orderQuery.data;

const canStartPayment =
    order?.status ===
    'PendingPayment';

const total =
    order?.totalAmount ??
    0;

const busy =
    startPayment.isPending ||
    verifyPayment.isPending ||
    completePayment.isPending;

const handleStartPayment = () => {
    if (!order) {
        return;
    }

    setError(null);

    if (!paymentKeyRef.current) {
        paymentKeyRef.current =
            crypto.randomUUID();
    }

    const callbackUrl =
        `${ window.location.origin } /orders/payment / ${ order.id } `;

    startPayment.mutate(
        {
            orderId:
                order.id,

            gatewayName:
                'TestGateway',

            callbackUrl,

            idempotencyKey:
                paymentKeyRef.current,
        },
        {
            onSuccess:
                result => {
                    if (
                        !result.gatewayReference
                    ) {
                        setError(
                            getText(
                                'payment.missingReference',
                                'The payment gateway did not return a payment reference.',
                            ),
                        );

                        return;
                    }

                    setPayment({
                        paymentAttemptId:
                            result.paymentAttemptId,

                        gatewayName:
                            result.gatewayName,

                        gatewayReference:
                            result.gatewayReference,

                        amount:
                            result.amount,

                        currency:
                            result.currency,

                        status:
                            result.status,
                    });
                },

            onError:
                () => {
                    setError(
                        getText(
                            'payment.startError',
                            'Unable to start the payment. Please try again.',
                        ),
                    );
                },
        },
    );
};

const handleConfirmPayment = () => {
    if (!payment) {
        return;
    }

    setError(null);

    verifyPayment.mutate(
        {
            paymentAttemptId:
                payment.paymentAttemptId,

            gatewayReference:
                payment.gatewayReference,
        },
        {
            onSuccess:
                verified => {
                    completePayment.mutate(
                        {
                            paymentAttemptId:
                                verified.id,

                            gatewayName:
                                verified.gatewayName ??
                                payment.gatewayName,

                            gatewayReference:
                                verified.gatewayReference ??
                                payment.gatewayReference,
                        },
                        {
                            onSuccess:
                                () => {
                                    navigate(
                                        `/ orders / ${ order.id } `,
                                        {
                                            replace: true,
                                        },
                                    );
                                },

                            onError:
                                () => {
                                    setError(
                                        getText(
                                            'payment.completeError',
                                            'Payment verification succeeded, but the order could not be completed.',
                                        ),
                                    );
                                },
                        },
                    );
                },

            onError:
                () => {
                    setError(
                        getText(
                            'payment.verifyError',
                            'Payment verification failed.',
                        ),
                    );
                },
        },
    );
};

if (
    orderQuery.isLoading
) {
    return (
        <div
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
            className="mx-auto max-w-4xl space-y-6 p-4 md:p-6"
        >
            <div className="animate-pulse space-y-4">
                <div className="h-8 w-52 rounded-lg bg-muted" />
                <div className="h-5 w-80 rounded-lg bg-muted" />

                <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
                    <div className="h-80 rounded-2xl bg-muted" />
                    <div className="h-64 rounded-2xl bg-muted" />
                </div>
            </div>
        </div>
    );
}

if (
    orderQuery.isError ||
    !order
) {
    return (
        <div
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
            className="mx-auto max-w-3xl p-6"
        >
            <div className="rounded-2xl border p-10 text-center">
                <CreditCard className="mx-auto size-12 text-muted-foreground" />

                <h1 className="mt-4 text-2xl font-semibold">
                    {getText(
                        'payment.orderNotFound',
                        'Order not found',
                    )}
                </h1>

                <p className="mt-2 text-muted-foreground">
                    {getText(
                        'payment.orderNotFoundDescription',
                        'We could not load this order for payment.',
                    )}
                </p>

                <Link
                    to="/orders"
                    className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                >
                    <ArrowLeft className="size-4" />

                    {getText(
                        'payment.backToOrders',
                        'Back to orders',
                    )}
                </Link>
            </div>
        </div>
    );
}

if (
    order.status !==
        'PendingPayment' &&
    order.status !==
        'Paid'
) {
    return (
        <div
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
            className="mx-auto max-w-3xl p-6"
        >
            <div className="rounded-2xl border p-10 text-center">
                <CheckCircle2 className="mx-auto size-12 text-muted-foreground" />

                <h1 className="mt-4 text-2xl font-semibold">
                    {getText(
                        'payment.notAvailable',
                        'Payment is not available',
                    )}
                </h1>

                <p className="mt-2 text-muted-foreground">
                    {getText(
                        'payment.notAvailableDescription',
                        'This order is no longer waiting for payment.',
                    )}
                </p>

                <Link
                    to={`/ orders / ${ order.id } `}
                    className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                >
                    <ArrowLeft className="size-4" />

                    {getText(
                        'payment.backToOrder',
                        'Back to order',
                    )}
                </Link>
            </div>
        </div>
    );
}

if (
    order.status ===
    'Paid'
) {
    return (
        <div
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
            className="mx-auto max-w-3xl p-6"
        >
            <div className="rounded-2xl border p-10 text-center">
                <CheckCircle2 className="mx-auto size-14 text-green-600" />

                <h1 className="mt-4 text-2xl font-semibold">
                    {getText(
                        'payment.alreadyPaid',
                        'Order already paid',
                    )}
                </h1>

                <p className="mt-2 text-muted-foreground">
                    {getText(
                        'payment.alreadyPaidDescription',
                        'This order has already been paid successfully.',
                    )}
                </p>

                <Link
                    to={`/ orders / ${ order.id } `}
                    className="mt-6 inline-flex items-center justify-center rounded-xl bg-primary px-5 py-3 font-semibold text-primary-foreground"
                >
                    {getText(
                        'payment.viewOrder',
                        'View order',
                    )}
                </Link>
            </div>
        </div>
    );
}

return (
    <div
        dir={
            isFa
                ? 'rtl'
                : 'ltr'
        }
        className="mx-auto max-w-4xl space-y-6 p-4 md:p-6"
    >
        <header>
            <div className="flex items-center gap-3">
                <div className="rounded-xl border p-2">
                    <CreditCard className="size-5" />
                </div>

                <div>
                    <h1 className="text-3xl font-bold tracking-tight">
                        {getText(
                            'payment.title',
                            'Secure payment',
                        )}
                    </h1>

                    <p className="mt-1 text-muted-foreground">
                        {getText(
                            'payment.subtitle',
                            'Complete payment for your order.',
                        )}
                    </p>
                </div>
            </div>
        </header>

        {error && (
            <div
                role="alert"
                className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm"
            >
                {error}
            </div>
        )}

        <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
            <section className="rounded-2xl border p-6">
                <div className="flex items-center gap-3">
                    <ShieldCheck className="size-6" />

                    <div>
                        <h2 className="font-semibold">
                            {getText(
                                'payment.secureTitle',
                                'Secure checkout',
                            )}
                        </h2>

                        <p className="text-sm text-muted-foreground">
                            {getText(
                                'payment.secureDescription',
                                'Your payment is processed through the configured payment gateway.',
                            )}
                        </p>
                    </div>
                </div>

                <div className="mt-8 space-y-4">
                    <div className="rounded-xl border p-4">
                        <div className="flex items-center gap-3">
                            <LockKeyhole className="size-5 shrink-0" />

                            <div>
                                <div className="font-medium">
                                    {getText(
                                        'payment.gateway',
                                        'Payment gateway',
                                    )}
                                </div>

                                <div className="mt-1 text-sm text-muted-foreground">
                                    {payment?.gatewayName ??
                                        'TestGateway'}
                                </div>
                            </div>
                        </div>
                    </div>

                    {!payment ? (
                        <button
                            type="button"
                            disabled={
                                !canStartPayment ||
                                busy
                            }
                            onClick={
                                handleStartPayment
                            }
                            className="flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-primary px-5 font-semibold text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            {startPayment.isPending ? (
                                <>
                                    <Loader2 className="size-4 animate-spin" />

                                    {getText(
                                        'payment.starting',
                                        'Starting payment...',
                                    )}
                                </>
                            ) : (
                                <>
                                    <CreditCard className="size-4" />

                                    {getText(
                                        'payment.start',
                                        'Start payment',
                                    )}
                                </>
                            )}
                        </button>
                    ) : (
                        <div className="space-y-4">
                            <div className="rounded-xl border p-5">
                                <div className="text-sm text-muted-foreground">
                                    {getText(
                                        'payment.reference',
                                        'Payment reference',
                                    )}
                                </div>

                                <div className="mt-2 break-all font-mono text-sm">
                                    {
                                        payment.gatewayReference
                                    }
                                </div>

                                <div className="mt-4 text-sm text-muted-foreground">
                                    {getText(
                                        'payment.testGatewayHint',
                                        'This is a test payment. Confirming will verify the transaction and mark the order as paid.',
                                    )}
                                </div>
                            </div>

                            <button
                                type="button"
                                disabled={
                                    busy
                                }
                                onClick={
                                    handleConfirmPayment
                                }
                                className="flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-primary px-5 font-semibold text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                            >
                                {busy ? (
                                    <>
                                        <Loader2 className="size-4 animate-spin" />

                                        {getText(
                                            'payment.processing',
                                            'Processing payment...',
                                        )}
                                    </>
                                ) : (
                                    <>
                                        <CheckCircle2 className="size-4" />

                                        {getText(
                                            'payment.confirm',
                                            'Confirm payment',
                                        )}
                                    </>
                                )}
                            </button>
                        </div>
                    )}
                </div>
            </section>

            <aside className="h-fit rounded-2xl border p-6 lg:sticky lg:top-6">
                <div className="flex items-center gap-3">
                    <CheckCircle2 className="size-5" />

                    <h2 className="font-semibold">
                        {getText(
                            'payment.summary',
                            'Order summary',
                        )}
                    </h2>
                </div>

                <div className="mt-6 space-y-4">
                    <div className="flex justify-between gap-4 text-sm">
                        <span className="text-muted-foreground">
                            {getText(
                                'payment.orderNumber',
                                'Order',
                            )}
                        </span>

                        <span className="font-medium">
                            {
                                order.orderNumber
                            }
                        </span>
                    </div>

                    <div className="flex justify-between gap-4 text-sm">
                        <span className="text-muted-foreground">
                            {getText(
                                'payment.status',
                                'Status',
                            )}
                        </span>

                        <span className="font-medium">
                            {
                                order.status
                            }
                        </span>
                    </div>

                    <div className="border-t pt-4">
                        <div className="flex items-center justify-between gap-4">
                            <span className="font-semibold">
                                {getText(
                                    'payment.amount',
                                    'Amount',
                                )}
                            </span>

                            <span className="text-xl font-bold">
                                {formatMoney(
                                    payment?.amount ??
                                        total,
                                    payment?.currency ??
                                        order.currency,
                                )}
                            </span>
                        </div>
                    </div>
                </div>

                <Link
                    to={`/ orders / ${ order.id } `}
                    className="mt-6 block text-center text-sm font-medium underline-offset-4 hover:underline"
                >
                    {getText(
                        'payment.backToOrder',
                        'Return to order',
                    )}
                </Link>
            </aside>
        </div>
    </div>
);


}
