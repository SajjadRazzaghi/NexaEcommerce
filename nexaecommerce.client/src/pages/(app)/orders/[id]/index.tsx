import {
    Check,
    Clock3,
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
    useQuery,
} from '@tanstack/react-query';

import {
    getOrder,
} from '@/modules/orders/api/ordersApi';

import {
    useShipment,
} from '@/modules/orders/hooks/useShipment';

function statusClass(
    status: string,
) {
    switch (status) {
        case 'Paid':
            return 'border';

        case 'Processing':
            return 'border';

        case 'Shipped':
            return 'border';

        case 'Delivered':
            return 'border';

        case 'Cancelled':
            return 'border text-destructive';

        case 'PendingPayment':
            return 'border';

        default:
            return 'border';
    }
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
    } =
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

    const {
        data: shipment,
        isLoading:
            shipmentLoading,
    } =
        useShipment(id);

    const text = isFa
        ? {
              loading:
                  'در حال بارگذاری سفارش...',
              notFound:
                  'سفارش پیدا نشد.',
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
                  'هنوز مرسوله‌ای ثبت نشده است.',
              pending:
                  'در انتظار آماده‌سازی',
              shipped:
                  'ارسال شده',
              delivered:
                  'تحویل داده شده',
              orderPlaced:
                  'سفارش ثبت شد',
              payment:
                  'پرداخت',
          }
        : {
              loading:
                  'Loading order...',
              notFound:
                  'Order not found.',
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
                  'No shipment has been created yet.',
              pending:
                  'Preparing shipment',
              shipped:
                  'Shipped',
              delivered:
                  'Delivered',
              orderPlaced:
                  'Order placed',
              payment:
                  'Payment',
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
                {text.loading}
            </div>
        );
    }

    if (!order) {
        return (
            <div
                className="mx-auto max-w-6xl p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                {text.notFound}
            </div>
        );
    }

    const showPayment =
        order.status ===
        'PendingPayment';

    const showShipping =
        order.status !==
            'PendingPayment' &&
        order.status !==
            'Cancelled';

    return (
        <div
            className="mx-auto max-w-6xl p-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-bold">
                        {text.order}{' '}
                        {order.orderNumber}
                    </h1>

                    <div className="mt-3">
                        <span
                            className={`inline - flex rounded - full px - 3 py - 1 text - sm ${
    statusClass(
        order.status,
    )
} `}
                        >
                            {
                                order.status
                            }
                        </span>
                    </div>
                </div>

                <Link
                    to="/orders"
                    className="rounded-lg border px-4 py-2"
                >
                    {text.back}
                </Link>
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                <div className="space-y-6">
                    <section className="rounded-2xl border p-6">
                        <h2 className="text-xl font-semibold">
                            {text.items}
                        </h2>

                        <div className="mt-5 space-y-4">
                            {order.items.map(
                                item => (
                                    <div
                                        key={
                                            item.productVariantId
                                        }
                                        className="rounded-xl border p-4"
                                    >
                                        <div className="flex flex-wrap justify-between gap-4">
                                            <div>
                                                <div className="font-semibold">
                                                    {
                                                        item.productName
                                                    }
                                                </div>

                                                <div className="text-muted-foreground mt-1 text-sm">
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

                                            <div className="font-semibold">
                                                {item.lineTotal.toLocaleString()}{' '}
                                                {
                                                    order.currency
                                                }
                                            </div>
                                        </div>
                                    </div>
                                ),
                            )}
                        </div>
                    </section>

                    {showShipping && (
                        <section className="rounded-2xl border p-6">
                            <div className="flex items-center gap-2">
                                <Package className="size-5" />

                                <h2 className="text-xl font-semibold">
                                    {
                                        text.shipment
                                    }
                                </h2>
                            </div>

                            {!shipment ? (
                                <div className="text-muted-foreground mt-5 rounded-xl border border-dashed p-5 text-sm">
                                    {
                                        text.noShipment
                                    }
                                </div>
                            ) : (
                                <>
                                    <div className="mt-6 grid gap-4 sm:grid-cols-2">
                                        <div>
                                            <div className="text-muted-foreground text-xs">
                                                {
                                                    text.method
                                                }
                                            </div>

                                            <div className="mt-1 font-medium">
                                                {
                                                    shipment.shippingMethod
                                                }
                                            </div>
                                        </div>

                                        <div>
                                            <div className="text-muted-foreground text-xs">
                                                {
                                                    text.carrier
                                                }
                                            </div>

                                            <div className="mt-1 font-medium">
                                                {
                                                    shipment.carrier
                                                }
                                            </div>
                                        </div>

                                        <div>
                                            <div className="text-muted-foreground text-xs">
                                                {
                                                    text.tracking
                                                }
                                            </div>

                                            <div className="mt-1 font-medium">
                                                {shipment.trackingNumber ??
                                                    '—'}
                                            </div>
                                        </div>

                                        <div>
                                            <div className="text-muted-foreground text-xs">
                                                {
                                                    text.status
                                                }
                                            </div>

                                            <div className="mt-1 font-medium">
                                                {
                                                    shipment.status
                                                }
                                            </div>
                                        </div>
                                    </div>

                                    <ShipmentTimeline
                                        orderStatus={
                                            order.status
                                        }
                                        shipmentStatus={
                                            shipment.status
                                        }
                                        labels={{
                                            orderPlaced:
                                                text.orderPlaced,
                                            pending:
                                                text.pending,
                                            shipped:
                                                text.shipped,
                                            delivered:
                                                text.delivered,
                                        }}
                                    />
                                </>
                            )}
                        </section>
                    )}

                    {showShipping && (
                        <section className="rounded-2xl border p-6">
                            <h2 className="text-xl font-semibold">
                                {
                                    text.shipping
                                }
                            </h2>

                            <div className="mt-5 rounded-xl border p-5">
                                <div className="font-medium">
                                    {
                                        order.shippingFullName
                                    }
                                </div>

                                <div className="text-muted-foreground mt-1 text-sm">
                                    {
                                        order.shippingPhone
                                    }
                                </div>

                                <div className="text-muted-foreground mt-3 text-sm leading-6">
                                    {
                                        order.shippingAddress
                                    }
                                </div>

                                <div className="text-muted-foreground text-sm">
                                    {
                                        order.shippingCity
                                    }
                                </div>

                                {order.shippingPostalCode && (
                                    <div className="text-muted-foreground mt-1 text-sm">
                                        {
                                            order.shippingPostalCode
                                        }
                                    </div>
                                )}
                            </div>
                        </section>
                    )}
                </div>

                <aside className="h-fit rounded-2xl border p-6">
                    <h2 className="text-xl font-semibold">
                        {
                            text.total
                        }
                    </h2>

                    <div className="mt-6 space-y-4">
                        <div className="flex justify-between gap-4">
                            <span>
                                {
                                    text.subtotal
                                }
                            </span>

                            <span>
                                {order.subtotal.toLocaleString()}
                            </span>
                        </div>

                        <div className="flex justify-between gap-4">
                            <span>
                                {
                                    text.shippingCost
                                }
                            </span>

                            <span>
                                {order.shippingAmount.toLocaleString()}
                            </span>
                        </div>

                        <div className="border-t pt-4">
                            <div className="flex justify-between gap-4 text-lg font-bold">
                                <span>
                                    {
                                        text.total
                                    }
                                </span>

                                <span>
                                    {order.totalAmount.toLocaleString()}{' '}
                                    {
                                        order.currency
                                    }
                                </span>
                            </div>
                        </div>

                        {showPayment && (
                            <Link
                                to={`/ orders / payment / ${ order.id } `}
                                className="bg-primary text-primary-foreground mt-4 flex w-full items-center justify-center rounded-xl px-5 py-3 font-semibold"
                            >
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

interface ShipmentTimelineProps {
    orderStatus: string;
    shipmentStatus: string;
    labels: {
        orderPlaced: string;
        pending: string;
        shipped: string;
        delivered: string;
    };
}

function ShipmentTimeline({
    orderStatus,
    shipmentStatus,
    labels,
}: ShipmentTimelineProps) {
    const delivered =
        shipmentStatus ===
        'Delivered';

    const shipped =
        shipmentStatus ===
            'Shipped' ||
        delivered ||
        orderStatus ===
            'Delivered';

    const processing =
        orderStatus ===
            'Processing' ||
        shipped ||
        delivered;

    const steps = [
        {
            label:
                labels.orderPlaced,
            complete:
                orderStatus !==
                'PendingPayment',
            icon: Check,
        },
        {
            label:
                labels.pending,
            complete:
                processing,
            icon: Clock3,
        },
        {
            label:
                labels.shipped,
            complete:
                shipped,
            icon: Truck,
        },
        {
            label:
                labels.delivered,
            complete:
                delivered,
            icon: Check,
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
        ? 'bg-primary text-primary-foreground'
        : ''
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