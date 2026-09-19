import {
    Check,
    Package,
    Truck,
} from 'lucide-react';
import {
    useTranslation,
} from 'react-i18next';
import {
    useState,
} from 'react';

import {
    useNavigate,
    useParams,
} from 'react-router-dom';

import {
    useAdminOrder,
} from '@/modules/orders/hooks/useAdminOrders';

import {
    useShipment,
    useShipmentMutations,
} from '@/modules/orders/hooks/useShipment';

import {
    useFulfillment,
    useFulfillmentMutations,
} from '@/modules/orders/hooks/useFulfillment';

export default function AdminOrderDetailsPage() {
    const { id } =
        useParams();

    
const navigate =
    useNavigate();

const {
    i18n,
} =
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
    useAdminOrder(id);

const {
    data: shipment,
    isLoading:
        shipmentLoading,
} =
    useShipment(id);

const {
    data: fulfillment,
    isLoading:
        fulfillmentLoading,
} =
    useFulfillment(id);

const {
    create,
    ship,
    deliver,
} =
    useShipmentMutations(
        id ?? '',
    );

const {
    start:
        startFulfillmentMutation,
    allocate:
        allocateMutation,
    reserve:
        reserveMutation,
    picking:
        pickingMutation,
    picked:
        pickedMutation,
    packing:
        packingMutation,
    packed:
        packedMutation,
    readyToShip:
        readyToShipMutation,
} =
    useFulfillmentMutations(
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

const [
    fulfillmentError,
    setFulfillmentError,
] =
    useState<string | null>(
        null,
    );

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
              'در حال انجام...',
          shippingNow:
              'در حال ارسال...',
          delivering:
              'در حال ثبت تحویل...',
          fulfillment:
              'فرآیند آماده‌سازی سفارش',
          fulfillmentNotStarted:
              'فرآیند آماده‌سازی هنوز شروع نشده است.',
          startFulfillment:
              'شروع پردازش سفارش',
          allocate:
              'تخصیص انبار',
          reserve:
              'رزرو موجودی',
          startPicking:
              'شروع جمع‌آوری',
          picked:
              'جمع‌آوری شد',
          startPacking:
              'شروع بسته‌بندی',
          packed:
              'بسته‌بندی شد',
          ready:
              'آماده ارسال',
          warehouse:
              'انبار',
          pickingLocation:
              'موقعیت برداشت',
          fulfillmentError:
              'عملیات آماده‌سازی سفارش انجام نشد.',
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
              'Processing...',
          shippingNow:
              'Shipping...',
          delivering:
              'Marking delivered...',
          fulfillment:
              'Order fulfillment',
          fulfillmentNotStarted:
              'Fulfillment has not started yet.',
          startFulfillment:
              'Start processing',
          allocate:
              'Allocate warehouse',
          reserve:
              'Reserve stock',
          startPicking:
              'Start picking',
          picked:
              'Mark picked',
          startPacking:
              'Start packing',
          packed:
              'Mark packed',
          ready:
              'Ready to ship',
          warehouse:
              'Warehouse',
          pickingLocation:
              'Picking location',
          fulfillmentError:
              'The fulfillment operation failed.',
      };

const isFulfillmentBusy =
    startFulfillmentMutation.isPending ||
    allocateMutation.isPending ||
    reserveMutation.isPending ||
    pickingMutation.isPending ||
    pickedMutation.isPending ||
    packingMutation.isPending ||
    packedMutation.isPending ||
    readyToShipMutation.isPending;

const runFulfillmentMutation =
    async (
        action: () => Promise<unknown>,
    ) => {
        setFulfillmentError(
            null,
        );

        try {
            await action();
        } catch (error) {
            setFulfillmentError(
                error instanceof Error
                    ? error.message
                    : text.fulfillmentError,
            );
        }
    };

if (
    orderLoading ||
    shipmentLoading ||
    fulfillmentLoading
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
            <div className="animate-pulse space-y-3">
                <div className="h-8 w-56 rounded-lg bg-muted" />

                <div className="h-4 w-72 rounded-lg bg-muted" />

                <div className="h-32 rounded-xl bg-muted" />

                <div className="h-72 rounded-xl bg-muted" />
            </div>
        </div>
    );
}

if (
    !order ||
    !id ||
    orderError
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
            {text.notFound}
        </div>
    );
}

const canShip =
    shipment?.status ===
        'Pending' &&
    Boolean(
        shipment.trackingNumber,
    ) &&
    fulfillment?.status ===
        'ReadyToShip';

const canDeliver =
    shipment?.status ===
        'Shipped' &&
    fulfillment?.status ===
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
                    {order.orderNumber}
                </h1>

                <p className="mt-1 text-muted-foreground">
                    {order.shippingFullName} ·{' '}
                    {order.shippingPhone}
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
                {text.back}
            </button>
        </div>

        <div className="grid gap-6 xl:grid-cols-[1fr_400px]">
            <div className="grid gap-6">
                <section className="rounded-xl border p-6">
                    <div className="flex items-center justify-between gap-4">
                        <h2 className="font-semibold">
                            {text.status}
                        </h2>

                        <span className="rounded-full border px-3 py-1 text-sm">
                            {order.status}
                        </span>
                    </div>
                </section>

                <section className="rounded-xl border p-6">
                    <div className="flex items-center gap-2">
                        <Package className="size-5" />

                        <h2 className="font-semibold">
                            {
                                text.fulfillment
                            }
                        </h2>
                    </div>

                    {fulfillmentError && (
                        <div
                            role="alert"
                            className="mt-4 rounded-lg border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive"
                        >
                            {
                                fulfillmentError
                            }
                        </div>
                    )}

                    {!fulfillment && (
                        <div className="mt-5 grid gap-4">
                            <p className="text-sm text-muted-foreground">
                                {
                                    text.fulfillmentNotStarted
                                }
                            </p>

                            {order.status ===
                                'Paid' && (
                                <button
                                    type="button"
                                    disabled={
                                        isFulfillmentBusy
                                    }
                                    onClick={() =>
                                        void runFulfillmentMutation(
                                            () =>
                                                startFulfillmentMutation.mutateAsync(),
                                        )
                                    }
                                    className="inline-flex w-fit items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
                                >
                                    <Truck className="size-4" />

                                    {
                                        startFulfillmentMutation.isPending
                                            ? text.creating
                                            : text.startFulfillment
                                    }
                                </button>
                            )}
                        </div>
                    )}

                    {fulfillment && (
                        <div className="mt-5 grid gap-5">
                            <div className="grid gap-3 sm:grid-cols-2">
                                <Info
                                    label={
                                        text.status
                                    }
                                    value={
                                        fulfillment.status
                                    }
                                />

                                <Info
                                    label={
                                        text.warehouse
                                    }
                                    value={
                                        fulfillment.warehouseId ??
                                        '—'
                                    }
                                />

                                <Info
                                    label={
                                        text.pickingLocation
                                    }
                                    value={
                                        fulfillment.pickingLocationId ??
                                        '—'
                                    }
                                />
                            </div>

                            <div className="flex flex-wrap gap-2 border-t pt-4">
                                {fulfillment.status ===
                                    'Pending' &&
                                    !fulfillment.warehouseId &&
                                    order.status ===
                                        'Processing' && (
                                        <button
                                            type="button"
                                            disabled={
                                                isFulfillmentBusy
                                            }
                                            onClick={() =>
                                                void runFulfillmentMutation(
                                                    () =>
                                                        allocateMutation.mutateAsync(),
                                                )
                                            }
                                            className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                        >
                                            {
                                                allocateMutation.isPending
                                                    ? text.creating
                                                    : text.allocate
                                            }
                                        </button>
                                    )}

                                {fulfillment.status ===
                                    'Pending' &&
                                    Boolean(
                                        fulfillment.warehouseId,
                                    ) && (
                                        <button
                                            type="button"
                                            disabled={
                                                isFulfillmentBusy
                                            }
                                            onClick={() =>
                                                void runFulfillmentMutation(
                                                    () =>
                                                        reserveMutation.mutateAsync(),
                                                )
                                            }
                                            className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                        >
                                            {
                                                reserveMutation.isPending
                                                    ? text.creating
                                                    : text.reserve
                                            }
                                        </button>
                                    )}

                                {fulfillment.status ===
                                    'Pending' &&
                                    Boolean(
                                        fulfillment.warehouseId,
                                    ) && (
                                        <button
                                            type="button"
                                            disabled={
                                                isFulfillmentBusy
                                            }
                                            onClick={() =>
                                                void runFulfillmentMutation(
                                                    () =>
                                                        pickingMutation.mutateAsync(),
                                                )
                                            }
                                            className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                        >
                                            {
                                                pickingMutation.isPending
                                                    ? text.creating
                                                    : text.startPicking
                                            }
                                        </button>
                                    )}

                                {fulfillment.status ===
                                    'Picking' && (
                                    <button
                                        type="button"
                                        disabled={
                                            isFulfillmentBusy
                                        }
                                        onClick={() =>
                                            void runFulfillmentMutation(
                                                () =>
                                                    pickedMutation.mutateAsync(),
                                            )
                                        }
                                        className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                    >
                                        {
                                            pickedMutation.isPending
                                                ? text.creating
                                                : text.picked
                                        }
                                    </button>
                                )}

                                {fulfillment.status ===
                                    'Picked' && (
                                    <button
                                        type="button"
                                        disabled={
                                            isFulfillmentBusy
                                        }
                                        onClick={() =>
                                            void runFulfillmentMutation(
                                                () =>
                                                    packingMutation.mutateAsync(),
                                            )
                                        }
                                        className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                    >
                                        {
                                            packingMutation.isPending
                                                ? text.creating
                                                : text.startPacking
                                        }
                                    </button>
                                )}

                                {fulfillment.status ===
                                    'Packing' && (
                                    <button
                                        type="button"
                                        disabled={
                                            isFulfillmentBusy
                                        }
                                        onClick={() =>
                                            void runFulfillmentMutation(
                                                () =>
                                                    packedMutation.mutateAsync(),
                                            )
                                        }
                                        className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                    >
                                        {
                                            packedMutation.isPending
                                                ? text.creating
                                                : text.packed
                                        }
                                    </button>
                                )}

                                {fulfillment.status ===
                                    'Packed' && (
                                    <button
                                        type="button"
                                        disabled={
                                            isFulfillmentBusy
                                        }
                                        onClick={() =>
                                            void runFulfillmentMutation(
                                                () =>
                                                    readyToShipMutation.mutateAsync(),
                                            )
                                        }
                                        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
                                    >
                                        <Truck className="size-4" />

                                        {
                                            readyToShipMutation.isPending
                                                ? text.creating
                                                : text.ready
                                        }
                                    </button>
                                )}
                            </div>
                        </div>
                    )}
                </section>

                <section className="rounded-xl border p-6">
                    <h2 className="font-semibold">
                        {text.items}
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

                                        <div className="mt-1 text-sm text-muted-foreground">
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
                        order.status ===
                            'Processing' && (
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
                                    className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2.5 font-medium text-primary-foreground disabled:opacity-50"
                                >
                                    <Package className="size-4" />

                                    {
                                        create.isPending
                                            ? text.creating
                                            : text.create
                                    }
                                </button>
                            </div>
                        )}

                    {!shipment && (
                        <div className="mt-4 text-sm text-muted-foreground">
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
                                        className="inline-flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                    >
                                        <Truck className="size-4" />

                                        {
                                            ship.isPending
                                                ? text.shippingNow
                                                : text.ship
                                        }
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
                                        className="inline-flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                    >
                                        <Check className="size-4" />

                                        {
                                            deliver.isPending
                                                ? text.delivering
                                                : text.deliver
                                        }
                                    </button>
                                )}
                            </div>
                        </div>
                    )}
                </section>
            </div>

            <aside className="h-fit rounded-xl border p-6">
                <h2 className="font-semibold">
                    {text.customer}
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

                    <div className="leading-6 text-muted-foreground">
                        {
                            order.shippingAddress
                        }
                    </div>

                    <div className="text-muted-foreground">
                        {
                            order.shippingCity
                        }
                    </div>

                    {order.shippingPostalCode && (
                        <div className="text-muted-foreground">
                            {
                                order.shippingPostalCode
                            }
                        </div>
                    )}
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
    return (<div> <div className="text-xs text-muted-foreground">
        {label} </div>

        
        <div className="mt-1 break-all font-medium">
            {value}
        </div>
    </div>
    );
    

}
