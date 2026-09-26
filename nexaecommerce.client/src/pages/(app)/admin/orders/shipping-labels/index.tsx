import {
    useEffect,
    useMemo,
    useState,
} from 'react';

import {
    useQuery,
} from '@tanstack/react-query';

import {
    useSearchParams,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    LoaderCircle,
    Printer,
} from 'lucide-react';

import {
    getAdminOrder,
} from '@/modules/orders/api/adminOrdersApi';

import type {
    OrderDto,
} from '@/modules/orders/types';

interface ShippingLabelsData {
    orders: OrderDto[];
}

type LabelLayout =
    | '2x1'
    | '2x2'
    | '2x3'
    | '2x4'
    | '2x5'
    | '2x6';

const LAYOUTS: Array<{
    value: LabelLayout;
    label: string;
    rows: number;
}> = [
        {
            value: '2x1',
            label: '۲ × ۱ — ۲ لیبل در هر صفحه',
            rows: 1,
        },
        {
            value: '2x2',
            label: '۲ × ۲ — ۴ لیبل در هر صفحه',
            rows: 2,
        },
        {
            value: '2x3',
            label: '۲ × ۳ — ۶ لیبل در هر صفحه',
            rows: 3,
        },
        {
            value: '2x4',
            label: '۲ × ۴ — ۸ لیبل در هر صفحه',
            rows: 4,
        },
        {
            value: '2x5',
            label: '۲ × ۵ — ۱۰ لیبل در هر صفحه',
            rows: 5,
        },
        {
            value: '2x6',
            label: '۲ × ۶ — ۱۲ لیبل در هر صفحه',
            rows: 6,
        },
    ];

function getSavedLayout(): LabelLayout {
    if (typeof window === 'undefined') {
        return '2x4';
    }

    const saved =
        window.localStorage.getItem(
            'nexa-shipping-label-layout',
        ) as LabelLayout | null;

    return LAYOUTS.some(
        layout =>
            layout.value === saved,
    )
        ? saved!
        : '2x4';
}

function getReceiverAddress(
    order: OrderDto,
): string {
    const city =
        order.shippingCity?.trim() ?? '';

    const address =
        order.shippingAddress?.trim() ?? '';

    if (!city) {
        return address || '—';
    }

    if (!address) {
        return city;
    }

    // اگر شهر قبلاً داخل آدرس آمده باشد،
    // دوباره تکرارش نمی‌کنیم.
    if (
        address
            .toLocaleLowerCase()
            .includes(
                city.toLocaleLowerCase(),
            )
    ) {
        return address;
    }

    return `${city}، ${address}`;
}

export default function AdminOrderShippingLabelsPage() {
    const [searchParams] =
        useSearchParams();

    const { i18n } =
        useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const [layout, setLayout] =
        useState<LabelLayout>(
            getSavedLayout,
        );

    const idsParam =
        searchParams.get('ids') ??
        '';

    const orderIds =
        useMemo(
            () =>
                Array.from(
                    new Set(
                        idsParam
                            .split(',')
                            .map(
                                value =>
                                    value.trim(),
                            )
                            .filter(
                                Boolean,
                            ),
                    ),
                ),
            [idsParam],
        );

    const selectedLayout =
        LAYOUTS.find(
            item =>
                item.value === layout,
        ) ??
        LAYOUTS[3];

    const labelsPerPage =
        2 * selectedLayout.rows;

    const {
        data,
        isLoading,
        isError,
        error,
    } =
        useQuery<ShippingLabelsData>({
            queryKey: [
                'admin',
                'orders',
                'shipping-labels',
                orderIds,
            ],

            queryFn:
                async () => {
                    const orders =
                        await Promise.all(
                            orderIds.map(
                                orderId =>
                                    getAdminOrder(
                                        orderId,
                                    ),
                            ),
                        );

                    return {
                        orders,
                    };
                },

            enabled:
                orderIds.length > 0,

            staleTime:
                60_000,
        });

    const pages =
        useMemo(
            () => {
                if (
                    !data ||
                    data.orders.length === 0
                ) {
                    return [];
                }

                const result: OrderDto[][] =
                    [];

                for (
                    let index = 0;
                    index <
                    data.orders.length;
                    index += labelsPerPage
                ) {
                    result.push(
                        data.orders.slice(
                            index,
                            index +
                            labelsPerPage,
                        ),
                    );
                }

                return result;
            },
            [
                data,
                labelsPerPage,
            ],
        );

    useEffect(
        () => {
            if (
                typeof window ===
                'undefined'
            ) {
                return;
            }

            window.localStorage.setItem(
                'nexa-shipping-label-layout',
                layout,
            );
        },
        [layout],
    );

    const text =
        isFa
            ? {
                loading:
                    'در حال آماده‌سازی لیبل‌ها...',
                empty:
                    'سفارشی برای چاپ انتخاب نشده است.',
                error:
                    'دریافت اطلاعات سفارش‌ها برای چاپ با مشکل مواجه شد.',
                print:
                    'چاپ لیبل‌ها',
                layout:
                    'چیدمان',
                receiver:
                    'گیرنده',
                address:
                    'آدرس گیرنده',
                products:
                    'محصول',
                quantity:
                    'تعداد',
                order:
                    'سفارش',
            }
            : {
                loading:
                    'Preparing shipping labels...',
                empty:
                    'No orders were selected for printing.',
                error:
                    'We could not load the orders for printing.',
                print:
                    'Print labels',
                layout:
                    'Layout',
                receiver:
                    'Receiver',
                address:
                    'Receiver address',
                products:
                    'Products',
                quantity:
                    'Qty',
                order:
                    'Order',
            };

    if (
        orderIds.length === 0
    ) {
        return (
            <div
                className="grid min-h-screen place-items-center p-6 text-center"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <div className="rounded-xl border p-8">
                    <p className="text-sm text-muted-foreground">
                        {text.empty}
                    </p>
                </div>
            </div>
        );
    }

    if (isLoading) {
        return (
            <div
                className="grid min-h-screen place-items-center p-6 text-center"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <div className="flex items-center gap-3">
                    <LoaderCircle className="size-5 animate-spin" />

                    <span>
                        {text.loading}
                    </span>
                </div>
            </div>
        );
    }

    if (
        isError ||
        !data
    ) {
        return (
            <div
                className="grid min-h-screen place-items-center p-6 text-center"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <div className="rounded-xl border border-destructive/30 p-8">
                    <p className="text-sm text-destructive">
                        {
                            error instanceof
                                Error
                                ? error.message
                                : text.error
                        }
                    </p>
                </div>
            </div>
        );
    }

    return (
        <>
            <style>
                {`
                    .shipping-labels-print {
                        min-height: 100vh;
                        background: #f3f4f6;
                        padding: 16px;
                        box-sizing: border-box;
                    }

                    .shipping-label-screen-actions {
                        display: flex;
                        flex-wrap: wrap;
                        align-items: center;
                        justify-content: center;
                        gap: 10px;
                        margin: 0 auto 18px;
                    }

                    .shipping-label-layout-control {
                        display: inline-flex;
                        align-items: center;
                        gap: 10px;
                        padding: 9px 12px;
                        border: 1px solid #d1d5db;
                        border-radius: 8px;
                        background: #fff;
                        color: #111827;
                        font-size: 13px;
                    }

                    .shipping-label-layout-control select {
                        min-width: 260px;
                        border: 0;
                        outline: none;
                        background: transparent;
                        color: inherit;
                        font: inherit;
                        cursor: pointer;
                    }

                    .shipping-labels-document {
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        gap: 18px;
                    }

                    .shipping-label-page {
                        box-sizing: border-box;
                        width: 200mm;
                        height: 287mm;
                        padding: 4mm;
                        background: #fff;

                        display: grid;

                        grid-template-columns:
                            repeat(
                                2,
                                minmax(0, 1fr)
                            );

                        grid-template-rows:
                            repeat(
                                var(--shipping-label-rows),
                                minmax(0, 1fr)
                            );

                        gap: 4mm;

                        overflow: hidden;

                        box-shadow:
                            0 2px 10px
                                rgba(
                                    0,
                                    0,
                                    0,
                                    0.08
                                );
                    }

                    .shipping-label {
                        box-sizing: border-box;

                        min-width: 0;
                        min-height: 0;

                        background: #fff;
                        color: #111;

                        border:
                            1px solid #9ca3af;

                        border-radius: 3mm;

                        padding: 4mm;

                        display: flex;
                        flex-direction: column;

                        direction: rtl;

                        font-family:
                            Tahoma,
                            Arial,
                            sans-serif;

                        overflow: hidden;
                    }

                    .shipping-label__order {
                        display: flex;
                        align-items: center;
                        justify-content: space-between;

                        gap: 3mm;

                        padding-bottom: 2.5mm;
                        margin-bottom: 3mm;

                        border-bottom:
                            1px solid #d1d5db;

                        font-size: 8.5pt;
                    }

                    .shipping-label__receiver {
                        font-size: 13pt;
                        font-weight: 700;
                        line-height: 1.45;

                        overflow-wrap: anywhere;
                        word-break: break-word;
                    }

                    .shipping-label__phone {
                        margin-top: 1.5mm;

                        font-size: 10.5pt;
                        font-weight: 700;

                        direction: ltr;
                        text-align: right;

                        overflow-wrap: anywhere;
                        word-break: break-word;
                    }

                    .shipping-label__address-title {
                        margin-top: 3mm;

                        font-size: 9pt;
                        font-weight: 700;
                    }

                    .shipping-label__address {
                        margin-top: 1.2mm;

                        font-size: 9pt;
                        line-height: 1.55;

                        white-space: pre-wrap;

                        overflow-wrap: anywhere;
                        word-break: break-word;

                        max-height: 45%;
                        overflow: hidden;
                    }

                    .shipping-label__items {
                        margin-top: auto;

                        padding-top: 2.5mm;

                        border-top:
                            1px solid #d1d5db;

                        overflow: hidden;
                    }

                    .shipping-label__items-title {
                        margin-bottom: 1.5mm;

                        font-size: 8.5pt;
                        font-weight: 700;
                    }

                    .shipping-label__item {
                        display: grid;

                        grid-template-columns:
                            minmax(0, 1fr)
                            auto;

                        gap: 2mm;

                        padding:
                            1mm
                            0;

                        font-size: 8.5pt;
                        line-height: 1.4;
                    }

                    .shipping-label__item-name {
                        min-width: 0;

                        overflow-wrap: anywhere;
                        word-break: break-word;
                    }

                    .shipping-label__item-quantity {
                        white-space: nowrap;
                        font-weight: 700;
                    }

                    .shipping-label__empty {
                        visibility: hidden;
                    }

                    @page {
                        size: A4 portrait;
                        margin: 5mm;
                    }

                    @media print {
                        html,
                        body {
                            margin: 0 !important;
                            padding: 0 !important;

                            background: #fff !important;
                        }

                        body {
                            -webkit-print-color-adjust: exact;
                            print-color-adjust: exact;
                        }

                        .shipping-labels-print {
                            width: 200mm !important;

                            min-height: 0 !important;

                            padding: 0 !important;
                            margin: 0 !important;

                            background: #fff !important;
                        }

                        .shipping-label-screen-actions {
                            display: none !important;
                        }

                        .shipping-labels-document {
                            display: block !important;
                        }

                        .shipping-label-page {
                            width: 200mm !important;
                            height: 287mm !important;

                            margin: 0 !important;
                            padding: 4mm !important;

                            display: grid !important;

                            grid-template-columns:
                                repeat(
                                    2,
                                    minmax(0, 1fr)
                                ) !important;

                            grid-template-rows:
                                repeat(
                                    var(
                                        --shipping-label-rows
                                    ),
                                    minmax(0, 1fr)
                                ) !important;

                            gap: 4mm !important;

                            box-shadow: none !important;

                            overflow: hidden !important;

                            page-break-after:
                                always !important;

                            break-after:
                                page !important;

                            page-break-inside:
                                avoid !important;

                            break-inside:
                                avoid !important;
                        }

                        .shipping-label-page:last-child {
                            page-break-after:
                                auto !important;

                            break-after:
                                auto !important;
                        }

                        .shipping-label {
                            width: auto !important;
                            height: auto !important;

                            min-width: 0 !important;
                            min-height: 0 !important;

                            margin: 0 !important;

                            break-inside:
                                avoid !important;

                            page-break-inside:
                                avoid !important;

                            overflow: hidden !important;
                        }
                    }
                `}
            </style>

            <main
                className="shipping-labels-print"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <div className="shipping-label-screen-actions">
                    <label className="shipping-label-layout-control">
                        <span>
                            {text.layout}
                        </span>

                        <select
                            value={layout}
                            onChange={event =>
                                setLayout(
                                    event.target
                                        .value as LabelLayout,
                                )
                            }
                        >
                            {LAYOUTS.map(
                                item => (
                                    <option
                                        key={
                                            item.value
                                        }
                                        value={
                                            item.value
                                        }
                                    >
                                        {
                                            item.label
                                        }
                                    </option>
                                ),
                            )}
                        </select>
                    </label>

                    <button
                        type="button"
                        onClick={() =>
                            window.print()
                        }
                        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground"
                    >
                        <Printer className="size-4" />

                        {text.print}
                    </button>
                </div>

                <div className="shipping-labels-document">
                    {pages.map(
                        (
                            pageOrders,
                            pageIndex,
                        ) => {
                            const emptyCount =
                                labelsPerPage -
                                pageOrders.length;

                            return (
                                <section
                                    key={
                                        pageIndex
                                    }
                                    className="shipping-label-page"
                                    style={
                                        {
                                            '--shipping-label-rows':
                                                selectedLayout.rows,
                                        } as Record<
                                            string,
                                            number
                                        >
                                    }
                                >
                                    {pageOrders.map(
                                        order => (
                                            <article
                                                key={
                                                    order.id
                                                }
                                                className="shipping-label"
                                            >
                                                <div className="shipping-label__order">
                                                    <span>
                                                        {
                                                            text.order
                                                        }
                                                    </span>

                                                    <strong>
                                                        {
                                                            order.orderNumber
                                                        }
                                                    </strong>
                                                </div>

                                                <div className="shipping-label__receiver">
                                                    {
                                                        order.shippingFullName ||
                                                        '—'
                                                    }
                                                </div>

                                                <div className="shipping-label__phone">
                                                    {
                                                        order.shippingPhone ||
                                                        '—'
                                                    }
                                                </div>

                                                <div className="shipping-label__address-title">
                                                    {
                                                        text.address
                                                    }
                                                </div>

                                                <div className="shipping-label__address">
                                                    {
                                                        getReceiverAddress(
                                                            order,
                                                        )
                                                    }
                                                </div>

                                                {order.items.length >
                                                    0 && (
                                                        <div className="shipping-label__items">
                                                            <div className="shipping-label__items-title">
                                                                {
                                                                    text.products
                                                                }
                                                            </div>

                                                            {order.items.map(
                                                                (
                                                                    item,
                                                                    itemIndex,
                                                                ) => (
                                                                    <div
                                                                        key={
                                                                            `${item.productVariantId}-${itemIndex}`
                                                                        }
                                                                        className="shipping-label__item"
                                                                    >
                                                                        <div className="shipping-label__item-name">
                                                                            {
                                                                                item.productName
                                                                            }
                                                                        </div>

                                                                        <div className="shipping-label__item-quantity">
                                                                            {
                                                                                text.quantity
                                                                            }
                                                                            :{' '}
                                                                            {
                                                                                item.quantity
                                                                            }
                                                                        </div>
                                                                    </div>
                                                                ),
                                                            )}
                                                        </div>
                                                    )}
                                            </article>
                                        ),
                                    )}

                                    {Array.from(
                                        {
                                            length:
                                                emptyCount,
                                        },
                                        (
                                            _,
                                            emptyIndex,
                                        ) => (
                                            <div
                                                key={
                                                    `empty-${emptyIndex}`
                                                }
                                                className="shipping-label shipping-label__empty"
                                                aria-hidden="true"
                                            />
                                        ),
                                    )}
                                </section>
                            );
                        },
                    )}
                </div>
            </main>
        </>
    );
}