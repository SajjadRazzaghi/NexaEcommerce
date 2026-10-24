import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { ColumnDef } from '@tanstack/react-table';
import { Users } from 'lucide-react';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { PageHeader } from '@/components/data-states';
import { DataGrid, useDataGrid, DateCell } from '@/components/data-grid';

import type { AdminCustomer } from '@/modules/customers/types';
import { meta } from './meta';

export default function CustomersPage() {
    const { t, i18n } = useTranslation();

    useDocumentTitle(meta.title);

    const grid = useDataGrid<AdminCustomer>({
        endpoint: '/customers',
        queryKey: ['admin', 'customers'],
        defaultSort: {
            id: 'createdAt',
            desc: true,
        },
    });

    const locale =
        i18n.resolvedLanguage?.startsWith('fa')
            ? 'fa-IR'
            : undefined;

    const formatMoney = (value: number) =>
        `${new Intl.NumberFormat(locale, {
            maximumFractionDigits: 0,
        }).format(value)} IRR`;

    const columns: ColumnDef<AdminCustomer>[] = [
        {
            id: 'customer',
            accessorKey: 'displayName',
            header: t('customers.customer', {
                defaultValue: 'مشتری',
            }),
            cell: ({ row }) => {
                const customer = row.original;

                return (
                    <div className="min-w-0">
                        <Link
                            to={`/admin/customers/${customer.id}`}
                            className="font-medium underline-offset-4 hover:underline"
                        >
                            {customer.displayName?.trim() ||
                                customer.phoneNumber ||
                                customer.email ||
                                customer.id}
                        </Link>

                        {customer.email && (
                            <p className="mt-1 text-xs text-muted-foreground">
                                {customer.email}
                            </p>
                        )}
                    </div>
                );
            },
        },
        {
            id: 'phoneNumber',
            accessorKey: 'phoneNumber',
            header: t('customers.phone', {
                defaultValue: 'شماره تماس',
            }),
            cell: ({ row }) =>
                row.original.phoneNumber || '—',
        },
        {
            id: 'totalOrders',
            accessorKey: 'totalOrders',
            header: t('customers.orders', {
                defaultValue: 'تعداد سفارش‌ها',
            }),
            enableSorting: false,
        },
        {
            id: 'paidOrders',
            accessorKey: 'paidOrders',
            header: t('customers.paidOrders', {
                defaultValue: 'سفارش‌های پرداخت‌شده',
            }),
            enableSorting: false,
        },
        {
            id: 'totalSpent',
            accessorKey: 'totalSpent',
            header: t('customers.totalSpent', {
                defaultValue: 'جمع خریدهای پرداخت‌شده',
            }),
            enableSorting: false,
            cell: ({ row }) =>
                formatMoney(row.original.totalSpent),
        },
        {
            id: 'lastOrderAt',
            accessorKey: 'lastOrderAt',
            header: t('customers.lastOrder', {
                defaultValue: 'آخرین سفارش',
            }),
            enableSorting: false,
            cell: ({ row }) =>
                row.original.lastOrderAt
                    ? new Date(
                        row.original.lastOrderAt,
                    ).toLocaleDateString(locale)
                    : '—',
        },
        {
            id: 'createdAt',
            accessorKey: 'createdAt',
            header: t('fields.joined', {
                defaultValue: 'تاریخ عضویت',
            }),
            cell: ({ row }) => (
                <DateCell value={row.original.createdAt} />
            ),
        },
    ];

    return (
        <div className="grid gap-4">
            <PageHeader
                title={t('nav.customers', {
                    defaultValue: 'مشتریان',
                })}
                description={t('customers.description', {
                    defaultValue:
                        'اطلاعات تماس، سفارش‌ها و سابقه خرید مشتریان',
                })}
            />

            <DataGrid
                grid={grid}
                columns={columns}
                getRowId={(customer) => customer.id}
                searchPlaceholder={t(
                    'customers.searchPlaceholder',
                    {
                        defaultValue:
                            'جست‌وجوی نام، شماره موبایل یا ایمیل',
                    },
                )}
                viewKey="customers"
                empty={{
                    icon: Users,
                    title: t('customers.empty', {
                        defaultValue: 'مشتری‌ای پیدا نشد.',
                    }),
                    description: t('customers.emptyDescription', {
                        defaultValue:
                            'عبارت جست‌وجو را تغییر دهید یا بعداً دوباره بررسی کنید.',
                    }),
                }}
            />
        </div>
    );
}