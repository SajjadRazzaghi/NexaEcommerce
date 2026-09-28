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
    CheckCircle2,
    ChevronLeft,
    ChevronRight,
    Clock3,
    Eye,
    Filter,
    LoaderCircle,
    Package,
        Printer,
Search,
    Truck,
    XCircle,
} from 'lucide-react';

import {
    useAdminOrders,
} from '@/modules/orders/hooks/useAdminOrders';

import type {
    OrderStatus,
} from '@/modules/orders/types';

const PAGE_SIZE = 20;

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
    status: OrderStatus | '',
    t: (
        key: string,
    ) => string,
) {
    if (!status) {
        return t(
            'storefront.orders.allStatuses',
        );
    }

    return t(
        `storefront.orders.statuses.${status}`,
    );
}
function StatusIcon({
    status,
    className = 'size-3.5',
}: {
    status: OrderStatus;
    className?: string;
}) {
    switch (status) {
        case 'PendingPayment':
            return <Clock3 className={className} />;

        case 'Paid':
            return (
                <CheckCircle2
                    className={className}
                />
            );

        case 'Processing':
            return (
                <LoaderCircle
                    className={className}
                />
            );

        case 'Shipped':
            return (
                <Truck
                    className={className}
                />
            );

        case 'Delivered':
            return (
                <CheckCircle2
                    className={className}
                />
            );

        case 'Cancelled':
            return (
                <XCircle
                    className={className}
                />
            );

        default:
            return (
                <Package
                    className={className}
                />
            );
    }
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

export default function AdminOrdersPage() {
    const {
        i18n,
        t,
    } = useTranslation();

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

    const [search, setSearch] =
        useState('');

        const [
        selectedOrderIds,
        setSelectedOrderIds,
    ] =
        useState<Set<string>>(
            () => new Set(),
        );
const query =
        useMemo(
            () => ({
                page,
                pageSize:
                    PAGE_SIZE,
                status,
                search,
            }),
            [
                page,
                status,
                search,
            ],
        );

    const {
        data,
        isLoading,
        isFetching,
        isError,
        error,
        refetch,
    } =
        useAdminOrders(
            query,
        );
    const visibleOrderIds =
        data?.items.map(
            order => order.id,
        ) ??
        [];

    const allVisibleOrdersSelected =
        visibleOrderIds.length > 0 &&
        visibleOrderIds.every(
            orderId =>
                selectedOrderIds.has(
                    orderId,
                ),
        );

    const toggleOrderSelection =
        (orderId: string) => {
            setSelectedOrderIds(
                current => {
                    const next =
                        new Set(current);

                    if (
                        next.has(orderId)
                    ) {
                        next.delete(orderId);
                    } else {
                        next.add(orderId);
                    }

                    return next;
                },
            );
        };

    const toggleVisibleOrderSelection =
        () => {
            setSelectedOrderIds(
                current => {
                    const next =
                        new Set(current);

                    if (
                        allVisibleOrdersSelected
                    ) {
                        visibleOrderIds.forEach(
                            orderId =>
                                next.delete(
                                    orderId,
                                ),
                        );
                    } else {
                        visibleOrderIds.forEach(
                            orderId =>
                                next.add(
                                    orderId,
                                ),
                        );
                    }

                    return next;
                },
            );
        };

    const printSelectedOrders =
        () => {
            if (
                selectedOrderIds.size === 0
            ) {
                return;
            }

            const ids =
                Array.from(
                    selectedOrderIds,
                ).join(',');

            window.open(
                `/admin/orders/shipping-labels?ids=${encodeURIComponent(ids)}`,
                '_blank',
                'noopener,noreferrer',
            );
        };

    const text = {
        title: t(
            'storefront.orders.title',
        ),

        subtitle: t(
            'storefront.orders.subtitle',
        ),

        description: t(
            'storefront.orders.description',
        ),

        filter: t(
            'storefront.orders.filter',
        ),

        search: t(
            'storefront.orders.search',
        ),

        all: t(
            'storefront.orders.allStatuses',
        ),

        loading: t(
            'storefront.orders.loading',
        ),

        error: t(
            'storefront.orders.error',
        ),

        retry: t(
            'storefront.orders.retry',
        ),

        empty: t(
            'storefront.orders.empty',
        ),

        shop: t(
            'storefront.orders.shop',
        ),

        items: t(
            'storefront.orders.items',
        ),

        total: t(
            'storefront.orders.total',
        ),

        view: t(
            'storefront.orders.view',
        ),

        previous: t(
            'storefront.orders.previous',
        ),

        next: t(
            'storefront.orders.next',
        ),

        page: t(
            'storefront.orders.page',
        ),

        selected: t(
            'storefront.orders.selected',
        ),

        printLabels: t(
            'storefront.orders.printLabels',
        ),

        selectAll: t(
            'storefront.orders.selectAll',
        ),

        order: t(
            'storefront.orders.order',
        ),

        customer: t(
            'storefront.orders.customer',
        ),

        status: t(
            'storefront.orders.status',
        ),

        date: t(
            'storefront.orders.date',
        ),

        actions: t(
            'storefront.orders.actions',
        ),
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
            <header>
                <h1 className="text-2xl font-semibold">
                    {text.title}
                </h1>

                <p className="mt-1 text-muted-foreground">
                    {
                        text.description
                    }
                </p>
            </header>

            <section className="rounded-xl border p-4">
                <div className="flex flex-col gap-3 lg:flex-row">
                    <div className="relative flex-1">
                        <Search className="absolute start-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                        <input
                            value={
                                search
                            }
                            onChange={
                                event => {
                                    setPage(
                                        1,
                                    );

                                    setSearch(
                                        event
                                            .target
                                            .value,
                                    );
                                }
                            }
                            placeholder={
                                text.search
                            }
                            className="w-full rounded-lg border bg-background py-2.5 ps-9 pe-3 text-sm outline-none focus:ring-2"
                        />
                    </div>

                    <div className="flex items-center gap-2 lg:w-64">
                        <Filter className="size-4 text-muted-foreground" />

                        <select
                            value={
                                status
                            }
                            onChange={
                                event => {
                                    setPage(
                                        1,
                                    );

                                    setStatus(
                                        event
                                            .target
                                            .value as OrderStatus | '',
                                    );
                                }
                            }
                            className="w-full rounded-lg border bg-background px-3 py-2.5 text-sm"
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
                                        {statusLabel(
                                            value,
                                            t,
                                        )}
                                    </option>
                                ),
                            )}
                        </select>
                    </div>
                </div>

                {isFetching &&
                    !isLoading && (
                        <div className="mt-3 text-xs text-muted-foreground">
                            ...
                        </div>
                    )}
            </section>
            <section className="flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4">
                <div className="text-sm text-muted-foreground">
                    {selectedOrderIds.size} {text.selected}
                </div>

                <button
                    type="button"
                    disabled={
                        selectedOrderIds.size === 0
                    }
                    onClick={
                        printSelectedOrders
                    }
                    className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50"
                >
                    <Printer className="size-4" />
                    {text.printLabels}
                </button>
            </section>

            {isLoading && (
                <div className="space-y-3">
                    {[1, 2, 3, 4].map(
                        item => (
                            <div
                                key={
                                    item
                                }
                                className="animate-pulse rounded-xl border p-5"
                            >
                                <div className="h-5 w-40 rounded bg-muted" />

                                <div className="mt-4 h-4 w-64 rounded bg-muted" />

                                <div className="mt-4 h-4 w-28 rounded bg-muted" />
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
                        {
                            text.retry
                        }
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
                            {
                                text.empty
                            }
                        </h2>
                    </div>
                )}

            {!isLoading &&
                !isError &&
                data &&
                data.items.length >
                0 && (
                    <>
                        <div className="overflow-x-auto rounded-xl border">
                            <table className="w-full min-w-[900px] text-sm">
                                <thead>
                                    <tr className="border-b bg-muted/30">
                                        <th className="w-12 px-4 py-3 text-center">
    <input
        type="checkbox"
        checked={
            allVisibleOrdersSelected
        }
        onChange={
            toggleVisibleOrderSelection
        }
        disabled={
            visibleOrderIds.length === 0
        }
        aria-label={text.selectAll}
        title={text.selectAll}
        className="size-4 rounded border"
    />
</th>
<th className="px-4 py-3 text-start font-medium">
                                            {
                                                text.order
                                            }
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {
                                                text.customer
                                            }
                                        </th>

                                    <th className="whitespace-nowrap px-4 py-3 text-start font-medium">
                                        {text.status}
                                    </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {
                                                text.total
                                            }
                                        </th>

                                        <th className="px-4 py-3 text-start font-medium">
                                            {
                                                text.date
                                            }
                                        </th>

                                        <th className="px-4 py-3 text-end font-medium">
                                            {
                                                text.actions
                                            }
                                        </th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {data.items.map(
                                        order => (
                                            <tr
                                                key={
                                                    order.id
                                                }
                                                className="border-b last:border-b-0"
                                            >
                                                <td className="w-12 px-4 py-4 text-center align-top">
    <input
        type="checkbox"
        checked={
            selectedOrderIds.has(
                order.id,
            )
        }
        onChange={() =>
            toggleOrderSelection(
                order.id,
            )
        }
        aria-label={`${text.selectAll}: ${order.orderNumber}`}
        className="mt-1 size-4 rounded border"
    />
</td>
<td className="px-4 py-4">
                                                    <div className="grid gap-2">
                                                        <Link
                                                            to={`/admin/orders/${order.id}`}
                                                            className="w-fit whitespace-nowrap font-medium underline-offset-4 hover:underline"
                                                        >
                                                            {order.orderNumber}
                                                        </Link>

                                                        <span
                                                            className={`inline-flex w-fit items-center gap-1.5 rounded-full border px-2 py-1 text-[11px] font-medium md:hidden ${statusClass(
                                                                order.status,
                                                            )}`}
                                                        >
                                                            <StatusIcon
                                                                status={order.status}
                                                                className="size-3"
                                                            />

                                                            {statusLabel(
                                                                order.status,
                                                                t,
                                                            )}
                                                        </span>
                                                    </div>
                                                </td>

                                                <td className="px-4 py-4">
                                                    <div className="max-w-[220px] truncate">
                                                        {
                                                            order.userId
                                                        }
                                                    </div>
                                                </td>

                                                <td className="whitespace-nowrap px-4 py-4">
                                                    <span
                                                        className={`inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-1 text-xs font-medium ${statusClass(
                                                            order.status,
                                                        )}`}
                                                    >
                                                        <StatusIcon
                                                            status={order.status}
                                                        />

                                                        {statusLabel(
                                                            order.status,
                                                            t,
                                                        )}
                                                    </span>
                                                </td>

                                                <td className="px-4 py-4 font-medium">
                                                    {order.totalAmount.toLocaleString(
                                                        isFa
                                                            ? 'fa-IR'
                                                            : undefined,
                                                    )}{' '}
                                                    {
                                                        order.currency
                                                    }
                                                </td>

                                                <td className="px-4 py-4 text-muted-foreground">
                                                    {new Date(
                                                        order.createdAt,
                                                    ).toLocaleString(
                                                        isFa
                                                            ? 'fa-IR'
                                                            : undefined,
                                                    )}
                                                </td>

                                                <td className="px-4 py-4 text-end">
                                                    <Link
                                                        to={`/admin/orders/${order.id}`}
                                                        className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-xs font-medium"
                                                    >
                                                        <Eye className="size-3.5" />

                                                        {
                                                            text.view
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
                                {
                                    text.page
                                }{' '}
                                {
                                    data.page
                                }{' '}
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
                                                    value -
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
                                        !data.hasNext
                                    }
                                    onClick={() =>
                                        setPage(
                                            value =>
                                                value +
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