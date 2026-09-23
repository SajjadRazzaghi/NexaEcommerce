import {
    Check,
    Clock3,
    CreditCard,
    MapPin,
    Package,
    Truck,
} from 'lucide-react';

import {
    Link,
    useParams,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    useOrder,
} from '@/modules/orders/hooks/useOrders';

import {
    useShipment,
} from '@/modules/orders/hooks/useShipment';

import type {
    OrderStatus,
    ShipmentStatus,
} from '@/modules/orders/types';

function statusLabel(
    status: OrderStatus,
    isFa: boolean,
): string {
    const labels: Record<
        OrderStatus,
        {
            en: string;
            fa: string;
        }
    > = {
        PendingPayment: {
            en: 'Pending payment',
            fa: 'در انتظار پرداخت',
        },

        Paid: {
            en: 'Paid',
            fa: 'پرداخت شده',
        },

        Processing: {
            en: 'Processing',
            fa: 'در حال پردازش',
        },

        Shipped: {
            en: 'Shipped',
            fa: 'ارسال شده',
        },

        Delivered: {
            en: 'Delivered',
            fa: 'تحویل شده',
        },

        Cancelled: {
            en: 'Cancelled',
            fa: 'لغو شده',
        },
    };

    return isFa
        ? labels[status].fa
        : labels[status].en;
}

function statusClass(
    status: OrderStatus,
): string {
    switch (status) {
        case 'Cancelled':
            return 'border-destructive/40 bg-destructive/5 text-destructive';

        case 'Delivered':
            return 'border-emerald-500/40 bg-emerald-500/5 text-emerald-600 dark:text-emerald-400';

        case 'Shipped':
            return 'border-blue-500/40 bg-blue-500/5 text-blue-600 dark:text-blue-400';

        case 'Processing':
            return 'border-amber-500/40 bg-amber-500/5 text-amber-600 dark:text-amber-400';

        case 'Paid':
            return 'border-primary/40 bg-primary/5 text-primary';

        case 'PendingPayment':
        default:
            return 'border-border bg-muted/30 text-muted-foreground';
    }
}

function formatMoney(
    amount: number,
    currency: string,
    isFa: boolean,
) {
    return (
        new Intl.NumberFormat(
            isFa
                ? 'fa-IR'
                : undefined,
            {
                maximumFractionDigits: 0,
            },
        ).format(amount) +
        ` ${ currency } `
    );
}

export default function OrderDetailsPage() {
    const { id } =
        useParams();

    const { i18n } =
        useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const {
        data: order,
        isLoading:
            orderLoading,
        isError:
            orderError,
    } =
        useOrder(id);

    const {
        data: shipment,
        isLoading:
            shipmentLoading,
        isError:
            shipmentError,
    } =
        useShipment(id);

    const text = isFa
        ? {
            loading:
                'در حال بارگذاری سفارش...',
            notFound:
                'سفارش پیدا نشد.',
            error:
                'دریافت اطلاعات سفارش با مشکل مواجه شد.',
            back:
                'بازگشت به سفارش‌ها',
            order:
                'سفارش',
            status:
                'وضعیت',
            items:
                'اقلام سفارش',
            quantity:
                'تعداد',
            shipping:
                'ارسال',
            address:
                'آدرس ارسال',
            subtotal:
                'جمع جزء',
            shippingCost:
                'هزینه ارسال',
            discount:
                'تخفیف',
            tax:
                'مالیات',
            total:
                'مبلغ نهایی',
            shipment:
                'مرسوله',
            carrier:
                'شرکت حمل',
            method:
                'روش ارسال',
            tracking:
                'کد رهگیری',
            noShipment:
                'هنوز مرسوله‌ای برای این سفارش ثبت نشده است.',
            orderPlaced:
                'سفارش ثبت شد',
            paid:
                'پرداخت شد',
            processing:
                'در حال آماده‌سازی',
            shipped:
                'ارسال شد',
            delivered:
                'تحویل داده شد',
            payment:
                'پرداخت سفارش',
            cancelled:
                'این سفارش لغو شده است.',
            coupon:
                'کد تخفیف',
        }
        : {
            loading:
                'Loading order...',
            notFound:
                'Order not found.',
            error:
                'We could not load this order.',
            back:
                'Back to orders',
            order:
                'Order',
            status:
                'Status',
            items:
                'Order items',
            quantity:
                'Quantity',
            shipping:
                'Shipping',
            address:
                'Shipping address',
            subtotal:
                'Subtotal',
            shippingCost:
                'Shipping',
            discount:
                'Discount',
            tax:
                'Tax',
            total:
                'Total',
            shipment:
                'Shipment',
            carrier:
                'Carrier',
            method:
                'Shipping method',
            tracking:
                'Tracking number',
            noShipment:
                'No shipment has been created for this order yet.',
            orderPlaced:
                'Order placed',
            paid:
                'Paid',
            processing:
                'Preparing',
            shipped:
                'Shipped',
            delivered:
                'Delivered',
            payment:
                'Pay for order',
            cancelled:
                'This order has been cancelled.',
            coupon:
                'Coupon',
        };

    if (
        orderLoading ||
        shipmentLoading
    ) {
        return (
            <div
                className="mx-auto max-w-6xl p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <div className="animate-pulse space-y-4">
                    <div className="h-8 w-64 rounded-lg bg-muted" />

                    <div className="h-5 w-40 rounded-lg bg-muted" />

                    <div className="h-48 rounded-2xl bg-muted" />

                    <div className="h-64 rounded-2xl bg-muted" />
                </div>
            </div>
        );
    }

    if (
        orderError ||
        !order ||
        !id
    ) {
        return (
            <div
                className="mx-auto max-w-6xl p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <Package className="mx-auto size-10 text-destructive" />

                    <h1 className="mt-4 text-xl font-semibold">
                        {orderError
                            ? text.error
                            : text.notFound}
                    </h1>

                    <Link
                        to="/orders"
                        className="mt-6 inline-flex rounded-lg border px-4 py-2 text-sm font-medium"
                    >
                        {text.back}
                    </Link>
                </div>
            </div>
        );
    }

    const showPayment =
        order.status ===
        'PendingPayment';

    const showTracking =
        order.status !==
            'PendingPayment' &&
        order.status !==
            'Cancelled';

    const formattedSubtotal =
        formatMoney(
            order.subtotal,
            order.currency,
            isFa,
        );

    const formattedShipping =
        formatMoney(
            order.shippingAmount,
            order.currency,
            isFa,
        );

    const formattedDiscount =
        formatMoney(
            order.discountAmount,
            order.currency,
            isFa,
        );

    const formattedTax =
        formatMoney(
            order.taxAmount,
            order.currency,
            isFa,
        );

    const formattedTotal =
        formatMoney(
            order.totalAmount,
            order.currency,
            isFa,
        );

    return (
        <div
            className="mx-auto max-w-6xl p-4 sm:p-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">
                        {text.order}{' '}
                        {order.orderNumber}
                    </h1>

                    <div className="mt-3 flex flex-wrap items-center gap-3">
                        <span
                            className={`inline - flex rounded - full border px - 3 py - 1 text - sm font - medium ${
    statusClass(
        order.status,
    )
} `}
                        >
                            {statusLabel(
                                order.status,
                                isFa,
                            )}
                        </span>

                        {order.status ===
                            'Cancelled' && (
                            <span className="text-sm text-muted-foreground">
                                {text.cancelled}
                            </span>
                        )}
                    </div>
                </div>

                <Link
                    to="/orders"
                    className="rounded-lg border px-4 py-2 text-sm font-medium transition-colors hover:bg-muted"
                >
                    {text.back}
                </Link>
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                <div className="space-y-6">

                    {/* ==================================================
                        Order items
                       ================================================== */}

                    <section className="rounded-2xl border bg-card p-6">
                        <div className="flex items-center gap-2">
                            <Package className="size-5" />

                            <h2 className="text-xl font-semibold">
                                {text.items}
                            </h2>
                        </div>

                        <div className="mt-5 space-y-4">
                            {order.items.map(
                                item => (
                                    <div
                                        key={
                                            item.productVariantId
                                        }
                                        className="rounded-xl border p-4"
                                    >
                                        <div className="flex flex-wrap items-start justify-between gap-4">
                                            <div className="min-w-0">
                                                <div className="font-semibold">
                                                    {
                                                        item.productName
                                                    }
                                                </div>

                                                <div className="mt-1 text-sm text-muted-foreground">
                                                    {
                                                        item.sku
                                                    }
                                                </div>

                                                <div className="mt-3 text-sm">
                                                    {
                                                        text.quantity
                                                    }
                                                    :{' '}
                                                    {
                                                        item.quantity
                                                    }
                                                </div>
                                            </div>

                                            <div className="shrink-0 text-end">
                                                <div className="text-sm text-muted-foreground">
                                                    {formatMoney(
                                                        item.unitPrice,
                                                        order.currency,
                                                        isFa,
                                                    )}
                                                </div>

                                                <div className="mt-1 font-semibold">
                                                    {formatMoney(
                                                        item.lineTotal,
                                                        order.currency,
                                                        isFa,
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ),
                            )}
                        </div>
                    </section>

                    {/* ==================================================
                        Order progress
                       ================================================== */}

                    {order.status !==
                        'Cancelled' && (
                        <section className="rounded-2xl border bg-card p-6">
                            <div className="flex items-center gap-2">
                                <Clock3 className="size-5" />

                                <h2 className="text-xl font-semibold">
                                    {text.status}
                                </h2>
                            </div>

                            <OrderTimeline
                                orderStatus={
                                    order.status
                                }
                                shipmentStatus={
                                    shipment?.status
                                }
                                labels={{
                                    orderPlaced:
                                        text.orderPlaced,
                                    paid:
                                        text.paid,
                                    processing:
                                        text.processing,
                                    shipped:
                                        text.shipped,
                                    delivered:
                                        text.delivered,
                                }}
                            />
                        </section>
                    )}

                    {/* ==================================================
                        Shipment
                       ================================================== */}

                    {showTracking && (
                        <section className="rounded-2xl border bg-card p-6">
                            <div className="flex items-center gap-2">
                                <Truck className="size-5" />

                                <h2 className="text-xl font-semibold">
                                    {
                                        text.shipment
                                    }
                                </h2>
                            </div>

                            {shipmentError ? (
                                <div className="mt-5 rounded-xl border border-destructive/30 bg-destructive/5 p-5 text-sm text-destructive">
                                    {
                                        text.error
                                    }
                                </div>
                            ) : !shipment ? (
                                <div className="mt-5 rounded-xl border border-dashed p-5 text-sm text-muted-foreground">
                                    {
                                        text.noShipment
                                    }
                                </div>
                            ) : (
                                <div className="mt-6 grid gap-5">
                                    <div className="grid gap-4 sm:grid-cols-2">
                                        <Info
                                            label={
                                                text.method
                                            }
                                            value={
                                                shipment.shippingMethod
                                            }
                                        />

                                        <Info
                                            label={
                                                text.carrier
                                            }
                                            value={
                                                shipment.carrier
                                            }
                                        />

                                        <Info
                                            label={
                                                text.tracking
                                            }
                                            value={
                                                shipment.trackingNumber ||
                                                '—'
                                            }
                                        />

                                        <Info
                                            label={
                                                text.status
                                            }
                                            value={
                                                shipment.status
                                            }
                                        />
                                    </div>

                                    {shipment.trackingNumber && (
                                        <div className="rounded-xl border bg-muted/20 p-4">
                                            <div className="text-xs text-muted-foreground">
                                                {
                                                    text.tracking
                                                }
                                            </div>

                                            <div className="mt-2 break-all font-mono text-sm font-medium">
                                                {
                                                    shipment.trackingNumber
                                                }
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )}
                        </section>
                    )}

                    {/* ==================================================
                        Shipping address
                       ================================================== */}

                    {showTracking && (
                        <section className="rounded-2xl border bg-card p-6">
                            <div className="flex items-center gap-2">
                                <MapPin className="size-5" />

                                <h2 className="text-xl font-semibold">
                                    {
                                        text.shipping
                                    }
                                </h2>
                            </div>

                            <div className="mt-5 rounded-xl border p-5">
                                <div className="font-medium">
                                    {
                                        order.shippingFullName
                                    }
                                </div>

                                <div className="mt-1 text-sm text-muted-foreground">
                                    {
                                        order.shippingPhone
                                    }
                                </div>

                                <div className="mt-3 text-sm leading-6 text-muted-foreground">
                                    {
                                        order.shippingAddress
                                    }
                                </div>

                                <div className="text-sm text-muted-foreground">
                                    {
                                        order.shippingCity
                                    }
                                </div>

                                {order.shippingPostalCode && (
                                    <div className="mt-1 text-sm text-muted-foreground">
                                        {
                                            order.shippingPostalCode
                                        }
                                    </div>
                                )}
                            </div>
                        </section>
                    )}
                </div>

                {/* ======================================================
                    Order summary
                   ====================================================== */}

                <aside className="h-fit rounded-2xl border bg-card p-6 lg:sticky lg:top-24">
                    <h2 className="text-xl font-semibold">
                        {text.total}
                    </h2>

                    <div className="mt-6 space-y-4">
                        <div className="flex justify-between gap-4 text-sm">
                            <span>
                                {
                                    text.subtotal
                                }
                            </span>

                            <span className="font-medium">
                                {
                                    formattedSubtotal
                                }
                            </span>
                        </div>

                        <div className="flex justify-between gap-4 text-sm">
                            <span>
                                {
                                    text.shippingCost
                                }
                            </span>

                            <span className="font-medium">
                                {
                                    formattedShipping
                                }
                            </span>
                        </div>

                        {order.discountAmount >
                            0 && (
                            <div className="flex justify-between gap-4 text-sm text-emerald-600 dark:text-emerald-400">
                                <span>
                                    {
                                        text.discount
                                    }
                                </span>

                                <span className="font-medium">
                                    -
                                    {
                                        formattedDiscount
                                    }
                                </span>
                            </div>
                        )}

                        {order.taxAmount >
                            0 && (
                            <div className="flex justify-between gap-4 text-sm">
                                <span>
                                    {
                                        text.tax
                                    }
                                    {order.taxRatePercent >
                                        0 && (
                                        <>
                                            {' '}
                                            (
                                            {
                                                order.taxRatePercent
                                            }
                                            %)
                                        </>
                                    )}
                                </span>

                                <span className="font-medium">
                                    {
                                        formattedTax
                                    }
                                </span>
                            </div>
                        )}

                        {order.couponCode && (
                            <div className="rounded-xl border bg-muted/20 p-3 text-sm">
                                <div className="text-xs text-muted-foreground">
                                    {
                                        text.coupon
                                    }
                                </div>

                                <div className="mt-1 font-mono font-medium">
                                    {
                                        order.couponCode
                                    }
                                </div>
                            </div>
                        )}

                        <div className="border-t pt-4">
                            <div className="flex justify-between gap-4 text-lg font-bold">
                                <span>
                                    {
                                        text.total
                                    }
                                </span>

                                <span>
                                    {
                                        formattedTotal
                                    }
                                </span>
                            </div>
                        </div>

                        {showPayment && (
                            <Link
                                to={`/ orders / payment / ${ order.id } `}
                                className="mt-4 flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-5 py-3 font-semibold text-primary-foreground transition-opacity hover:opacity-90"
                            >
                                <CreditCard className="size-4" />

                                {
                                    text.payment
                                }
                            </Link>
                        )}
                    </div>
                </aside>
            </div>
        </div>
    );
}

interface OrderTimelineProps {
    orderStatus: OrderStatus;
    shipmentStatus?: ShipmentStatus | null;
    labels: {
        orderPlaced: string;
        paid: string;
        processing: string;
        shipped: string;
        delivered: string;
    };
}

function OrderTimeline({
    orderStatus,
    shipmentStatus,
    labels,
}: OrderTimelineProps) {
    const isCancelled =
        orderStatus ===
        'Cancelled';

    const isPaid =
        orderStatus ===
            'Paid' ||
        orderStatus ===
            'Processing' ||
        orderStatus ===
            'Shipped' ||
        orderStatus ===
            'Delivered';

    const isProcessing =
        orderStatus ===
            'Processing' ||
        orderStatus ===
            'Shipped' ||
        orderStatus ===
            'Delivered';

    const isShipped =
        orderStatus ===
            'Shipped' ||
        orderStatus ===
            'Delivered' ||
        shipmentStatus ===
            'Shipped' ||
        shipmentStatus ===
            'Delivered';

    const isDelivered =
        orderStatus ===
            'Delivered' ||
        shipmentStatus ===
            'Delivered';

    const steps = [
        {
            label:
                labels.orderPlaced,
            complete:
                !isCancelled,
            icon:
                Check,
        },
        {
            label:
                labels.paid,
            complete:
                isPaid,
            icon:
                CreditCard,
        },
        {
            label:
                labels.processing,
            complete:
                isProcessing,
            icon:
                Package,
        },
        {
            label:
                labels.shipped,
            complete:
                isShipped,
            icon:
                Truck,
        },
        {
            label:
                labels.delivered,
            complete:
                isDelivered,
            icon:
                Check,
        },
    ];

    return (
        <div className="mt-8">
            <div className="space-y-5">
                {steps.map(
                    step => {
                        const Icon =
                            step.icon;

                        return (
                            <div
                                key={
                                    step.label
                                }
                                className="flex items-center gap-3"
                            >
                                <span
                                    className={`flex size - 9 shrink - 0 items - center justify - center rounded - full border ${
    step.complete
        ? 'border-primary bg-primary text-primary-foreground'
        : 'border-border text-muted-foreground'
} `}
                                >
                                    <Icon className="size-4" />
                                </span>

                                <span
                                    className={
                                        step.complete
                                            ? 'font-medium'
                                            : 'text-muted-foreground'
                                    }
                                >
                                    {
                                        step.label
                                    }
                                </span>
                            </div>
                        );
                    },
                )}
            </div>
        </div>
    );
}

function Info({
    label,
    value,
}: {
    label: string;
    value: string;
}) {
    return (
        <div>
            <div className="text-xs text-muted-foreground">
                {label}
            </div>

            <div className="mt-1 font-medium">
                {value}
            </div>
        </div>
    );
}

