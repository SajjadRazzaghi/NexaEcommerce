import {
    Check,
    Package,
    Truck,
} from 'lucide-react';

import {
    useState,
} from 'react';

import {
    useNavigate,
    useParams,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    useAdminOrder,
    useAdminOrderMutations,
} from '@/modules/orders/hooks/useAdminOrders';

import {
    useShipment,
    useShipmentMutations,
} from '@/modules/orders/hooks/useShipment';

import type {
    OrderStatus,
} from '@/modules/orders/types';

const nextStatuses: OrderStatus[] = [
    'Paid',
    'Processing',
    'Shipped',
    'Delivered',
    'Cancelled',
];

export default function AdminOrderDetailsPage() {
    const { id } =
        useParams();

    const navigate =
        useNavigate();

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
        useAdminOrder(id);

    const {
        data: shipment,
        isLoading:
            shipmentLoading,
    } =
        useShipment(id);

    const {
        updateStatus,
    } =
        useAdminOrderMutations();

    const {
        create,
        ship,
        deliver,
    } =
        useShipmentMutations(
            id ?? '',
        );

    const [
        shippingMethod,
        setShippingMethod,
    ] =
        useState(
            'Standard',
        );

    const [
        carrier,
        setCarrier,
    ] =
        useState(
            'TestCarrier',
        );

    const [
        trackingNumber,
        setTrackingNumber,
    ] =
        useState('');

    const text = isFa
        ? {
              loading:
                  'در حال بارگذاری سفارش...',
              notFound:
                  'سفارش پیدا نشد.',
              back:
                  'بازگشت',
              status:
                  'وضعیت',
              update:
                  'تغییر وضعیت',
              order:
                  'سفارش',
              customer:
                  'مشتری',
              items:
                  'اقلام',
              shipping:
                  'ارسال',
              method:
                  'روش ارسال',
              carrier:
                  'حامل',
              tracking:
                  'کد رهگیری',
              create:
                  'ایجاد مرسوله',
              ship:
                  'ارسال سفارش',
              deliver:
                  'ثبت تحویل',
              noShipment:
                  'مرسوله هنوز ایجاد نشده است.',
              total:
                  'مبلغ کل',
              creating:
                  'در حال ایجاد...',
              updating:
                  'در حال بروزرسانی...',
              shippingNow:
                  'در حال ارسال...',
              delivering:
                  'در حال ثبت تحویل...',
          }
        : {
              loading:
                  'Loading order...',
              notFound:
                  'Order not found.',
              back:
                  'Back',
              status:
                  'Status',
              update:
                  'Update status',
              order:
                  'Order',
              customer:
                  'Customer',
              items:
                  'Items',
              shipping:
                  'Shipping',
              method:
                  'Shipping method',
              carrier:
                  'Carrier',
              tracking:
                  'Tracking number',
              create:
                  'Create shipment',
              ship:
                  'Ship order',
              deliver:
                  'Mark delivered',
              noShipment:
                  'No shipment has been created yet.',
              total:
                  'Total',
              creating:
                  'Creating...',
              updating:
                  'Updating...',
              shippingNow:
                  'Shipping...',
              delivering:
                  'Marking delivered...',
          };

    if (
        orderLoading ||
        shipmentLoading
    ) {
        return (
            <div
                className="p-6"
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

    if (!order || !id) {
        return (
            <div
                className="p-6"
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

    const canCreateShipment =
        order.status ===
        'Processing' &&
        !shipment;

    const canShip =
        shipment?.status ===
            'Pending' &&
        Boolean(
            shipment.trackingNumber,
        );

    const canDeliver =
        shipment?.status ===
        'Shipped';

    return (
        <div
            className="grid gap-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <div className="flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-semibold">
                        {text.order}{' '}
                        {
                            order.orderNumber
                        }
                    </h1>

                    <p className="text-muted-foreground mt-1">
                        {order.shippingFullName} · {order.shippingPhone}
                    </p>
                </div>

                <button
                    type="button"
                    onClick={() =>
                        navigate(
                            '/admin/orders',
                        )
                    }
                    className="rounded-lg border px-4 py-2 text-sm"
                >
                    {
                        text.back
                    }
                </button>
            </div>

            <div className="grid gap-6 xl:grid-cols-[1fr_400px]">
                <div className="grid gap-6">
                    <section className="rounded-xl border p-6">
                        <div className="flex items-center justify-between gap-4">
                            <h2 className="font-semibold">
                                {
                                    text.status
                                }
                            </h2>

                            <span className="rounded-full border px-3 py-1 text-sm">
                                {
                                    order.status
                                }
                            </span>
                        </div>

                        <div className="mt-5 flex flex-wrap gap-2">
                            {nextStatuses.map(
                                status => (
                                    <button
                                        key={
                                            status
                                        }
                                        type="button"
                                        disabled={
                                            updateStatus.isPending ||
                                            status ===
                                                order.status
                                        }
                                        onClick={() =>
                                            updateStatus.mutate(
                                                {
                                                    id:
                                                        order.id,
                                                    status,
                                                },
                                            )
                                        }
                                        className="rounded-lg border px-3 py-2 text-xs disabled:opacity-50"
                                    >
                                        {updateStatus.isPending &&
                                        status ===
                                            order.status
                                            ? text.updating
                                            : status}
                                    </button>
                                ),
                            )}
                        </div>
                    </section>

                    <section className="rounded-xl border p-6">
                        <h2 className="font-semibold">
                            {
                                text.items
                            }
                        </h2>

                        <div className="mt-5 grid gap-3">
                            {order.items.map(
                                item => (
                                    <div
                                        key={
                                            item.productVariantId
                                        }
                                        className="flex flex-wrap justify-between gap-4 rounded-lg border p-4"
                                    >
                                        <div>
                                            <div className="font-medium">
                                                {
                                                    item.productName
                                                }
                                            </div>

                                            <div className="text-muted-foreground mt-1 text-sm">
                                                {
                                                    item.sku
                                                }
                                            </div>

                                            <div className="mt-2 text-sm">
                                                ×{' '}
                                                {
                                                    item.quantity
                                                }
                                            </div>
                                        </div>

                                        <div className="font-medium">
                                            {item.lineTotal.toLocaleString()}{' '}
                                            {
                                                order.currency
                                            }
                                        </div>
                                    </div>
                                ),
                            )}
                        </div>

                        <div className="mt-5 flex justify-between border-t pt-4 font-bold">
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
                    </section>

                    <section className="rounded-xl border p-6">
                        <div className="flex items-center gap-2">
                            <Truck className="size-5" />

                            <h2 className="font-semibold">
                                {
                                    text.shipping
                                }
                            </h2>
                        </div>

                        {!shipment &&
                            canCreateShipment && (
                                <div className="mt-5 grid gap-4">
                                    <input
                                        value={
                                            shippingMethod
                                        }
                                        onChange={event =>
                                            setShippingMethod(
                                                event
                                                    .target
                                                    .value,
                                            )
                                        }
                                        placeholder={
                                            text.method
                                        }
                                        className="rounded-lg border px-3 py-2.5"
                                    />

                                    <input
                                        value={
                                            carrier
                                        }
                                        onChange={event =>
                                            setCarrier(
                                                event
                                                    .target
                                                    .value,
                                            )
                                        }
                                        placeholder={
                                            text.carrier
                                        }
                                        className="rounded-lg border px-3 py-2.5"
                                    />

                                    <input
                                        value={
                                            trackingNumber
                                        }
                                        onChange={event =>
                                            setTrackingNumber(
                                                event
                                                    .target
                                                    .value,
                                            )
                                        }
                                        placeholder={
                                            text.tracking
                                        }
                                        className="rounded-lg border px-3 py-2.5"
                                    />

                                    <button
                                        type="button"
                                        disabled={
                                            create.isPending
                                        }
                                        onClick={() =>
                                            create.mutate(
                                                {
                                                    shippingMethod,
                                                    carrier,
                                                    trackingNumber:
                                                        trackingNumber.trim() ||
                                                        null,
                                                },
                                            )
                                        }
                                        className="bg-primary text-primary-foreground inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2.5 font-medium"
                                    >
                                        <Package className="size-4" />

                                        {create.isPending
                                            ? text.creating
                                            : text.create}
                                    </button>
                                </div>
                            )}

                        {!shipment && (
                            <div className="text-muted-foreground mt-4 text-sm">
                                {
                                    text.noShipment
                                }
                            </div>
                        )}

                        {shipment && (
                            <div className="mt-5 grid gap-4">
                                <div className="grid gap-3 sm:grid-cols-2">
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
                                            shipment.trackingNumber ??
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

                                <div className="flex flex-wrap gap-2 border-t pt-4">
                                    {canShip && (
                                        <button
                                            type="button"
                                            disabled={
                                                ship.isPending
                                            }
                                            onClick={() =>
                                                ship.mutate()
                                            }
                                            className="inline-flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium"
                                        >
                                            <Truck className="size-4" />

                                            {ship.isPending
                                                ? text.shippingNow
                                                : text.ship}
                                        </button>
                                    )}

                                    {canDeliver && (
                                        <button
                                            type="button"
                                            disabled={
                                                deliver.isPending
                                            }
                                            onClick={() =>
                                                deliver.mutate()
                                            }
                                            className="inline-flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium"
                                        >
                                            <Check className="size-4" />

                                            {deliver.isPending
                                                ? text.delivering
                                                : text.deliver}
                                        </button>
                                    )}
                                </div>
                            </div>
                        )}
                    </section>
                </div>

                <aside className="h-fit rounded-xl border p-6">
                    <h2 className="font-semibold">
                        {
                            text.customer
                        }
                    </h2>

                    <div className="mt-5 grid gap-2 text-sm">
                        <div>
                            {
                                order.shippingFullName
                            }
                        </div>

                        <div className="text-muted-foreground">
                            {
                                order.shippingPhone
                            }
                        </div>

                        <div className="text-muted-foreground leading-6">
                            {
                                order.shippingAddress
                            }
                        </div>

                        <div className="text-muted-foreground">
                            {
                                order.shippingCity
                            }
                        </div>

                        {
                            order.shippingPostalCode &&
                            (
                                <div className="text-muted-foreground">
                                    {
                                        order.shippingPostalCode
                                    }
                                </div>
                            )
                        }
                    </div>
                </aside>
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
            <div className="text-muted-foreground text-xs">
                {label}
            </div>

            <div className="mt-1 font-medium">
                {value}
            </div>
        </div>
    );
}
