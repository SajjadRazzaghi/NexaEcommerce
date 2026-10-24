import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Mail, MapPin, Phone, ShoppingBag } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { PageHeader } from '@/components/data-states';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';

import { adminCustomersApi } from '@/modules/customers/api/adminCustomersApi';

export default function AdminCustomerDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { t, i18n } = useTranslation();
    const [orderPage, setOrderPage] = useState(1);

    const canManageOrders = usePermission(PERM.ordersManage);

    const customerQuery = useQuery({
        queryKey: ['admin', 'customer', id],
        queryFn: () => adminCustomersApi.get(id!),
        enabled: Boolean(id),
    });

    const addressesQuery = useQuery({
        queryKey: ['admin', 'customer-addresses', id],
        queryFn: () => adminCustomersApi.getAddresses(id!),
        enabled: Boolean(id),
    });

    const ordersQuery = useQuery({
        queryKey: ['admin', 'customer-orders', id, orderPage],
        queryFn: () =>
            adminCustomersApi.getOrders(id!, orderPage, 20),
        enabled: Boolean(id),
    });

    const customer = customerQuery.data;
    const locale =
        i18n.resolvedLanguage?.startsWith('fa')
            ? 'fa-IR'
            : undefined;

    useDocumentTitle(
        customer?.displayName ||
        customer?.phoneNumber ||
        customer?.email ||
        'Customer details',
    );

    const date = (value?: string | null) =>
        value
            ? new Date(value).toLocaleString(locale)
            : '—';

    const money = (value: number) =>
        `${new Intl.NumberFormat(locale, {
            maximumFractionDigits: 0,
        }).format(value)} IRR`;

    const label = (key: string, fallback: string) =>
        t(key, { defaultValue: fallback });

    if (customerQuery.isLoading) {
        return (
            <div className="rounded-xl border p-8 text-muted-foreground">
                {label('common.loading', 'در حال بارگذاری...')}
            </div>
        );
    }

    if (customerQuery.isError || !customer) {
        return (
            <div className="space-y-4 rounded-xl border p-8">
                <p>
                    {label(
                        'customers.notFound',
                        'اطلاعات مشتری دریافت نشد.',
                    )}
                </p>

                <Button
                    variant="outline"
                    onClick={() => navigate('/admin/customers')}
                >
                    <ArrowLeft className="size-4" />
                    {label('customers.back', 'بازگشت به مشتریان')}
                </Button>
            </div>
        );
    }

    const orders = ordersQuery.data;
    const addresses = addressesQuery.data ?? [];

    return (
        <div className="grid gap-6">
            <PageHeader
                title={
                    customer.displayName ||
                    customer.phoneNumber ||
                    customer.email ||
                    customer.id
                }
                description={label(
                    'customers.profileDescription',
                    'پرونده مشتری، اطلاعات تماس و سابقه خرید',
                )}
                actions={
                    <Button
                        variant="outline"
                        onClick={() => navigate('/admin/customers')}
                    >
                        <ArrowLeft className="size-4" />
                        {label(
                            'customers.back',
                            'بازگشت به مشتریان',
                        )}
                    </Button>
                }
            />

            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <Metric
                    title={label('customers.orders', 'تعداد سفارش‌ها')}
                    value={String(customer.totalOrders)}
                />

                <Metric
                    title={label(
                        'customers.paidOrders',
                        'سفارش‌های پرداخت‌شده',
                    )}
                    value={String(customer.paidOrders)}
                />

                <Metric
                    title={label(
                        'customers.totalSpent',
                        'جمع خریدهای پرداخت‌شده',
                    )}
                    value={money(customer.totalSpent)}
                />

                <Metric
                    title={label(
                        'customers.lastOrder',
                        'آخرین سفارش',
                    )}
                    value={date(customer.lastOrderAt)}
                />
            </div>

            <div className="grid gap-4 lg:grid-cols-2">
                <section className="rounded-xl border p-5">
                    <h2 className="mb-4 font-semibold">
                        {label(
                            'customers.contact',
                            'اطلاعات تماس',
                        )}
                    </h2>

                    <div className="grid gap-4 text-sm">
                        <ContactRow
                            icon={<Phone className="size-4" />}
                            label={label(
                                'customers.phone',
                                'شماره تماس',
                            )}
                            value={customer.phoneNumber || '—'}
                        />

                        <p className="text-xs text-muted-foreground">
                            {label(
                                customer.phoneNumberConfirmed
                                    ? 'customers.phoneVerified'
                                    : 'customers.phoneNotVerified',
                                customer.phoneNumberConfirmed
                                    ? 'شماره تأیید شده'
                                    : 'شماره تأیید نشده',
                            )}
                        </p>

                        <ContactRow
                            icon={<Mail className="size-4" />}
                            label={label('customers.email', 'ایمیل')}
                            value={customer.email || '—'}
                        />

                        <p className="text-xs text-muted-foreground">
                            {label(
                                customer.emailConfirmed
                                    ? 'customers.emailVerified'
                                    : 'customers.emailNotVerified',
                                customer.emailConfirmed
                                    ? 'ایمیل تأیید شده'
                                    : 'ایمیل تأیید نشده',
                            )}
                        </p>

                        <div>
                            <p className="text-muted-foreground">
                                {label(
                                    'customers.joined',
                                    'تاریخ عضویت',
                                )}
                            </p>
                            <p className="mt-1">{date(customer.createdAt)}</p>
                        </div>
                    </div>
                </section>

                <section className="rounded-xl border p-5">
                    <h2 className="mb-4 font-semibold">
                        {label(
                            'customers.addresses',
                            'آدرس‌های ذخیره‌شده',
                        )}
                    </h2>

                    {addressesQuery.isLoading ? (
                        <p className="text-sm text-muted-foreground">
                            {label('common.loading', 'در حال بارگذاری...')}
                        </p>
                    ) : addresses.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                            {label(
                                'customers.noAddresses',
                                'این مشتری آدرس ذخیره‌شده‌ای ندارد.',
                            )}
                        </p>
                    ) : (
                        <div className="grid gap-3">
                            {addresses.map((address) => (
                                <div
                                    key={address.id}
                                    className="rounded-lg border p-3"
                                >
                                    <div className="flex items-center gap-2 font-medium">
                                        <MapPin className="size-4" />
                                        {address.title}

                                        {address.isDefault && (
                                            <span className="rounded-full border px-2 py-0.5 text-xs">
                                                {label(
                                                    'customers.defaultAddress',
                                                    'پیش‌فرض',
                                                )}
                                            </span>
                                        )}
                                    </div>

                                    <p className="mt-2 text-sm">
                                        {address.recipientName}
                                        {' · '}
                                        {address.phoneNumber}
                                    </p>

                                    <p className="mt-1 text-sm leading-6 text-muted-foreground">
                                        {[
                                            address.country,
                                            address.province,
                                            address.city,
                                            address.addressLine,
                                            address.postalCode,
                                        ]
                                            .filter(Boolean)
                                            .join('، ')}
                                    </p>
                                </div>
                            ))}
                        </div>
                    )}
                </section>
            </div>

            <section className="rounded-xl border">
                <div className="flex items-center gap-2 border-b p-5">
                    <ShoppingBag className="size-5" />
                    <h2 className="font-semibold">
                        {label(
                            'customers.orderHistory',
                            'تاریخچه سفارش‌ها',
                        )}
                    </h2>
                </div>

                {ordersQuery.isLoading ? (
                    <p className="p-5 text-sm text-muted-foreground">
                        {label('common.loading', 'در حال بارگذاری...')}
                    </p>
                ) : ordersQuery.isError ? (
                    <p className="p-5 text-sm text-destructive">
                        {label(
                            'customers.ordersError',
                            'دریافت سفارش‌های مشتری ناموفق بود.',
                        )}
                    </p>
                ) : !orders?.items.length ? (
                    <p className="p-5 text-sm text-muted-foreground">
                        {label(
                            'customers.noOrders',
                            'هنوز سفارشی ثبت نشده است.',
                        )}
                    </p>
                ) : (
                    <>
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[650px] text-sm">
                                <thead>
                                    <tr className="border-b bg-muted/30 text-start">
                                        <th className="px-4 py-3 text-start">
                                            {label(
                                                'customers.orderNumber',
                                                'شماره سفارش',
                                            )}
                                        </th>
                                        <th className="px-4 py-3 text-start">
                                            {label(
                                                'customers.status',
                                                'وضعیت',
                                            )}
                                        </th>
                                        <th className="px-4 py-3 text-start">
                                            {label(
                                                'customers.amount',
                                                'مبلغ',
                                            )}
                                        </th>
                                        <th className="px-4 py-3 text-start">
                                            {label(
                                                'customers.date',
                                                'تاریخ',
                                            )}
                                        </th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {orders.items.map((order) => (
                                        <tr
                                            key={order.id}
                                            className="border-b last:border-b-0"
                                        >
                                            <td className="px-4 py-3">
                                                {canManageOrders ? (
                                                    <Link
                                                        to={`/admin/orders/${order.id}`}
                                                        className="font-medium underline-offset-4 hover:underline"
                                                    >
                                                        {order.orderNumber}
                                                    </Link>
                                                ) : (
                                                    order.orderNumber
                                                )}
                                            </td>

                                            <td className="px-4 py-3">
                                                {label(
                                                    `storefront.orders.statuses.${order.status}`,
                                                    order.status,
                                                )}
                                            </td>

                                            <td className="px-4 py-3">
                                                {money(order.totalAmount)}
                                            </td>

                                            <td className="px-4 py-3">
                                                {date(order.createdAt)}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <div className="flex items-center justify-between gap-3 border-t p-4">
                            <p className="text-sm text-muted-foreground">
                                {label('customers.page', 'صفحه')}
                                {' '}
                                {orders.page}
                                {' / '}
                                {orders.totalPages}
                            </p>

                            <div className="flex gap-2">
                                <Button
                                    variant="outline"
                                    size="sm"
                                    disabled={!orders.hasPrevious}
                                    onClick={() =>
                                        setOrderPage((page) =>
                                            Math.max(1, page - 1),
                                        )
                                    }
                                >
                                    {label('customers.previous', 'قبلی')}
                                </Button>

                                <Button
                                    variant="outline"
                                    size="sm"
                                    disabled={!orders.hasNext}
                                    onClick={() =>
                                        setOrderPage((page) => page + 1)
                                    }
                                >
                                    {label('customers.next', 'بعدی')}
                                </Button>
                            </div>
                        </div>
                    </>
                )}
            </section>
        </div>
    );
}

function Metric({
    title,
    value,
}: {
    title: string;
    value: string;
}) {
    return (
        <div className="min-w-0 rounded-xl border p-4">
            <p className="text-sm text-muted-foreground">{title}</p>
            <p className="mt-2 break-words text-lg font-semibold">
                {value}
            </p>
        </div>
    );
}

function ContactRow({
    icon,
    label,
    value,
}: {
    icon: React.ReactNode;
    label: string;
    value: string;
}) {
    return (
        <div className="flex items-start gap-3">
            <span className="mt-0.5 text-muted-foreground">{icon}</span>
            <div className="min-w-0">
                <p className="text-muted-foreground">{label}</p>
                <p className="mt-1 break-words">{value}</p>
            </div>
        </div>
    );
}