param(
    [string]$ProjectRoot = (Get-Location).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$clientRoot = Join-Path $ProjectRoot 'nexaecommerce.client'
$orderPagePath = Join-Path $clientRoot 'src\pages\(app)\admin\orders\index.tsx'
$printPageDir = Join-Path $clientRoot 'src\pages\(app)\admin\orders\shipping-labels'
$printPagePath = Join-Path $printPageDir 'index.tsx'
$backupPath = "$orderPagePath.shipping-labels.bak"

if (-not (Test-Path $orderPagePath)) {
    throw "Order page was not found: $orderPagePath"
}

$orderPage = [System.IO.File]::ReadAllText(
    $orderPagePath,
    [System.Text.UTF8Encoding]::new($false)
)

# Normalize LF/CRLF differences so replacements are reliable.
$orderPage = $orderPage -replace "`r`n", "`n"
$orderPage = $orderPage -replace "`r", "`n"

if ($orderPage.Contains("shipping-labels?ids=")) {
    throw "Shipping label printing appears to be already applied to the order page."
}

Copy-Item $orderPagePath $backupPath -Force

# ------------------------------------------------------------
# 1. Add Printer icon to lucide-react import
# ------------------------------------------------------------
if ($orderPage -notmatch "from\s+['""]lucide-react['""]") {
    throw "Could not find the lucide-react import."
}

if ($orderPage -notmatch "\bPrinter\b") {
    $lucideImportPattern = '(?ms)(import\s*\{)(.*?)(\}\s*from\s+[''"]lucide-react[''"];?)'
    $lucideMatch = [regex]::Match($orderPage, $lucideImportPattern)
    if (-not $lucideMatch.Success) {
        throw "Could not parse the lucide-react import block."
    }

    $importBody = $lucideMatch.Groups[2].Value
    $newImportBody = $importBody

    if ($newImportBody -notmatch '(?m)^\s*Printer,\s*$') {
        if ($newImportBody -match '\bPackage,\s*') {
            $newImportBody = [regex]::Replace(
                $newImportBody,
                '(\bPackage,\s*)',
                '${1}    Printer,' + "`n",
                1
            )
        }
        else {
            $newImportBody = "    Printer," + "`n" + $newImportBody
        }
    }

    $replacement =
        $lucideMatch.Groups[1].Value +
        $newImportBody +
        $lucideMatch.Groups[3].Value

    $orderPage =
        $orderPage.Substring(0, $lucideMatch.Index) +
        $replacement +
        $orderPage.Substring(
            $lucideMatch.Index + $lucideMatch.Length
        )
}

# ------------------------------------------------------------
# 2. Add selection state after search state if not present
# ------------------------------------------------------------
if ($orderPage -notmatch "selectedOrderIds") {
    $searchStatePattern = '(?ms)(\bconst\s+\[search,\s+setSearch\]\s*=\s*useState\([^;]+;\s*)'
    $searchMatch = [regex]::Match($orderPage, $searchStatePattern)

    if (-not $searchMatch.Success) {
        throw "Could not locate the search state in the orders page."
    }

    $selectionState = @'
    const [
        selectedOrderIds,
        setSelectedOrderIds,
    ] =
        useState<Set<string>>(
            () => new Set(),
        );

'@

    $orderPage =
        $orderPage.Substring(0, $searchMatch.Index + $searchMatch.Length) +
        $selectionState +
        $orderPage.Substring(
            $searchMatch.Index + $searchMatch.Length
        )
}

# ------------------------------------------------------------
# 3. Add selection helpers before "const text"
# ------------------------------------------------------------
if ($orderPage -notmatch "const visibleOrderIds") {
    $textMarkerPattern = '(?m)^(\s*const\s+text\s*=\s*isFa\b)'
    $textMatch = [regex]::Match($orderPage, $textMarkerPattern)

    if (-not $textMatch.Success) {
        throw "Could not locate the text object in the orders page."
    }

    $helpers = @'
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

'@

    $orderPage =
        $orderPage.Substring(0, $textMatch.Index) +
        $helpers +
        $orderPage.Substring($textMatch.Index)
}

# ------------------------------------------------------------
# 4. Add translations safely
# ------------------------------------------------------------
if ($orderPage -notmatch "printLabels:") {
    # Insert before the final closing of the Persian text object.
    $persianMarker = "            next:`n                'بعدی',"
    if ($orderPage.Contains($persianMarker)) {
        $orderPage = $orderPage.Replace(
            $persianMarker,
            $persianMarker + @"

            selected:
                'انتخاب شده',
            printLabels:
                'چاپ برچسب پستی',
            selectAll:
                'انتخاب همه',
"@
        )
    }
    else {
        Write-Warning "Persian translation marker was not found; add printLabels/selected/selectAll manually if needed."
    }

    $englishMarker = "            next:`n                'Next',"
    if ($orderPage.Contains($englishMarker)) {
        $orderPage = $orderPage.Replace(
            $englishMarker,
            $englishMarker + @"

            selected:
                'selected',
            printLabels:
                'Print shipping labels',
            selectAll:
                'Select all',
"@
        )
    }
    else {
        Write-Warning "English translation marker was not found; add printLabels/selected/selectAll manually if needed."
    }
}

# ------------------------------------------------------------
# 5. Add toolbar before the loading table
# ------------------------------------------------------------
if ($orderPage -notmatch "text\.printLabels") {
    $loadingMarker = '(?ms)(\s*{isLoading\s*&&\s*\()'
    $loadingMatch = [regex]::Match(
        $orderPage,
        $loadingMarker
    )

    if (-not $loadingMatch.Success) {
        throw "Could not locate the orders loading section."
    }

    $toolbar = @'

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
'@

    $orderPage =
        $orderPage.Substring(
            0,
            $loadingMatch.Index
        ) +
        $toolbar +
        $orderPage.Substring(
            $loadingMatch.Index
        )
}

# ------------------------------------------------------------
# 6. Add select-all checkbox to the first table header
# ------------------------------------------------------------
if ($orderPage -notmatch "toggleVisibleOrderSelection") {
    # This branch is intentionally unused because helper insertion above
    # already ensures the function exists. Kept for clarity.
}

if ($orderPage -notmatch "aria-label=\{\s*text\.selectAll\s*\}") {
    $headerPattern = '(?ms)(<tr[^>]*>\s*)(<th\b)'
    $headerMatch = [regex]::Match(
        $orderPage,
        $headerPattern
    )

    if (-not $headerMatch.Success) {
        throw "Could not locate the orders table header."
    }

    $selectAllHeader = @'
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

'@

    $orderPage =
        $orderPage.Substring(0, $headerMatch.Index + $headerMatch.Groups[1].Length) +
        $selectAllHeader +
        $orderPage.Substring(
            $headerMatch.Index + $headerMatch.Groups[1].Length
        )
}

# ------------------------------------------------------------
# 7. Add checkbox to order rows
# ------------------------------------------------------------
if ($orderPage -notmatch "toggleOrderSelection\(\s*order\.id") {
    $rowPattern = '(?ms)(<tr\s+key=\{\s*order\.id\s*\}[^>]*>\s*)(<td\b)'
    $rowMatch = [regex]::Match(
        $orderPage,
        $rowPattern
    )

    if (-not $rowMatch.Success) {
        throw "Could not locate the order table row."
    }

    $rowCheckbox = @'
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

'@

    $orderPage =
        $orderPage.Substring(0, $rowMatch.Index + $rowMatch.Groups[1].Length) +
        $rowCheckbox +
        $orderPage.Substring(
            $rowMatch.Index + $rowMatch.Groups[1].Length
        )
}

# Write page back with the original project-friendly UTF-8 format.
[System.IO.File]::WriteAllText(
    $orderPagePath,
    $orderPage,
    [System.Text.UTF8Encoding]::new($false)
)

# ------------------------------------------------------------
# 8. Create printable shipping label page
# ------------------------------------------------------------
New-Item -ItemType Directory -Path $printPageDir -Force | Out-Null

$printPage = @'
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
                    'در حال آماده‌سازی برچسب‌ها...',
                empty:
                    'سفارشی برای چاپ انتخاب نشده است.',
                error:
                    'دریافت اطلاعات سفارش‌ها برای چاپ با مشکل مواجه شد.',
                printAgain:
                    'چاپ دوباره',
                sender:
                    'فرستنده',
                receiver:
                    'گیرنده',
                city:
                    'شهر',
                postalCode:
                    'کد پستی',
                address:
                    'آدرس',
                order:
                    'شماره سفارش',
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
                                        '—'
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
                                        '—'
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
                                        '—'
                                    }
                                </div>

                                <div className="shipping-label__phone">
                                    {
                                        order.shippingPhone ||
                                        '—'
                                    }
                                </div>

                                <div className="shipping-label__address">
                                    {
                                        order.shippingAddress ||
                                        '—'
                                    }
                                </div>

                                <div className="shipping-label__meta">
                                    <div>
                                        <strong>
                                            {text.city}:
                                        </strong>{' '}
                                        {
                                            order.shippingCity ||
                                            '—'
                                        }
                                    </div>

                                    <div>
                                        <strong>
                                            {text.postalCode}:
                                        </strong>{' '}
                                        {
                                            order.shippingPostalCode ||
                                            '—'
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
'@

[System.IO.File]::WriteAllText(
    $printPagePath,
    $printPage,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ""
Write-Host "Shipping label printing has been added successfully." -ForegroundColor Green
Write-Host "Backup:" $backupPath
Write-Host "Print page:" $printPagePath
Write-Host "Print size: 100mm x 150mm"
Write-Host ""
Write-Host "Next:"
Write-Host "  cd .\nexaecommerce.client"
Write-Host "  npm run build"
