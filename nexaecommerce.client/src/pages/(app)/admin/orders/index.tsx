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
    Eye,
    Filter,
    Package,
    Search,
  
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
    isFa: boolean,
) {
    if (!status) {
        return isFa
            ? 'همه وضعیت‌ها'
            : 'All statuses';
    }

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

export default function AdminOrdersPage() {
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

    const [search, setSearch] =
        useState('');

    const query = useMemo(
        () => ({
            page,
            pageSize:
                PAGE_SIZE,
            status,
            search,
        }),
        [
            page,
            search,
            status,
        ],
    );

    const {
        data,
        isLoading,
        isFetching,
        isError,
        error,
    } =
        useAdminOrders(
            query,
        );

    const text = isFa
        ? {
              title:
                  'مدیریت سفارش‌ها',
              description:
                  'مدیریت سفارش‌ها، وضعیت پرداخت و ارسال.',
              search:
                  'جستجوی سفارش...',
              filter:
                  'وضعیت',
              loading:
                  'در حال بارگذاری سفارش‌ها...',
              error:
                  'خطا در دریافت سفارش‌ها.',
              empty:
                  'سفارشی پیدا نشد.',
              order:
                  'سفارش',
              customer:
                  'مشتری',
              status:
                  'وضعیت',
              total:
                  'مبلغ',
              date:
                  'تاریخ',
              actions:
                  'عملیات',
              view:
                  'مشاهده',
              page:
                  'صفحه',
              previous:
                  'قبلی',
              next:
                  'بعدی',
              items:
                  'مورد',
              processing:
                  'در حال دریافت...',
          }
        : {
              title:
                  'Order Management',
              description:
                  'Manage orders, payment status and fulfillment.',
              search:
                  'Search orders...',
              filter:
                  'Status',
              loading:
                  'Loading orders...',
              error:
                  'Failed to load orders.',
              empty:
                  'No orders found.',
              order:
                  'Order',
              customer:
                  'Customer',
              status:
                  'Status',
              total:
                  'Total',
              date:
                  'Date',
              actions:
                  'Actions',
              view:
                  'View',
              page:
                  'Page',
              previous:
                  'Previous',
              next:
                  'Next',
              items:
                  'items',
              processing:
                  'Refreshing...',
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

                <p className="text-muted-foreground mt-1">
                    {
                        text.description
                    }
                </p>
            </header>

            <section className="rounded-xl border p-4">
                <div className="flex flex-col gap-3 lg:flex-row">
                    <div className="relative flex-1">
                        <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />

                        <input
                            value={
                                search
                            }
                            onChange={event => {
                                setPage(
                                    1,
                                );

                                setSearch(
                                    event
                                        .target
                                        .value,
                                );
                            }}
                            placeholder={
                                text.search
                            }
                            className="w-full rounded-lg border bg-background py-2.5 ps-9 pe-3 text-sm outline-none focus:ring-2"
                        />
                    </div>

                    <div className="flex items-center gap-2 lg:w-64">
                        <Filter className="text-muted-foreground size-4" />

                        <select
                            value={
                                status
                            }
                            onChange={event => {
                                setPage(
                                    1,
                                );

                                setStatus(
                                    event
                                        .target
                                        .value as OrderStatus | '',
                                );
                            }}
                            className="w-full rounded-lg border bg-background px-3 py-2.5 text-sm"
                        >
                            {statuses.map(
                                value => (
                                    <option
                                        key={
                                            value
                                        }
                                        value={
                                            value
                                        }
                                    >
                                        {statusLabel(
                                            value,
                                            isFa,
                                        )}
                                    </option>
                                ),
                            )}
                        </select>
                    </div>
                </div>

                {isFetching &&
                    !isLoading && (
                        <div className="text-muted-foreground mt-3 text-xs">
                            {
                                text.processing
                            }
                        </div>
                    )}
            </section>

            {isLoading && (
                <div className="rounded-xl border p-8 text-center text-sm">
                    {
                        text.loading
                    }
                </div>
            )}

            {isError && (
                <div className="rounded-xl border border-destructive/40 p-8 text-center text-sm text-destructive">
                    {error instanceof
                    Error
                        ? error.message
                        : text.error}
                </div>
            )}

            {!isLoading &&
                !isError &&
                data &&
                data.items.length ===
                    0 && (
                    <div className="rounded-xl border border-dashed p-12 text-center">
                        <Package className="text-muted-foreground mx-auto size-10" />

                        <h2 className="mt-4 font-semibold">
                            {text.empty}
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

                                        <th className="px-4 py-3 text-start font-medium">
                                            {
                                                text.status
                                            }
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
                                                <td className="px-4 py-4">
                                                    <Link
                                                        to={`/ admin / orders / ${ order.id } `}
                                                        className="font-medium underline-offset-4 hover:underline"
                                                    >
                                                        {
                                                            order.orderNumber
                                                        }
                                                    </Link>
                                                </td>

                                                <td className="px-4 py-4">
                                                    <div className="max-w-[220px] truncate">
                                                        {
                                                            order.userId
                                                        }
                                                    </div>
                                                </td>

                                                <td className="px-4 py-4">
                                                    <span className="rounded-full border px-2.5 py-1 text-xs">
                                                        {statusLabel(
                                                            order.status,
                                                            isFa,
                                                        )}
                                                    </span>
                                                </td>

                                                <td className="px-4 py-4 font-medium">
                                                    {order.totalAmount.toLocaleString()}{' '}
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
                                                        to={`/ admin / orders / ${ order.id } `}
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
                            <div className="text-muted-foreground text-sm">
                                {
                                    text.page
                                }{' '}
                                {data.page}{' '}
                                /{' '}
                                {
                                    data.totalPages
                                }
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
