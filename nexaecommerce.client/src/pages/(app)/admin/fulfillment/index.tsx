import {
    useMemo,
    useState,
} from 'react';

import {
    useTranslation,
} from 'react-i18next';

import {
    Link,
} from 'react-router-dom';

import {
    ChevronLeft,
    ChevronRight,
    ExternalLink,
    Package,
    RefreshCw,
} from 'lucide-react';

import {
    useFulfillmentQueue,
} from '@/modules/orders/hooks/useFulfillmentQueue';

import type {
    FulfillmentStatus,
} from '@/modules/orders/api/fulfillmentQueueApi';

const PAGE_SIZE = 30;

function statusLabel(
    status: FulfillmentStatus,
    isFa: boolean,
) {
    const labels: Record<
        FulfillmentStatus,
        {
            en: string;
            fa: string;
        }
    > = {
        Pending: {
            en: 'Pending',
            fa: 'در انتظار عملیات',
        },

        Picking: {
            en: 'Picking',
            fa: 'در حال جمع‌آوری',
        },

        Picked: {
            en: 'Picked',
            fa: 'جمع‌آوری شده',
        },

        Packing: {
            en: 'Packing',
            fa: 'در حال بسته‌بندی',
        },

        Packed: {
            en: 'Packed',
            fa: 'بسته‌بندی شده',
        },

        ReadyToShip: {
            en: 'Ready to ship',
            fa: 'آماده ارسال',
        },
    };

    return isFa
        ? labels[status].fa
        : labels[status].en;
}

function statusClass(
    status: FulfillmentStatus,
) {
    switch (status) {
        case 'Pending':
            return 'border-border text-muted-foreground';

        case 'Picking':
            return 'border-amber-500/40 text-amber-600 dark:text-amber-400';

        case 'Picked':
            return 'border-cyan-500/40 text-cyan-600 dark:text-cyan-400';

        case 'Packing':
            return 'border-orange-500/40 text-orange-600 dark:text-orange-400';

        case 'Packed':
            return 'border-blue-500/40 text-blue-600 dark:text-blue-400';

        case 'ReadyToShip':
            return 'border-emerald-500/40 text-emerald-600 dark:text-emerald-400';

        default:
            return 'border-border text-muted-foreground';
    }
}

export default function AdminFulfillmentPage() {
    const { i18n } =
        useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const [
        page,
        setPage,
    ] = useState(1);

    const skip =
        useMemo(
            () =>
                (page - 1) *
                PAGE_SIZE,
            [page],
        );

    const {
        data,
        isLoading,
        isFetching,
        isError,
        error,
        refetch,
    } =
        useFulfillmentQueue(
            skip,
            PAGE_SIZE,
        );

    const text = isFa
        ? {
            title:
                'صف عملیات انبار',
            description:
                'سفارش‌های فعال در فرایند جمع‌آوری، بسته‌بندی و آماده‌سازی ارسال.',
            refresh:
                'به‌روزرسانی',
            loading:
                'در حال بارگذاری صف عملیات...',
            error:
                'دریافت صف عملیات با مشکل مواجه شد.',
            retry:
                'تلاش دوباره',
            empty:
                'در حال حاضر سفارشی در صف عملیات وجود ندارد.',
            order:
                'سفارش',
            customer:
                'مشتری',
            status:
                'مرحله',
            items:
                'اقلام',
            total:
                'مبلغ',
            city:
                'شهر',
            warehouse:
                'انبار',
            action:
                'عملیات',
            open:
                'ادامه عملیات',
            page:
                'صفحه',
            previous:
                'قبلی',
            next:
                'بعدی',
            updated:
                'به‌روزرسانی خودکار هر ۵ ثانیه',
        }
        : {
            title:
                'Fulfillment Queue',
            description:
                'Active orders waiting for picking, packing and shipment preparation.',
            refresh:
                'Refresh',
            loading:
                'Loading fulfillment queue...',
            error:
                'We could not load the fulfillment queue.',
            retry:
                'Try again',
            empty:
                'There are no orders waiting in fulfillment.',
            order:
                'Order',
            customer:
                'Customer',
            status:
                'Stage',
            items:
                'Items',
            total:
                'Total',
            city:
                'City',
            warehouse:
                'Warehouse',
            action:
                'Action',
            open:
                'Continue',
            page:
                'Page',
            previous:
                'Previous',
            next:
                'Next',
            updated:
                'Auto-refresh every 5 seconds',
        };

    return (
        <div
            className="grid gap-5"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                <div>
                    <h1 className="text-2xl font-semibold">
                        {text.title}
                    </h1>

                    <p className="mt-1 text-muted-foreground">
                        {text.description}
                    </p>
                </div>

                <div className="flex items-center gap-3">
                    <span className="text-xs text-muted-foreground">
                        {text.updated}
                    </span>

                    <button
                        type="button"
                        onClick={() =>
                            void refetch()
                        }
                        disabled={
                            isFetching
                        }
                        className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm disabled:opacity-50"
                    >
                        <RefreshCw
                            className={`size-4 ${isFetching
                                    ? 'animate-spin'
                                    : ''
                                }`}
                        />

                        {text.refresh}
                    </button>
                </div>
            </header>

            {isLoading && (
                <div className="space-y-3">
                    {[1, 2, 3, 4, 5].map(
                        item => (
                            <div
                                key={item}
                                className="animate-pulse rounded-xl border p-5"
                            >
                                <div className="h-5 w-48 rounded bg-muted" />

                                <div className="mt-4 h-4 w-72 rounded bg-muted" />

                                <div className="mt-4 h-4 w-32 rounded bg-muted" />
                            </div>
                        ),
                    )}
                </div>
            )}

            {isError && (
                <div className="rounded-xl border border-destructive/30 p-10 text-center">
                    <p className="text-sm text-destructive">
                        {error instanceof Error
                            ? error.message
                            : text.error}
                    </p>

                    <button
                        type="button"
                        onClick={() =>
                            void refetch()
                        }
                        className="mt-5 rounded-lg border px-4 py-2 text-sm"
                    >
                        {text.retry}
                    </button>
                </div>
            )}

            {!isLoading &&
                !isError &&
                data?.items.length ===
                0 && (
                    <div className="rounded-xl border border-dashed p-12 text-center">
                        <Package className="mx-auto size-10 text-muted-foreground" />

                        <h2 className="mt-4 font-semibold">
                            {text.empty}
                        </h2>
                    </div>
                )}

            {!isLoading &&
                !isError &&
                data &&
                data.items.length > 0 && (
                    <>
                        <div className="overflow-x-auto rounded-xl border">
                            <table className="w-full min-w-[1100px] text-sm">
                                <thead>
                                    <tr className="border-b bg-muted/30">
                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.order}
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.customer}
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.status}
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.items}
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.total}
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.city}
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {text.warehouse}
                                        </th>

                                        <th className="px-4 py-3 text-end font-medium">
                                            {text.action}
                                        </th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {data.items.map(
                                        item => (
                                            <tr
                                                key={
                                                    item.fulfillment.id
                                                }
                                                className="border-b last:border-b-0"
                                            >
                                                <td className="px-4 py-4">
                                                    <Link
                                                        to={`/admin/orders/${item.orderId}`}
                                                        className="font-medium underline-offset-4 hover:underline"
                                                    >
                                                        {
                                                            item.orderNumber
                                                        }
                                                    </Link>
                                                </td>

                                                <td className="px-4 py-4">
                                                    <div className="font-medium">
                                                        {
                                                            item.shippingFullName
                                                        }
                                                    </div>

                                                    <div className="mt-1 text-xs text-muted-foreground">
                                                        {
                                                            item.shippingPhone
                                                        }
                                                    </div>
                                                </td>

                                                <td className="px-4 py-4">
                                                    <span
                                                        className={`rounded-full border px-2.5 py-1 text-xs font-medium ${statusClass(
                                                            item
                                                                .fulfillment
                                                                .status,
                                                        )}`}
                                                    >
                                                        {
                                                            statusLabel(
                                                                item
                                                                    .fulfillment
                                                                    .status,
                                                                isFa,
                                                            )
                                                        }
                                                    </span>
                                                </td>

                                                <td className="px-4 py-4">
                                                    {
                                                        item.itemCount
                                                    }
                                                </td>

                                                <td className="px-4 py-4 font-medium">
                                                    {item.totalAmount.toLocaleString(
                                                        isFa
                                                            ? 'fa-IR'
                                                            : undefined,
                                                    )}{' '}
                                                    {
                                                        item.currency
                                                    }
                                                </td>

                                                <td className="px-4 py-4">
                                                    {
                                                        item.shippingCity
                                                    }
                                                </td>

                                                <td className="px-4 py-4">
                                                    {item
                                                        .fulfillment
                                                        .warehouseId ? (
                                                        <span className="font-mono text-xs">
                                                            {item.fulfillment.warehouseId.slice(
                                                                0,
                                                                8,
                                                            )}
                                                        </span>
                                                    ) : (
                                                        <span className="text-xs text-muted-foreground">
                                                            —
                                                        </span>
                                                    )}
                                                </td>

                                                <td className="px-4 py-4 text-end">
                                                    <Link
                                                        to={`/admin/orders/${item.orderId}`}
                                                        className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-medium"
                                                    >
                                                        <ExternalLink className="size-3.5" />

                                                        {
                                                            text.open
                                                        }
                                                    </Link>
                                                </td>
                                            </tr>
                                        ),
                                    )}
                                </tbody>
                            </table>
                        </div>

                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <div className="text-sm text-muted-foreground">
                                {text.page}{' '}
                                {page}
                            </div>

                            <div className="flex gap-2">
                                <button
                                    type="button"
                                    disabled={
                                        page <= 1
                                    }
                                    onClick={() =>
                                        setPage(
                                            current =>
                                                Math.max(
                                                    1,
                                                    current -
                                                    1,
                                                ),
                                        )
                                    }
                                    className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-sm disabled:opacity-50"
                                >
                                    <ChevronLeft className="size-4" />

                                    {
                                        text.previous
                                    }
                                </button>

                                <button
                                    type="button"
                                    disabled={
                                        data.items.length <
                                        PAGE_SIZE
                                    }
                                    onClick={() =>
                                        setPage(
                                            current =>
                                                current +
                                                1,
                                        )
                                    }
                                    className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-sm disabled:opacity-50"
                                >
                                    {
                                        text.next
                                    }

                                    <ChevronRight className="size-4" />
                                </button>
                            </div>
                        </div>
                    </>
                )}
        </div>
    );
}