import {
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
    Package,
    SearchX,
} from 'lucide-react';

import {
    useOrders,
} from '@/modules/orders/hooks/useOrders';

import type {
    OrderStatus,
} from '@/modules/orders/types';

const PAGE_SIZE = 10;

const statuses: Array<
    OrderStatus | ''
> = [
        '',
        'PendingPayment',
        'Paid',
        'Processing',
        'Shipped',
        'Delivered',
        'Cancelled',
    ];

function statusLabel(
    status: OrderStatus,
    isFa: boolean,
) {
    const labels:
        Record<
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
) {
    switch (status) {
        case 'Cancelled':
            return 'border-destructive/40 text-destructive';

        case 'Delivered':
            return 'border-emerald-500/40 text-emerald-600 dark:text-emerald-400';

        case 'Shipped':
            return 'border-blue-500/40 text-blue-600 dark:text-blue-400';

        case 'Processing':
            return 'border-amber-500/40 text-amber-600 dark:text-amber-400';

        case 'Paid':
            return 'border-primary/40 text-primary';

        default:
            return 'border-border text-muted-foreground';
    }
}

export default function OrdersPage() {
    const { i18n } =
        useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const [page, setPage] =
        useState(1);

    const [status, setStatus] =
        useState<
            OrderStatus | ''
        >('');

    const {
        data,
        isLoading,
        isError,
        isFetching,
        refetch,
    } =
        useOrders(
            page,
            PAGE_SIZE,
            status || undefined,
        );

    const text = isFa
        ? {
            title: 'سفارش‌های من',
            subtitle:
                'سوابق سفارش‌ها و وضعیت ارسال را مشاهده کنید.',
            filter: 'فیلتر وضعیت',
            all: 'همه وضعیت‌ها',
            loading: 'در حال بارگذاری سفارش‌ها...',
            error:
                'دریافت سفارش‌ها با مشکل مواجه شد.',
            retry: 'تلاش دوباره',
            empty:
                'هنوز سفارشی ثبت نکرده‌اید.',
            shop:
                'شروع خرید',
            items: 'آیتم',
            total: 'مبلغ نهایی',
            view: 'مشاهده سفارش',
            previous: 'قبلی',
            next: 'بعدی',
            page: 'صفحه',
        }
        : {
            title: 'My Orders',
            subtitle:
                'View your order history and fulfillment status.',
            filter: 'Filter by status',
            all: 'All statuses',
            loading: 'Loading your orders...',
            error:
                'We could not load your orders.',
            retry: 'Try again',
            empty:
                'You have not placed any orders yet.',
            shop:
                'Start shopping',
            items: 'items',
            total: 'Total',
            view: 'View order',
            previous: 'Previous',
            next: 'Next',
            page: 'Page',
        };

    return (
        <div
            className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <header>
                <h1 className="text-3xl font-bold tracking-tight">
                    {text.title}
                </h1>

                <p className="mt-2 text-sm text-muted-foreground">
                    {text.subtitle}
                </p>
            </header>

            <section className="rounded-2xl border bg-card p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-2 text-sm font-medium">
                        <Package className="size-4" />

                        {text.filter}
                    </div>

                    <select
                        value={
                            status
                        }
                        onChange={event => {
                            setPage(1);

                            setStatus(
                                event.target.value as OrderStatus | '',
                            );
                        }}
                        className="w-full rounded-lg border bg-background px-3 py-2 text-sm sm:w-64"
                    >
                        {statuses.map(
                            value => (
                                <option
                                    key={
                                        value ||
                                        'all'
                                    }
                                    value={
                                        value
                                    }
                                >
                                    {value
                                        ? statusLabel(
                                            value,
                                            isFa,
                                        )
                                        : text.all}
                                </option>
                            ),
                        )}
                    </select>
                </div>
            </section>

            {isLoading && (
                <div className="space-y-4">
                    {[1, 2, 3].map(
                        item => (
                            <div
                                key={
                                    item
                                }
                                className="animate-pulse rounded-2xl border p-5"
                            >
                                <div className="h-5 w-40 rounded bg-muted" />

                                <div className="mt-4 h-4 w-56 rounded bg-muted" />

                                <div className="mt-4 h-4 w-32 rounded bg-muted" />
                            </div>
                        ),
                    )}
                </div>
            )}

            {isError && (
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <p className="font-medium text-destructive">
                        {text.error}
                    </p>

                    <button
                        type="button"
                        onClick={() =>
                            void refetch()
                        }
                        className="mt-5 rounded-lg border px-4 py-2 text-sm font-medium"
                    >
                        {text.retry}
                    </button>
                </div>
            )}

            {!isLoading &&
                !isError &&
                data?.items.length === 0 && (
                    <div className="rounded-2xl border border-dashed p-12 text-center">
                        <SearchX className="mx-auto size-10 text-muted-foreground" />

                        <h2 className="mt-4 text-xl font-semibold">
                            {text.empty}
                        </h2>

                        <Link
                            to="/products"
                            className="mt-6 inline-flex rounded-lg border px-5 py-3 text-sm font-medium"
                        >
                            {text.shop}
                        </Link>
                    </div>
                )}

            {!isLoading &&
                !isError &&
                data &&
                data.items.length > 0 && (
                    <>
                        {isFetching && (
                            <div className="text-xs text-muted-foreground">
                                ...
                            </div>
                        )}

                        <div className="space-y-4">
                            {data.items.map(
                                order => (
                                    <Link
                                        key={
                                            order.id
                                        }
                                        to={`/orders/${order.id}`}
                                        className="block rounded-2xl border bg-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm"
                                    >
                                        <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
                                            <div>
                                                <div className="flex flex-wrap items-center gap-3">
                                                    <span className="font-semibold">
                                                        {
                                                            order.orderNumber
                                                        }
                                                    </span>

                                                    <span
                                                        className={`rounded-full border px-3 py-1 text-xs font-medium ${statusClass(
                                                            order.status,
                                                        )}`}
                                                    >
                                                        {statusLabel(
                                                            order.status,
                                                            isFa,
                                                        )}
                                                    </span>
                                                </div>

                                                <div className="mt-2 text-sm text-muted-foreground">
                                                    {new Date(
                                                        order.createdAt,
                                                    ).toLocaleString(
                                                        isFa
                                                            ? 'fa-IR'
                                                            : undefined,
                                                    )}
                                                </div>

                                                <div className="mt-2 text-sm text-muted-foreground">
                                                    {
                                                        order.itemCount
                                                    }{' '}
                                                    {text.items}
                                                </div>
                                            </div>

                                            <div className="flex items-center justify-between gap-5 lg:justify-end">
                                                <div className="text-end">
                                                    <div className="text-xs text-muted-foreground">
                                                        {text.total}
                                                    </div>

                                                    <div className="mt-1 text-lg font-bold">
                                                        {order.totalAmount.toLocaleString(
                                                            isFa
                                                                ? 'fa-IR'
                                                                : undefined,
                                                        )}{' '}
                                                        {
                                                            order.currency
                                                        }
                                                    </div>
                                                </div>

                                                <span className="rounded-lg border px-3 py-2 text-sm font-medium">
                                                    {text.view}
                                                </span>
                                            </div>
                                        </div>
                                    </Link>
                                ),
                            )}
                        </div>

                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <div className="text-sm text-muted-foreground">
                                {text.page}{' '}
                                {data.page}{' '}
                                /{' '}
                                {Math.max(
                                    data.totalPages,
                                    1,
                                )}
                            </div>

                            <div className="flex gap-2">
                                <button
                                    type="button"
                                    disabled={
                                        !data.hasPrevious
                                    }
                                    onClick={() =>
                                        setPage(
                                            value =>
                                                Math.max(
                                                    1,
                                                    value - 1,
                                                ),
                                        )
                                    }
                                    className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-sm disabled:opacity-50"
                                >
                                    <ChevronLeft className="size-4" />

                                    {text.previous}
                                </button>

                                <button
                                    type="button"
                                    disabled={
                                        !data.hasNext
                                    }
                                    onClick={() =>
                                        setPage(
                                            value =>
                                                value + 1,
                                        )
                                    }
                                    className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-sm disabled:opacity-50"
                                >
                                    {text.next}

                                    <ChevronRight className="size-4" />
                                </button>
                            </div>
                        </div>
                    </>
                )}
        </div>
    );
}