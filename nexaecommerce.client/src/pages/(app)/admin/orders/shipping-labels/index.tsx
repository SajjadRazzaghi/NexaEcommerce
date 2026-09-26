import {
    useEffect,
    useMemo,
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
    appearanceApi,
    type Appearance,
} from '@/lib/api/appearance';

import {
    getAdminOrder,
} from '@/modules/orders/api/adminOrdersApi';

import type {
    OrderDto,
} from '@/modules/orders/types';

interface ShippingLabelsData {
    appearance: Appearance;
    orders: OrderDto[];
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
                    const appearance =
                        await appearanceApi.get();

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
                        appearance,
                        orders,
                    };
                },

            enabled:
                orderIds.length > 0,

            staleTime:
                60_000,
        });

    useEffect(
        () => {
            if (
                !data ||
                data.orders.length === 0
            ) {
                return;
            }

            const timer =
                window.setTimeout(
                    () =>
                        window.print(),
                    150,
                );

            const handleAfterPrint =
                () => {
                    window.setTimeout(
                        () =>
                            window.close(),
                        100,
                    );
                };

            window.addEventListener(
                'afterprint',
                handleAfterPrint,
                { once: true },
            );

            return () => {
                window.clearTimeout(
                    timer,
                );

                window.removeEventListener(
                    'afterprint',
                    handleAfterPrint,
                );
            };
        },
        [data],
    );

    const text =
        isFa
            ? {
                loading:
                    'ط¯ط± ط­ط§ظ„ ط¢ظ…ط§ط¯ظ‡â€Œط³ط§ط²غŒ ط¨ط±ع†ط³ط¨â€Œظ‡ط§...',
                empty:
                    'ط³ظپط§ط±ط´غŒ ط¨ط±ط§غŒ ع†ط§ظ¾ ط§ظ†طھط®ط§ط¨ ظ†ط´ط¯ظ‡ ط§ط³طھ.',
                error:
                    'ط¯ط±غŒط§ظپطھ ط§ط·ظ„ط§ط¹ط§طھ ط³ظپط§ط±ط´â€Œظ‡ط§ ط¨ط±ط§غŒ ع†ط§ظ¾ ط¨ط§ ظ…ط´ع©ظ„ ظ…ظˆط§ط¬ظ‡ ط´ط¯.',
                printAgain:
                    'ع†ط§ظ¾ ط¯ظˆط¨ط§ط±ظ‡',
                sender:
                    'ظپط±ط³طھظ†ط¯ظ‡',
                receiver:
                    'ع¯غŒط±ظ†ط¯ظ‡',
                city:
                    'ط´ظ‡ط±',
                postalCode:
                    'ع©ط¯ ظ¾ط³طھغŒ',
                address:
                    'ط¢ط¯ط±ط³',
                order:
                    'ط´ظ…ط§ط±ظ‡ ط³ظپط§ط±ط´',
            }
            : {
                loading:
                    'Preparing shipping labels...',
                empty:
                    'No orders were selected for printing.',
                error:
                    'We could not load the orders for printing.',
                printAgain:
                    'Print again',
                sender:
                    'Sender',
                receiver:
                    'Receiver',
                city:
                    'City',
                postalCode:
                    'Postal code',
                address:
                    'Address',
                order:
                    'Order number',
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
                    }

                    .shipping-label-screen-actions {
                        display: flex;
                        justify-content: center;
                        gap: 8px;
                        margin: 0 auto 16px;
                    }

                    .shipping-label {
                        box-sizing: border-box;
                        width: 100mm;
                        min-height: 150mm;
                        margin: 0 auto 16px;
                        padding: 7mm;
                        background: #fff;
                        color: #111;
                        border: 1px solid #d1d5db;
                        display: flex;
                        flex-direction: column;
                        gap: 5mm;
                        font-family: Tahoma, Arial, sans-serif;
                    }

                    .shipping-label__order {
                        display: flex;
                        justify-content: space-between;
                        gap: 8px;
                        padding-bottom: 4mm;
                        border-bottom: 1px solid #d1d5db;
                        font-size: 10pt;
                    }

                    .shipping-label__section {
                        border: 1px solid #9ca3af;
                        border-radius: 4mm;
                        padding: 5mm;
                    }

                    .shipping-label__section--receiver {
                        flex: 1;
                    }

                    .shipping-label__heading {
                        margin-bottom: 3mm;
                        font-size: 12pt;
                        font-weight: 700;
                    }

                    .shipping-label__name {
                        font-size: 16pt;
                        font-weight: 700;
                        line-height: 1.4;
                    }

                    .shipping-label__phone {
                        margin-top: 2mm;
                        font-size: 13pt;
                        font-weight: 700;
                    }

                    .shipping-label__address {
                        margin-top: 4mm;
                        font-size: 12pt;
                        line-height: 1.8;
                        white-space: pre-wrap;
                        overflow-wrap: anywhere;
                    }

                    .shipping-label__meta {
                        margin-top: 3mm;
                        display: grid;
                        gap: 2mm;
                        font-size: 10.5pt;
                    }

                    @page {
                        size: 100mm 150mm;
                        margin: 0;
                    }

                    @media print {
                        html,
                        body {
                            margin: 0 !important;
                            padding: 0 !important;
                            background: #fff !important;
                        }

                        body * {
                            visibility: hidden !important;
                        }

                        .shipping-labels-print,
                        .shipping-labels-print * {
                            visibility: visible !important;
                        }

                        .shipping-labels-print {
                            position: absolute !important;
                            inset: 0 !important;
                            width: 100% !important;
                            min-height: 0 !important;
                            padding: 0 !important;
                            margin: 0 !important;
                            background: #fff !important;
                        }

                        .shipping-label-screen-actions {
                            display: none !important;
                        }

                        .shipping-label {
                            width: 100mm !important;
                            height: 150mm !important;
                            min-height: 150mm !important;
                            margin: 0 !important;
                            border: 0 !important;
                            border-radius: 0 !important;
                            page-break-after: always !important;
                            break-after: page !important;
                        }

                        .shipping-label:last-child {
                            page-break-after: auto !important;
                            break-after: auto !important;
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
                    <button
                        type="button"
                        onClick={() =>
                            window.print()
                        }
                        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground"
                    >
                        <Printer className="size-4" />
                        {text.printAgain}
                    </button>
                </div>

                {data.orders.map(
                    order => (
                        <article
                            key={
                                order.id
                            }
                            className="shipping-label"
                        >
                            <div className="shipping-label__order">
                                <span>
                                    {text.order}
                                </span>

                                <strong>
                                    {
                                        order.orderNumber
                                    }
                                </strong>
                            </div>

                            <section className="shipping-label__section">
                                <div className="shipping-label__heading">
                                    {text.sender}
                                </div>

                                <div className="shipping-label__name">
                                    {
                                        data.appearance.storeName ??
                                        'â€”'
                                    }
                                </div>

                                {data.appearance.contactPhone && (
                                    <div className="shipping-label__phone">
                                        {
                                            data.appearance.contactPhone
                                        }
                                    </div>
                                )}

                                <div className="shipping-label__address">
                                    {
                                        data.appearance.contactAddress ??
                                        'â€”'
                                    }
                                </div>
                            </section>

                            <section className="shipping-label__section shipping-label__section--receiver">
                                <div className="shipping-label__heading">
                                    {text.receiver}
                                </div>

                                <div className="shipping-label__name">
                                    {
                                        order.shippingFullName ||
                                        'â€”'
                                    }
                                </div>

                                <div className="shipping-label__phone">
                                    {
                                        order.shippingPhone ||
                                        'â€”'
                                    }
                                </div>

                                <div className="shipping-label__address">
                                    {
                                        order.shippingAddress ||
                                        'â€”'
                                    }
                                </div>

                                <div className="shipping-label__meta">
                                    <div>
                                        <strong>
                                            {text.city}:
                                        </strong>{' '}
                                        {
                                            order.shippingCity ||
                                            'â€”'
                                        }
                                    </div>

                                    <div>
                                        <strong>
                                            {text.postalCode}:
                                        </strong>{' '}
                                        {
                                            order.shippingPostalCode ||
                                            'â€”'
                                        }
                                    </div>
                                </div>
                            </section>
                        </article>
                    ),
                )}
            </main>
        </>
    );
}