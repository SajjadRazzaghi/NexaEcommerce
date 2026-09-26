import {
    Check,
    ExternalLink,
    Package,
    Truck,
   } from 'lucide-react';
import {
    useQuery,
} from '@tanstack/react-query';
import {
    warehousesApi,
} from '@/modules/inventory/api/warehouses';
import {
    useState,
} from 'react';

import {
    useTranslation,
} from 'react-i18next';

import {
    useNavigate,
    useParams,
} from 'react-router-dom';

import {
    useAdminOrder,
} from '@/modules/orders/hooks/useAdminOrders';

import {
    useAdminShipment,
    useShipmentMutations,
} from '@/modules/orders/hooks/useShipment';

import {
    useFulfillment,
    useFulfillmentMutations,
} from '@/modules/orders/hooks/useFulfillment';

function getErrorMessage(
    error: unknown,
): string {
    const value =
        error as {
            response?: {
                data?: {
                    error?: string;
                    message?: string;
                };
            };
            message?: string;
        };

    return (
        value.response?.data?.error ??
        value.response?.data?.message ??
        value.message ??
        ''
    );
}

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
        useAdminOrder(
            id,
        );

    const {
        data: shipment,
        isLoading:
        shipmentLoading,
    } = useAdminShipment(
        id,
    );

    const {
        data: fulfillment,
        isLoading:
        fulfillmentLoading,
    } =
        useFulfillment(
            id,
        );
    const warehouseId =
        fulfillment?.warehouseId ??
        undefined;

    const {
        data: warehouseList,
        isLoading:
        warehouseListLoading,
    } =
        useQuery({
            queryKey: [
                'admin',
                'inventory',
                'warehouses',
            ],
            queryFn: async () =>
                (
                    await warehousesApi.list(
                        true,
                    )
                ),
            enabled:
                Boolean(
                    warehouseId,
                ),
        });

    const {
        data: warehouseLocations,
        isLoading:
        warehouseLocationsLoading,
    } =
        useQuery({
            queryKey: [
                'admin',
                'inventory',
                'warehouses',
                warehouseId,
                'locations',
            ],
            queryFn: async () =>
                (
                    await warehousesApi.getLocations(
                        warehouseId!,
                        true,
                    )
                ),
            enabled:
                Boolean(
                    warehouseId,
                ),
        });

    const allocatedWarehouse =
        warehouseList?.items?.find(
            warehouse =>
                warehouse.id ===
                fulfillment?.warehouseId,
        );

    const pickingLocation =
        warehouseLocations?.items?.find(
            location =>
                location.id ===
                fulfillment?.pickingLocationId,
        );
    const {
        updateTracking,
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
        rollback:
        rollbackMutation,
    } =
        useFulfillmentMutations(
            id ?? '',
        );

    const [
        trackingOverride,
        setTrackingOverride,
    ] =
        useState<string | null>(
            null,
        );

    const [
        operationError,
        setOperationError,
    ] =
        useState<string | null>(
            null,
        );

    const text = isFa
        ? {
            warehouse:
                'انبار',
            pickingLocation:
                'موقعیت برداشت',
            loadingWarehouse:
                'در حال دریافت نام انبار...',
            loadingPickingLocation:
                'در حال دریافت موقعیت برداشت...',
            operationError:
                'عملیات انجام نشد.',
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
                'شرکت حمل',
            tracking:
                'کد رهگیری',
            saveTracking:
                'ذخیره کد رهگیری',
            shipmentPending:
                'مرسوله آماده است؛ کد رهگیری را ثبت کنید.',
            shipmentAuto:
                'مرسوله به‌صورت خودکار از روش ارسال انتخاب‌شده در checkout ساخته می‌شود.',
            shipmentMissing:
                'پس از آماده‌شدن سفارش، مرسوله باید به‌صورت خودکار ساخته شود.',
            ship:
                'ارسال سفارش',
            deliver:
                'ثبت تحویل',
            shippingNow:
                'در حال ارسال...',
            delivering:
                'در حال ثبت تحویل...',
            shipmentConfig:
                'مدیریت روش‌های ارسال',
            total:
                'مبلغ کل',
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
            rollback:
                'بازگشت به مرحله قبل',
            rollbackTitle:
                'بازگشت مرحله',
            rollbackReadyToShip:
                'بازگشت از «آماده ارسال» به «بسته‌بندی شده»',
            rollbackPacked:
                'بازگشت از «بسته‌بندی شده» به «در حال بسته‌بندی»',
            rollbackPacking:
                'بازگشت از «در حال بسته‌بندی» به «جمع‌آوری شده»',
            rollbackPicked:
                'بازگشت از «جمع‌آوری شده» به «در حال جمع‌آوری»',
            rollbackPicking:
                'بازگشت از «در حال جمع‌آوری» به «در انتظار عملیات»',
            rollbackConfirm:
                'آیا از بازگشت سفارش به مرحله قبل مطمئن هستید؟',
            cancel:
                'انصراف',
            confirm:
                'تأیید بازگشت',
            rollingBack:
                'در حال بازگردانی...',
            rollbackDescription:
                'این عملیات وضعیت مراحل انبار را یک مرحله به عقب برمی‌گرداند و اطلاعات مرتبط را نیز هماهنگ می‌کند.',
           
        }
        : {
            warehouse:
                'Warehouse',
            pickingLocation:
                'Picking location',
            loadingWarehouse:
                'Loading warehouse name...',
            loadingPickingLocation:
                'Loading picking location...',
            operationError:
                'The operation failed.',
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
            saveTracking:
                'Save tracking number',
            shipmentPending:
                'Shipment is ready. Enter the tracking number.',
            shipmentAuto:
                'The shipment is prepared automatically from the shipping method selected at checkout.',
            shipmentMissing:
                'The shipment should be created automatically once the order is ready to ship.',
            ship:
                'Ship order',
            deliver:
                'Mark delivered',
            shippingNow:
                'Shipping...',
            delivering:
                'Marking delivered...',
            shipmentConfig:
                'Manage shipping methods',
            total:
                'Total',
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
            rollback:
                'Rollback to previous stage',
            rollbackTitle:
                'Rollback stage',
            rollbackReadyToShip:
                'Rollback from "Ready to ship" to "Packed"',
            rollbackPacked:
                'Rollback from "Packed" to "Packing"',
            rollbackPacking:
                'Rollback from "Packing" to "Picked"',
            rollbackPicked:
                'Rollback from "Picked" to "Picking"',
            rollbackPicking:
                'Rollback from "Picking" to "Pending"',
            rollbackConfirm:
                'Are you sure you want to roll the order back to the previous stage?',
            cancel:
                'Cancel',
            confirm:
                'Confirm rollback',
            rollingBack:
                'Rolling back...',
            rollbackDescription:
                'This operation moves the warehouse workflow back by one stage and synchronizes the related data.',
        };

    const isFulfillmentBusy =
        startFulfillmentMutation.isPending ||
        allocateMutation.isPending ||
        reserveMutation.isPending ||
        pickingMutation.isPending ||
        pickedMutation.isPending ||
        packingMutation.isPending ||
        packedMutation.isPending ||
        readyToShipMutation.isPending ||
        rollbackMutation.isPending;

    const currentTracking =
        trackingOverride ??
        shipment?.trackingNumber ??
        '';

    const runOperation =
        async (
            action: () => Promise<unknown>,
        ) => {
            setOperationError(
                null,
            );

            try {
                await action();
            } catch (error) {
                setOperationError(
                    getErrorMessage(
                        error,
                    ) ||
                    text.operationError,
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
                <div className="rounded-xl border border-destructive/30 p-10 text-center">
                    {
                        text.notFound
                    }
                </div>
            </div>
        );
    }

    const canSetTracking =
        shipment?.status ===
        'Pending' &&
        fulfillment?.status ===
        'ReadyToShip';

    const canShip =
        shipment?.status ===
        'Pending' &&
        Boolean(
            currentTracking.trim(),
        ) &&
        fulfillment?.status ===
        'ReadyToShip' &&
        order.status ===
        'Processing';

    const canDeliver =
        shipment?.status ===
        'Shipped' &&
        fulfillment?.status ===
        'Shipped' &&
        order.status ===
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
            <header className="flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-semibold">
                        {
                            text.order
                        }{' '}
                        {
                            order.orderNumber
                        }
                    </h1>

                    <p className="mt-1 text-sm text-muted-foreground">
                        {
                            order.shippingFullName
                        }{' '}
                        ·{' '}
                        {
                            order.shippingPhone
                        }
                    </p>
                </div>

                <div className="flex flex-wrap gap-2">
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

                    <button
                        type="button"
                        onClick={() =>
                            navigate(
                                '/admin/shipping-methods',
                            )
                        }
                        className="inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm"
                    >
                        <ExternalLink className="size-4" />

                        {
                            text.shipmentConfig
                        }
                    </button>
                </div>
            </header>

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

                        {operationError && (
                            <div
                                role="alert"
                                className="mt-4 rounded-lg border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive"
                            >
                                {
                                    operationError
                                }
                            </div>
                        )}

                        {(!fulfillment ||
                            (fulfillment.status ===
                                'Pending' &&
                                order.status ===
                                'Paid')) && (
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
                                                void runOperation(
                                                    () =>
                                                        startFulfillmentMutation.mutateAsync(),
                                                )
                                            }
                                            className="inline-flex w-fit items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
                                        >
                                            <Truck className="size-4" />

                                            {
                                                startFulfillmentMutation.isPending
                                                    ? text.loading
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
                                            warehouseListLoading
                                                ? text.loadingWarehouse
                                                : allocatedWarehouse
                                                    ? `${allocatedWarehouse.name}${allocatedWarehouse.code
                                                        ? ` (${allocatedWarehouse.code})`
                                                        : ''
                                                    }`
                                                    : '—'
                                        }
                                    />

                                    <Info
                                        label={
                                            text.pickingLocation
                                        }
                                        value={
                                            warehouseLocationsLoading
                                                ? text.loadingPickingLocation
                                                : pickingLocation
                                                    ? `${pickingLocation.name}${pickingLocation.code
                                                        ? ` (${pickingLocation.code})`
                                                        : ''
                                                    }`
                                                    : '—'
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
                                                    void runOperation(
                                                        () =>
                                                            allocateMutation.mutateAsync(),
                                                    )
                                                }
                                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                            >
                                                {
                                                    allocateMutation.isPending
                                                        ? text.loading
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
                                                    void runOperation(
                                                        () =>
                                                            reserveMutation.mutateAsync(),
                                                    )
                                                }
                                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                            >
                                                {
                                                    reserveMutation.isPending
                                                        ? text.loading
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
                                                    void runOperation(
                                                        () =>
                                                            pickingMutation.mutateAsync(),
                                                    )
                                                }
                                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                            >
                                                {
                                                    pickingMutation.isPending
                                                        ? text.loading
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
                                                    void runOperation(
                                                        () =>
                                                            pickedMutation.mutateAsync(),
                                                    )
                                                }
                                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                            >
                                                {
                                                    pickedMutation.isPending
                                                        ? text.loading
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
                                                    void runOperation(
                                                        () =>
                                                            packingMutation.mutateAsync(),
                                                    )
                                                }
                                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                            >
                                                {
                                                    packingMutation.isPending
                                                        ? text.loading
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
                                                    void runOperation(
                                                        () =>
                                                            packedMutation.mutateAsync(),
                                                    )
                                                }
                                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                                            >
                                                {
                                                    packedMutation.isPending
                                                        ? text.loading
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
                                                    void runOperation(
                                                        () =>
                                                            readyToShipMutation.mutateAsync(),
                                                    )
                                                }
                                                className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
                                            >
                                                <Truck className="size-4" />

                                                {
                                                    readyToShipMutation.isPending
                                                        ? text.loading
                                                        : text.ready
                                                }
                                            </button>
                                        )}
                                </div>
                            </div>
                        )}
                    </section>

                    <section className="rounded-xl border p-6">
                        <div className="flex items-center justify-between gap-4">
                            <h2 className="font-semibold">
                                {
                                    text.items
                                }
                            </h2>
                        </div>

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
                                            {
                                                item.lineTotal.toLocaleString()
                                            }{' '}
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
                                {
                                    order.totalAmount.toLocaleString()
                                }{' '}
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

                        {!shipment && (
                            <div className="mt-5 rounded-xl border border-dashed p-5">
                                <p className="text-sm text-muted-foreground">
                                    {
                                        fulfillment?.status ===
                                            'ReadyToShip'
                                            ? text.shipmentMissing
                                            : text.shipmentAuto
                                    }
                                </p>
                            </div>
                        )}

                        {shipment && (
                            <div className="mt-5 grid gap-5">
                                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
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
                                            text.status
                                        }
                                        value={
                                            shipment.status
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
                                </div>

                                {canSetTracking && (
                                    <div className="grid gap-3 rounded-xl border bg-muted/20 p-4">
                                        <div>
                                            <div className="font-medium">
                                                {
                                                    text.shipmentPending
                                                }
                                            </div>

                                            <div className="mt-1 text-sm text-muted-foreground">
                                                {
                                                    text.shipmentAuto
                                                }
                                            </div>
                                        </div>

                                        <div className="flex flex-col gap-2 sm:flex-row">
                                            <input
                                                value={
                                                    currentTracking
                                                }
                                                onChange={
                                                    event =>
                                                        setTrackingOverride(
                                                            event
                                                                .target
                                                                .value,
                                                        )
                                                }
                                                maxLength={
                                                    200
                                                }
                                                placeholder={
                                                    text.tracking
                                                }
                                                className="min-w-0 flex-1 rounded-lg border bg-background px-3 py-2.5 text-sm"
                                            />

                                            <button
                                                type="button"
                                                disabled={
                                                    updateTracking.isPending ||
                                                    !currentTracking.trim()
                                                }
                                                onClick={() =>
                                                    void runOperation(
                                                        async () => {
                                                            await updateTracking.mutateAsync({
                                                                trackingNumber:
                                                                    currentTracking.trim(),
                                                            });

                                                            setTrackingOverride(
                                                                null,
                                                            );
                                                        },
                                                    )
                                                }
                                                className="rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
                                            >
                                                {
                                                    updateTracking.isPending
                                                        ? text.loading
                                                        : text.saveTracking
                                                }
                                            </button>
                                        </div>
                                    </div>
                                )}

                                <div className="flex flex-wrap gap-2 border-t pt-4">
                                    {canShip && (
                                        <button
                                            type="button"
                                            disabled={
                                                ship.isPending
                                            }
                                            onClick={() =>
                                                void runOperation(
                                                    () =>
                                                        ship.mutateAsync(),
                                                )
                                            }
                                            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
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
                                                void runOperation(
                                                    () =>
                                                        deliver.mutateAsync(),
                                                )
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
    return (
        <div>
            <div className="text-xs text-muted-foreground">
                {
                    label
                }
            </div>

            <div className="mt-1 break-all font-medium">
                {
                    value
                }
            </div>
        </div>
    );
}