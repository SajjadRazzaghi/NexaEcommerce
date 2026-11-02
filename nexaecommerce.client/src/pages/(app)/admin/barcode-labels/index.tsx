import { useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
    Barcode,
    Printer,
    Search,
    Plus,
    Trash2,
    Info,
    Package,
    RotateCw,
    Eye,
    RefreshCw,
    AlertCircle,
    Settings2,
   
} from 'lucide-react';
import JsBarcode from 'jsbarcode';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import { useDocumentTitle } from '@/hooks/use-document-title';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';
import { formatMoney } from '@/lib/formatMoney';
import {
    productsApi,
    type Product,
    type ProductVariant,
} from '@/modules/catalog/api/products';

// ---------------------------------------------------------
// Types
// ---------------------------------------------------------

type BarcodeLabelLine = {
    variantId: string;
    productId: string;
    productName: string;
    sku: string;
    barcode: string;
    variantText: string;
    price: number;
    currency: string;
    quantity: number;
};

type PrintFormat = 'a4' | 'thermal';
type ThermalOrientation = 'normal' | 'rotated';

type ThermalPrintSettings = {
    rollWidthMm: number | null;
    columns: 1 | 2 | 3;

    labelWidthMm: number | null;
    labelHeightMm: number | null;

    orientation: ThermalOrientation;

    horizontalGapMm: number | null;
    verticalGapMm: number | null;

    marginLeftMm: number | null;
    marginRightMm: number | null;
    marginTopMm: number | null;
    marginBottomMm: number | null;
};

type ThermalNumericKey =
    Exclude<keyof ThermalPrintSettings, 'columns' | 'orientation'>;

type NumericSettingProps = {
    id: string;
    label: string;
    value: string;
    onChange: (value: string) => void;
    description?: string;
};

// ---------------------------------------------------------
// Defaults
// ---------------------------------------------------------

const THERMAL_SETTINGS_KEY = 'nexa.barcode-labels.thermal-settings.v2';

const DEFAULT_THERMAL_SETTINGS: ThermalPrintSettings = {
    rollWidthMm: 95,

    columns: 1,

    labelWidthMm: 45,
    labelHeightMm: 30,

    orientation: 'normal',

    horizontalGapMm: 0,
    verticalGapMm: 0,

    marginLeftMm: 0,
    marginRightMm: 0,
    marginTopMm: 0,
    marginBottomMm: 0,
};

// ---------------------------------------------------------
// Settings helpers
// ---------------------------------------------------------

function loadThermalSettings(): ThermalPrintSettings {
    if (typeof window === 'undefined') {
        return DEFAULT_THERMAL_SETTINGS;
    }

    try {
        const raw = window.localStorage.getItem(THERMAL_SETTINGS_KEY);

        if (!raw) {
            return DEFAULT_THERMAL_SETTINGS;
        }

        const saved = JSON.parse(raw) as Partial<ThermalPrintSettings>;

        const readNumber = (
            value: unknown,
            fallback: number | null,
        ): number | null => {
            if (value === null) return null;

            const number = Number(value);

            return Number.isFinite(number)
                ? number
                : fallback;
        };

        return {
            rollWidthMm: readNumber(
                saved.rollWidthMm,
                DEFAULT_THERMAL_SETTINGS.rollWidthMm,
            ),

            columns:
                saved.columns === 3
                    ? 3
                    : saved.columns === 2
                        ? 2
                        : 1,

            labelWidthMm: readNumber(
                saved.labelWidthMm,
                DEFAULT_THERMAL_SETTINGS.labelWidthMm,
            ),

            labelHeightMm: readNumber(
                saved.labelHeightMm,
                DEFAULT_THERMAL_SETTINGS.labelHeightMm,
            ),

            orientation:
                saved.orientation === 'rotated'
                    ? 'rotated'
                    : 'normal',

            horizontalGapMm: readNumber(
                saved.horizontalGapMm,
                DEFAULT_THERMAL_SETTINGS.horizontalGapMm,
            ),

            verticalGapMm: readNumber(
                saved.verticalGapMm,
                DEFAULT_THERMAL_SETTINGS.verticalGapMm,
            ),

            marginLeftMm: readNumber(
                saved.marginLeftMm,
                DEFAULT_THERMAL_SETTINGS.marginLeftMm,
            ),

            marginRightMm: readNumber(
                saved.marginRightMm,
                DEFAULT_THERMAL_SETTINGS.marginRightMm,
            ),

            marginTopMm: readNumber(
                saved.marginTopMm,
                DEFAULT_THERMAL_SETTINGS.marginTopMm,
            ),

            marginBottomMm: readNumber(
                saved.marginBottomMm,
                DEFAULT_THERMAL_SETTINGS.marginBottomMm,
            ),
        };
    } catch {
        return DEFAULT_THERMAL_SETTINGS;
    }
}

function safeNumber(value: number | null): number {
    return value ?? 0;
}

// ---------------------------------------------------------
// Product helpers
// ---------------------------------------------------------

function variantDescription(variant: ProductVariant): string {
    const values = (variant.attributes ?? [])
        .map((attribute) => {
            const value =
                attribute.displayValue?.trim() ||
                attribute.value?.trim();

            if (!value) return null;

            return `${attribute.attributeName}: ${value}`;
        })
        .filter((value): value is string => Boolean(value));

    return values.join(' / ') || 'تنوع پیش‌فرض';
}

// ---------------------------------------------------------
// Number input component
// No min / max / auto-clamp / required
// ---------------------------------------------------------

function NumberSetting({
    id,
    label,
    value,
    onChange,
    description,
}: NumericSettingProps) {
    return (
        <div className="min-w-0 space-y-2">
            <label
                htmlFor={id}
                className="flex items-center gap-2 text-sm font-semibold text-slate-700"
            >
                {label}
            </label>

            <Input
                id={id}
                type="number"
                step="any"
                inputMode="decimal"
                value={value}
                onChange={(event) => onChange(event.target.value)}
                className="h-11 bg-white text-center text-base font-semibold"
            />

            {description && (
                <p className="text-xs leading-5 text-muted-foreground">
                    {description}
                </p>
            )}
        </div>
    );
}

// ---------------------------------------------------------
// Barcode renderer
// ---------------------------------------------------------

function BarcodeSvg({ value }: { value: string }) {
    const svgRef = useRef<SVGSVGElement>(null);

    useEffect(() => {
        if (!svgRef.current || !value) return;

        try {
            svgRef.current.innerHTML = '';

            JsBarcode(svgRef.current, value, {
                format: 'CODE128',
                displayValue: false,
                lineColor: '#000000',
                background: '#ffffff',

                // ضخامت خطوط بارکد
                width: 1.25,

                // ارتفاع مبنای بارکد
                height: 38,

                // حاشیه سفید دو طرف بارکد
                margin: 0,
                marginLeft: 10,
                marginRight: 10,
                marginTop: 0,
                marginBottom: 0,
            });
        } catch (error) {
            console.error('Barcode generation failed:', error);

            if (svgRef.current) {
                svgRef.current.innerHTML = '';
            }
        }
    }, [value]);

    return (
        <svg
            ref={svgRef}
            aria-label={`بارکد ${value}`}
            preserveAspectRatio="xMidYMid meet"
            className="barcode-svg mx-auto block h-auto w-[90%] max-w-[90%] shrink-0"
        />
    );
}
// ---------------------------------------------------------
// Label content
// ---------------------------------------------------------
function getPrintableBarcode(value: string): string {
    return value.replace(/\D/g, '').slice(0, 8);
}
function LabelContent({
    line,
    locale,
}: {
    line: BarcodeLabelLine;
    locale: string;
}) {
    const printableBarcode = getPrintableBarcode(line.barcode);

    return (
        <div className="barcode-label-content flex h-full w-full flex-col items-center justify-between gap-0.5 overflow-hidden bg-white px-1.5 py-1 text-black">

            {/* نام محصول */}
            <div
                className="w-full text-center text-[9px] font-extrabold leading-[1.2] break-words"
                style={{
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden',
                }}
            >
                {line.productName}
            </div>

            {/* سایز و رنگ */}
            <div
                className="w-full text-center text-[7px] font-semibold leading-tight overflow-hidden"
                style={{
                    display: '-webkit-box',
                    WebkitLineClamp: 1,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden',
                }}
            >
                {line.variantText}
            </div>

            {/* بارکد Code 128 - هشت رقم */}
            <div className="flex w-full flex-1 items-center justify-center overflow-hidden">
                <BarcodeSvg value={printableBarcode} />
            </div>

            {/* قیمت */}
            <div className="w-full text-center text-[8px] font-extrabold leading-tight whitespace-nowrap">
                {formatMoney(line.price, line.currency, locale)}
            </div>

            {/* عدد زیر بارکد */}
            <div className="text-[7px] font-bold tracking-wider leading-none">
                {printableBarcode}
            </div>
        </div>
    );
}
// ---------------------------------------------------------
// Help item
// ---------------------------------------------------------

function HelpItem({
    number,
    title,
    description,
}: {
    number: number;
    title: string;
    description: string;
}) {
    return (
        <div className="flex gap-3">
            <div className="flex size-7 shrink-0 items-center justify-center rounded-full bg-blue-600 text-sm font-bold text-white">
                {number}
            </div>

            <div className="min-w-0">
                <div className="text-sm font-bold text-slate-800">
                    {title}
                </div>

                <p className="mt-1 text-xs leading-6 text-slate-600">
                    {description}
                </p>
            </div>
        </div>
    );
}

// =========================================================
// Main page
// =========================================================

export default function BarcodeLabelsPage() {
    useDocumentTitle('چاپ برچسب بارکد');

    const canManageProducts = usePermission(PERM.productsUpdate);

    // Product selection
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [selectedProductId, setSelectedProductId] = useState('');

    // Variant quantities
    const [quantities, setQuantities] = useState<Record<string, number>>({});

    // Print queue
    const [lines, setLines] = useState<BarcodeLabelLine[]>([]);

    // Print format
    const [format, setFormat] = useState<PrintFormat>('thermal');

    // Thermal settings
    const [thermalSettings, setThermalSettings] =
        useState<ThermalPrintSettings>(loadThermalSettings);

    // Keep raw text while typing. An empty input remains empty.
    const [thermalDrafts, setThermalDrafts] =
        useState<Partial<Record<ThermalNumericKey, string>>>({});

    const [preparing, setPreparing] = useState(false);
    const [showPreview, setShowPreview] = useState(false);

    const locale = 'fa-IR';

    // -----------------------------------------------------
    // Save settings
    // -----------------------------------------------------

    useEffect(() => {
        try {
            window.localStorage.setItem(
                THERMAL_SETTINGS_KEY,
                JSON.stringify(thermalSettings),
            );
        } catch {
            // Browser storage is optional.
        }
    }, [thermalSettings]);

    // -----------------------------------------------------
    // Product queries
    // -----------------------------------------------------

    const productsQuery = useQuery({
        queryKey: ['barcode-labels', 'products', search, page],

        queryFn: () =>
            productsApi.getAdminAll({
                page,
                pageSize: 20,
                search: search.trim() || undefined,
                sortBy: 'newest',
            }),

        enabled: canManageProducts,
        staleTime: 15_000,
    });

    const productQuery = useQuery({
        queryKey: ['barcode-labels', 'product', selectedProductId],

        queryFn: () => productsApi.getById(selectedProductId),

        enabled: canManageProducts && Boolean(selectedProductId),
        staleTime: 15_000,
    });

    const product = productQuery.data as Product | undefined;
    const variants = product?.variants ?? [];

    // -----------------------------------------------------
    // Print calculations
    // -----------------------------------------------------

    const totalLabels = useMemo(
        () => lines.reduce(
            (sum, line) => sum + line.quantity,
            0,
        ),
        [lines],
    );

    const expandedLabels = useMemo(
        () =>
            lines.flatMap((line) =>
                Array.from(
                    { length: line.quantity },
                    (_, copyIndex) => ({
                        ...line,
                        printKey: `${line.variantId}-${copyIndex}`,
                    }),
                ),
            ),
        [lines],
    );

    // Physical width/height of a label according to orientation.
    const thermalLabelWidthMm =
        thermalSettings.orientation === 'normal'
            ? safeNumber(thermalSettings.labelWidthMm)
            : safeNumber(thermalSettings.labelHeightMm);

    const thermalLabelHeightMm =
        thermalSettings.orientation === 'normal'
            ? safeNumber(thermalSettings.labelHeightMm)
            : safeNumber(thermalSettings.labelWidthMm);

    const horizontalGap = safeNumber(thermalSettings.horizontalGapMm);
    const verticalGap = safeNumber(thermalSettings.verticalGapMm);

    const marginLeft = safeNumber(thermalSettings.marginLeftMm);
    const marginRight = safeNumber(thermalSettings.marginRightMm);
    const marginTop = safeNumber(thermalSettings.marginTopMm);
    const marginBottom = safeNumber(thermalSettings.marginBottomMm);

    const thermalRequiredWidthMm =
        thermalSettings.columns * thermalLabelWidthMm
        + (thermalSettings.columns - 1) * horizontalGap
        + marginLeft
        + marginRight;

    const thermalPageHeightMm =
        thermalLabelHeightMm
        + verticalGap
        + marginTop
        + marginBottom;

    // If roll width is empty, the printer driver determines paper width.
    const thermalEffectiveRollWidthMm =
        thermalSettings.rollWidthMm ?? thermalRequiredWidthMm;

    // -----------------------------------------------------
    // Validation
    // -----------------------------------------------------

    const thermalErrors = useMemo(() => {
        const errors: string[] = [];

        // Check invalid text still being edited.
        for (const raw of Object.values(thermalDrafts)) {
            if (raw === undefined || raw.trim() === '') {
                continue;
            }

            if (!Number.isFinite(Number(raw))) {
                errors.push('یکی از مقادیر عددی معتبر نیست.');
                break;
            }
        }

        if (thermalSettings.rollWidthMm !== null) {
            if (
                !Number.isFinite(thermalSettings.rollWidthMm) ||
                thermalSettings.rollWidthMm <= 0
            ) {
                errors.push('عرض رول باید بیشتر از صفر باشد.');
            }
        }

        if (
            thermalSettings.labelWidthMm === null ||
            !Number.isFinite(thermalSettings.labelWidthMm) ||
            thermalSettings.labelWidthMm <= 0
        ) {
            errors.push('عرض لیبل باید بیشتر از صفر باشد.');
        }

        if (
            thermalSettings.labelHeightMm === null ||
            !Number.isFinite(thermalSettings.labelHeightMm) ||
            thermalSettings.labelHeightMm <= 0
        ) {
            errors.push('ارتفاع لیبل باید بیشتر از صفر باشد.');
        }

        const nonNegativeValues = [
            thermalSettings.horizontalGapMm,
            thermalSettings.verticalGapMm,
            thermalSettings.marginLeftMm,
            thermalSettings.marginRightMm,
            thermalSettings.marginTopMm,
            thermalSettings.marginBottomMm,
        ];

        if (
            nonNegativeValues.some(
                (value) =>
                    value !== null &&
                    (!Number.isFinite(value) || value < 0),
            )
        ) {
            errors.push('فاصله‌ها و حاشیه‌ها نمی‌توانند منفی باشند.');
        }

        if (
            thermalSettings.rollWidthMm !== null &&
            thermalRequiredWidthMm > thermalSettings.rollWidthMm
        ) {
            errors.push(
                `عرض موردنیاز ${thermalRequiredWidthMm.toFixed(1)} میلی‌متر است، اما عرض رول ${thermalSettings.rollWidthMm} میلی‌متر وارد شده است.`,
            );
        }

        return errors;
    }, [
        thermalDrafts,
        thermalSettings,
        thermalRequiredWidthMm,
    ]);

    const thermalLayoutValid = thermalErrors.length === 0;

    // -----------------------------------------------------
    // Group labels by physical row
    // -----------------------------------------------------

    const thermalRows = useMemo(() => {
        const rows: (typeof expandedLabels)[] = [];

        const columns = thermalSettings.columns;

        for (
            let index = 0;
            index < expandedLabels.length;
            index += columns
        ) {
            rows.push(
                expandedLabels.slice(index, index + columns),
            );
        }

        return rows;
    }, [expandedLabels, thermalSettings.columns]);

    // -----------------------------------------------------
    // Numeric settings update
    // No clamping and no forced value.
    // -----------------------------------------------------

    const updateThermalNumber = (
        key: ThermalNumericKey,
        rawValue: string,
    ) => {
        setThermalDrafts((current) => ({
            ...current,
            [key]: rawValue,
        }));

        if (rawValue.trim() === '') {
            setThermalSettings((current) => ({
                ...current,
                [key]: null,
            }));

            return;
        }

        const parsed = Number(rawValue);

        if (!Number.isFinite(parsed)) {
            return;
        }

        // Save exactly what the user entered.
        setThermalSettings((current) => ({
            ...current,
            [key]: parsed,
        }));
    };

    const getThermalInputValue = (
        key: ThermalNumericKey,
    ): string => {
        const draft = thermalDrafts[key];

        if (draft !== undefined) {
            return draft;
        }

        const value = thermalSettings[key];

        return value === null ? '' : String(value);
    };

    const updateOrientation = (orientation: ThermalOrientation) => {
        setThermalSettings((current) => ({
            ...current,
            orientation,
        }));
    };

    const resetThermalSettings = () => {
        setThermalSettings(DEFAULT_THERMAL_SETTINGS);
        setThermalDrafts({});

        toast.success('تنظیمات به حالت اولیه بازگشت.');
    };

    // -----------------------------------------------------
    // Product search
    // -----------------------------------------------------

    const changeSearch = (value: string) => {
        setSearch(value);
        setPage(1);
    };

    // -----------------------------------------------------
    // Add product variants
    // -----------------------------------------------------

    const addSelectedVariants = async () => {
        if (!product) return;

        const selected = variants.filter(
            (variant) =>
                variant.isActive &&
                (quantities[variant.id] ?? 0) > 0,
        );

        if (selected.length === 0) {
            toast.error(
                'برای حداقل یک تنوع، تعداد برچسب را وارد کنید.',
            );
            return;
        }

        setPreparing(true);

        try {
            const preparedLines: BarcodeLabelLine[] = [];

            for (const variant of selected) {
                const requestedQuantity = Math.max(
                    1,
                    Math.min(
                        500,
                        Math.floor(quantities[variant.id] ?? 1),
                    ),
                );

                const barcode =
                    variant.barcode?.trim() ||
                    (
                        await productsApi.ensureVariantBarcode(
                            product.id,
                            variant.id,
                        )
                    ).barcode;

                if (!barcode) {
                    throw new Error(
                        `بارکد تنوع ${variant.sku} دریافت نشد.`,
                    );
                }

                const variantBasePrice =
                    variant.priceOverride ?? product.price;

                const unitPrice =
                    product.price > 0
                        ? variantBasePrice *
                        (product.finalPrice / product.price)
                        : variantBasePrice;

                preparedLines.push({
                    variantId: variant.id,
                    productId: product.id,
                    productName: product.name,
                    sku: variant.sku,
                    barcode,
                    variantText: variantDescription(variant),
                    price: unitPrice,
                    currency: product.currency || 'IRR',
                    quantity: requestedQuantity,
                });
            }

            setLines((current) => {
                const next = [...current];

                for (const incoming of preparedLines) {
                    const index = next.findIndex(
                        (line) =>
                            line.variantId === incoming.variantId,
                    );

                    if (index >= 0) {
                        next[index] = {
                            ...next[index],
                            barcode: incoming.barcode,
                            quantity: Math.min(
                                500,
                                next[index].quantity +
                                incoming.quantity,
                            ),
                        };
                    } else {
                        next.push(incoming);
                    }
                }

                return next;
            });

            toast.success(
                `${preparedLines.length} تنوع به فهرست چاپ اضافه شد.`,
            );
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : 'آماده‌سازی برچسب ناموفق بود.',
            );
        } finally {
            setPreparing(false);
        }
    };

    // -----------------------------------------------------
    // Label queue
    // -----------------------------------------------------

    const updateLabelQuantity = (
        variantId: string,
        rawQuantity: string,
    ) => {
        const parsed = Number(rawQuantity);

        if (!Number.isFinite(parsed) || rawQuantity.trim() === '') {
            return;
        }

        const quantity = Math.max(
            1,
            Math.min(500, Math.floor(parsed)),
        );

        setLines((current) =>
            current.map((line) =>
                line.variantId === variantId
                    ? { ...line, quantity }
                    : line,
            ),
        );
    };

    const removeLine = (variantId: string) => {
        setLines((current) =>
            current.filter(
                (line) => line.variantId !== variantId,
            ),
        );
    };

    // -----------------------------------------------------
    // Print
    // -----------------------------------------------------

    const printLabels = () => {
        if (expandedLabels.length === 0) {
            toast.error('فهرست برچسب‌ها خالی است.');
            return;
        }

        if (format === 'thermal' && !thermalLayoutValid) {
            toast.error(
                'تنظیمات چاپ دارای خطاست. پیام خطا را در بخش تنظیمات بررسی کنید.',
            );
            return;
        }

        window.print();
    };

    // -----------------------------------------------------
    // Permission
    // -----------------------------------------------------

    if (!canManageProducts) {
        return (
            <Card dir="rtl">
                <CardContent className="p-6 text-sm">
                    برای چاپ و ایجاد بارکد، مجوز{' '}
                    <code>products.update</code> لازم است.
                </CardContent>
            </Card>
        );
    }

    // -----------------------------------------------------
    // Main UI
    // -----------------------------------------------------

    return (
        <div dir="rtl" className="space-y-6">

            {/* Print CSS */}
            <style>{`
    .barcode-print-surface {
        display: none;
    }

    @page {
        size: ${format === 'thermal'
                    ? thermalSettings.rollWidthMm !== null
                        ? `${thermalSettings.rollWidthMm}mm ${thermalPageHeightMm}mm`
                        : 'auto'
                    : 'A4'
                };

        margin: ${format === 'thermal' ? '0' : '6mm'};
    }

  @media print {

    /* --------------------------------
       تنظیمات عمومی چاپ
    -------------------------------- */

    html,
    body,
    #root {
        width: auto !important;
        height: auto !important;
        min-height: 0 !important;
        margin: 0 !important;
        padding: 0 !important;
    }

    body * {
        visibility: hidden !important;
    }

    .barcode-screen-only {
        display: none !important;
    }

    .barcode-print-surface,
    .barcode-print-surface * {
        visibility: visible !important;
    }

    .barcode-print-surface {
        display: block !important;
        background: #fff !important;
        color: #000 !important;
    }


    /* --------------------------------
       چاپ A4
    -------------------------------- */

    .barcode-print-surface.mode-a4 {
        position: absolute;
        inset: 0;
        width: 100%;
    }

    .barcode-print-surface.mode-a4 .barcode-print-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 2mm;
    }

    .barcode-print-surface.mode-a4 .barcode-label {
        box-sizing: border-box;
        min-width: 0;
        height: 30mm;
        padding: 1.5mm;
        overflow: hidden;
        border: 0.2mm solid #bbb;
        break-inside: avoid;
        background: #fff !important;
        color: #000 !important;
    }


    /* --------------------------------
       چاپگر حرارتی
    -------------------------------- */

    .barcode-print-surface.mode-thermal {
        position: absolute !important;

        top: 0 !important;
        left: 0 !important;
        right: auto !important;
        bottom: auto !important;

        width: ${thermalEffectiveRollWidthMm}mm !important;

        margin: 0 !important;
        padding: 0 !important;
    }

    /* حذف فاصله‌های اضافی والد */

    body .space-y-6 > .barcode-print-surface.mode-thermal {
        margin-top: 0 !important;
        margin-bottom: 0 !important;
    }


    /* --------------------------------
       محدوده کلی ردیف‌های چاپ
    -------------------------------- */

    .barcode-print-surface.mode-thermal .barcode-print-grid {
        display: flex;
        flex-direction: column;

        width: ${thermalEffectiveRollWidthMm}mm;

        gap: 0;
        margin: 0;
        padding: 0;
    }


    /* --------------------------------
       هر ردیف چاپ
    -------------------------------- */

    .barcode-print-surface.mode-thermal .barcode-print-row {
        display: grid;

        grid-template-columns: repeat(
            ${thermalSettings.columns},
            ${thermalLabelWidthMm}mm
        );

        column-gap: ${horizontalGap}mm;

        align-items: start;
        justify-content: center;

        box-sizing: border-box;

        width: ${thermalEffectiveRollWidthMm}mm;
        height: ${thermalPageHeightMm}mm;

        margin: 0;

        padding:
            ${marginTop}mm
            ${marginRight}mm
            ${marginBottom}mm
            ${marginLeft}mm;

        break-after: page;
        page-break-after: always;
    }

    .barcode-print-surface.mode-thermal
    .barcode-print-row:last-child {
        break-after: auto;
        page-break-after: auto;
    }


    /* --------------------------------
       اندازه فیزیکی هر لیبل
    -------------------------------- */

    .barcode-print-surface.mode-thermal .barcode-label {
        position: relative;

        box-sizing: border-box;

        width: ${thermalLabelWidthMm}mm;
        height: ${thermalLabelHeightMm}mm;

        min-width: 0;

        margin: 0;
        padding: 0;

        overflow: hidden;

        border: none;

        break-inside: avoid;

        background: #fff !important;
        color: #000 !important;
    }


    /* --------------------------------
       محتوای داخل لیبل
    -------------------------------- */

    .barcode-print-surface.mode-thermal .barcode-label-content {
        width: 100%;
        height: 100%;
    }


    /* --------------------------------
       چرخش محتوای لیبل
    -------------------------------- */

    .barcode-print-surface.mode-thermal
    .barcode-label.is-rotated
    .barcode-label-content {

        position: absolute;

        top: 50%;
        left: 50%;

        width: ${safeNumber(thermalSettings.labelWidthMm)}mm;
        height: ${safeNumber(thermalSettings.labelHeightMm)}mm;

        transform: translate(-50%, -50%) rotate(90deg);

        transform-origin: center;
    }


    /* --------------------------------
       اندازه بارکد در چاپ A4
    -------------------------------- */

    .barcode-print-surface.mode-a4 .barcode-svg {
        display: block !important;

        width: 90% !important;
        max-width: 90% !important;

        height: auto !important;

        margin-left: auto !important;
        margin-right: auto !important;
    }


    /* --------------------------------
       اندازه بارکد در چاپ حرارتی
       90 درصد عرض فضای داخلی لیبل
    -------------------------------- */

    .barcode-print-surface.mode-thermal .barcode-svg {
        display: block !important;

        width: 90% !important;
        max-width: 90% !important;

        height: auto !important;

        margin-left: auto !important;
        margin-right: auto !important;

        flex: 0 0 auto !important;
    }

}
`}</style>

            {/* Header */}
            <div className="barcode-screen-only flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="flex items-center gap-3 text-2xl font-bold text-slate-800">
                        <span className="flex size-11 items-center justify-center rounded-xl bg-blue-100 text-blue-700">
                            <Printer className="size-6" />
                        </span>

                        چاپ برچسب بارکد
                    </h1>

                    <p className="mt-2 text-sm text-muted-foreground">
                        تنظیمات دقیق رول، ابعاد لیبل و چیدمان چاپ
                    </p>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm font-medium text-muted-foreground">
                        قالب چاپ
                    </span>

                    <select
                        value={format}
                        onChange={(event) =>
                            setFormat(event.target.value as PrintFormat)
                        }
                        className="h-10 rounded-lg border bg-white px-3 text-sm"
                    >
                        <option value="thermal">
                            رول حرارتی INNOVERS LP-203
                        </option>

                        <option value="a4">
                            برگه A4
                        </option>
                    </select>
                </div>
            </div>

            {/* Main columns */}
            <div className="barcode-screen-only grid items-start gap-5 xl:grid-cols-[minmax(0,1.7fr)_minmax(310px,0.85fr)]">

                {/* Main column */}
                <div className="min-w-0 space-y-5">

                    {/* 1. Product selection */}
                    <Card className="overflow-hidden">
                        <CardHeader className="border-b bg-slate-50/80">
                            <CardTitle className="flex items-center gap-3 text-lg">
                                <span className="flex size-8 items-center justify-center rounded-full bg-blue-600 text-sm text-white">
                                    ۱
                                </span>

                                انتخاب محصول
                            </CardTitle>

                            <CardDescription>
                                ابتدا محصول موردنظر را جست‌وجو و انتخاب کنید.
                            </CardDescription>
                        </CardHeader>

                        <CardContent className="space-y-4 p-5">
                            <div className="grid gap-3 sm:grid-cols-[1fr_auto]">
                                <Input
                                    value={search}
                                    onChange={(event) =>
                                        changeSearch(event.target.value)
                                    }
                                    placeholder="جست‌وجوی نام محصول یا SKU..."
                                    className="h-12"
                                    onKeyDown={(event) => {
                                        if (event.key === 'Enter') {
                                            event.preventDefault();
                                            void productsQuery.refetch();
                                        }
                                    }}
                                />

                                <Button
                                    type="button"
                                    variant="outline"
                                    className="h-12"
                                    disabled={productsQuery.isFetching}
                                    onClick={() =>
                                        void productsQuery.refetch()
                                    }
                                >
                                    <Search className="ms-2 size-4" />
                                    جست‌وجو
                                </Button>
                            </div>

                            {productsQuery.isPending ? (
                                <p className="text-sm text-muted-foreground">
                                    در حال بارگذاری محصولات...
                                </p>
                            ) : productsQuery.isError ? (
                                <div className="flex flex-wrap items-center justify-between gap-3 text-sm">
                                    <span>
                                        بارگذاری محصولات ناموفق بود.
                                    </span>

                                    <Button
                                        variant="outline"
                                        onClick={() =>
                                            void productsQuery.refetch()
                                        }
                                    >
                                        تلاش دوباره
                                    </Button>
                                </div>
                            ) : (
                                <>
                                    <div className="flex items-center gap-3">
                                        <Package className="size-5 shrink-0 text-blue-600" />

                                        <select
                                            value={selectedProductId}
                                            onChange={(event) => {
                                                setSelectedProductId(
                                                    event.target.value,
                                                );

                                                setQuantities({});
                                            }}
                                            className="h-12 min-w-0 flex-1 rounded-lg border bg-white px-3 text-sm"
                                        >
                                            <option value="">
                                                انتخاب محصول...
                                            </option>

                                            {productsQuery.data.items.map(
                                                (item) => (
                                                    <option
                                                        key={item.id}
                                                        value={item.id}
                                                    >
                                                        {item.name}
                                                        {item.sku
                                                            ? ` (${item.sku})`
                                                            : ''}
                                                    </option>
                                                ),
                                            )}
                                        </select>
                                    </div>

                                    {productsQuery.data.items.length === 0 && (
                                        <p className="text-sm text-muted-foreground">
                                            محصولی یافت نشد.
                                        </p>
                                    )}

                                    <div className="flex items-center justify-between border-t pt-3 text-sm">
                                        <span className="text-muted-foreground">
                                            صفحه {productsQuery.data.page} از{' '}
                                            {productsQuery.data.totalPages || 1}
                                        </span>

                                        <div className="flex gap-2">
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                disabled={page <= 1}
                                                onClick={() =>
                                                    setPage((value) =>
                                                        Math.max(1, value - 1),
                                                    )
                                                }
                                            >
                                                قبلی
                                            </Button>

                                            <Button
                                                variant="outline"
                                                size="sm"
                                                disabled={
                                                    page >=
                                                    productsQuery.data.totalPages
                                                }
                                                onClick={() =>
                                                    setPage((value) => value + 1)
                                                }
                                            >
                                                بعدی
                                            </Button>
                                        </div>
                                    </div>
                                </>
                            )}
                        </CardContent>
                    </Card>

                    {/* 2. Thermal settings */}
                    {format === 'thermal' && (
                        <Card className="overflow-hidden">
                            <CardHeader className="border-b bg-slate-50/80">
                                <CardTitle className="flex items-center gap-3 text-lg">
                                    <span className="flex size-8 items-center justify-center rounded-full bg-blue-600 text-sm text-white">
                                        ۲
                                    </span>

                                    تنظیمات رول و چیدمان
                                </CardTitle>

                                <CardDescription>
                                    مقادیر را مطابق رول واقعی و اندازه برچسب وارد کنید.
                                    هیچ مقداری به‌صورت خودکار اصلاح نمی‌شود.
                                </CardDescription>
                            </CardHeader>

                            <CardContent className="space-y-5 p-5">

                                {/* First row */}
                                <div className="grid gap-4 rounded-xl bg-slate-50 p-4 sm:grid-cols-3">

                                    <NumberSetting
                                        id="thermal-roll-width"
                                        label="عرض رول (میلی‌متر)"
                                        value={getThermalInputValue('rollWidthMm')}
                                        onChange={(value) =>
                                            updateThermalNumber('rollWidthMm', value)
                                        }
                                        description="مثلاً ۹۰؛ خالی باشد، عرض را درایور چاپگر تعیین می‌کند."
                                    />

                                    <div className="space-y-2">
                                        <label className="text-sm font-semibold text-slate-700">
                                            تعداد ستون
                                        </label>

                                        <div className="grid grid-cols-3 gap-2">
                                            {([1, 2, 3] as const).map((column) => (
                                                <Button
                                                    key={column}
                                                    type="button"
                                                    variant={
                                                        thermalSettings.columns === column
                                                            ? 'default'
                                                            : 'outline'
                                                    }
                                                    className="h-11 text-base"
                                                    onClick={() =>
                                                        setThermalSettings((current) => ({
                                                            ...current,
                                                            columns: column,
                                                        }))
                                                    }
                                                >
                                                    {column} ستون
                                                </Button>
                                            ))}
                                        </div>

                                        <p className="text-xs text-muted-foreground">
                                            تعداد لیبل در هر ردیف رول
                                        </p>

                                        <p className="text-xs font-medium text-blue-700">
                                            {thermalSettings.columns === 1 &&
                                                'در هر ردیف یک لیبل چاپ می‌شود.'}

                                            {thermalSettings.columns === 2 &&
                                                'در هر ردیف دو لیبل کنار هم چاپ می‌شوند.'}

                                            {thermalSettings.columns === 3 &&
                                                'در هر ردیف سه لیبل کنار هم چاپ می‌شوند.'}
                                        </p>
                                    </div>

                                    <div className="space-y-2">
                                        <label className="text-sm font-semibold text-slate-700">
                                            جهت لیبل
                                        </label>

                                        <div className="grid grid-cols-2 gap-2">
                                            <Button
                                                type="button"
                                                variant={
                                                    thermalSettings.orientation === 'normal'
                                                        ? 'default'
                                                        : 'outline'
                                                }
                                                className="h-11 px-2 text-xs"
                                                onClick={() =>
                                                    updateOrientation('normal')
                                                }
                                            >
                                                عادی
                                                <br />
                                                ۴۵ × ۳۰
                                            </Button>

                                            <Button
                                                type="button"
                                                variant={
                                                    thermalSettings.orientation === 'rotated'
                                                        ? 'default'
                                                        : 'outline'
                                                }
                                                className="h-11 px-2 text-xs"
                                                onClick={() =>
                                                    updateOrientation('rotated')
                                                }
                                            >
                                                <RotateCw className="ms-1 size-3" />
                                                چرخیده
                                                <br />
                                                ۳۰ × ۴۵
                                            </Button>
                                        </div>
                                    </div>
                                </div>

                                {/* Label dimensions */}
                                <div>
                                    <div className="mb-3 flex items-center gap-2">
                                        <Settings2 className="size-4 text-blue-600" />

                                        <h3 className="text-sm font-bold text-slate-800">
                                            ابعاد هر لیبل
                                        </h3>
                                    </div>

                                    <div className="grid gap-4 sm:grid-cols-2">
                                        <NumberSetting
                                            id="thermal-label-width"
                                            label="عرض اصلی لیبل (میلی‌متر)"
                                            value={getThermalInputValue('labelWidthMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('labelWidthMm', value)
                                            }
                                            description="مقدار اولیه: ۴۵ میلی‌متر"
                                        />

                                        <NumberSetting
                                            id="thermal-label-height"
                                            label="ارتفاع اصلی لیبل (میلی‌متر)"
                                            value={getThermalInputValue('labelHeightMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('labelHeightMm', value)
                                            }
                                            description="مقدار اولیه: ۳۰ میلی‌متر"
                                        />
                                    </div>
                                </div>

                                {/* Gaps */}
                                <div>
                                    <div className="mb-3 flex items-center gap-2">
                                        <Settings2 className="size-4 text-blue-600" />

                                        <h3 className="text-sm font-bold text-slate-800">
                                            فاصله بین لیبل‌ها
                                        </h3>
                                    </div>

                                    <div className="grid gap-4 sm:grid-cols-2">
                                        <NumberSetting
                                            id="thermal-horizontal-gap"
                                            label="فاصله افقی (میلی‌متر)"
                                            value={getThermalInputValue('horizontalGapMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('horizontalGapMm', value)
                                            }
                                            description="فاصله بین لیبل‌های کنار هم"
                                        />

                                        <NumberSetting
                                            id="thermal-vertical-gap"
                                            label="فاصله عمودی (میلی‌متر)"
                                            value={getThermalInputValue('verticalGapMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('verticalGapMm', value)
                                            }
                                            description="فاصله بین دو ردیف در مسیر حرکت رول"
                                        />
                                    </div>
                                </div>

                                {/* Margins */}
                                <div>
                                    <div className="mb-3 flex items-center gap-2">
                                        <Settings2 className="size-4 text-blue-600" />

                                        <h3 className="text-sm font-bold text-slate-800">
                                            حاشیه‌های چاپ
                                        </h3>
                                    </div>

                                    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                                        <NumberSetting
                                            id="thermal-margin-left"
                                            label="حاشیه چپ"
                                            value={getThermalInputValue('marginLeftMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('marginLeftMm', value)
                                            }
                                        />

                                        <NumberSetting
                                            id="thermal-margin-right"
                                            label="حاشیه راست"
                                            value={getThermalInputValue('marginRightMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('marginRightMm', value)
                                            }
                                        />

                                        <NumberSetting
                                            id="thermal-margin-top"
                                            label="حاشیه بالا"
                                            value={getThermalInputValue('marginTopMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('marginTopMm', value)
                                            }
                                        />

                                        <NumberSetting
                                            id="thermal-margin-bottom"
                                            label="حاشیه پایین"
                                            value={getThermalInputValue('marginBottomMm')}
                                            onChange={(value) =>
                                                updateThermalNumber('marginBottomMm', value)
                                            }
                                        />
                                    </div>
                                </div>

                                {/* Calculated width */}
                                <div className="rounded-xl border bg-slate-50 p-4">
                                    <div className="flex flex-wrap items-center justify-between gap-3">
                                        <div>
                                            <div className="text-sm font-bold text-slate-800">
                                                عرض موردنیاز چیدمان
                                            </div>

                                            <div className="mt-1 text-xs text-muted-foreground">
                                                مجموع عرض لیبل‌ها، فاصله‌ها و حاشیه‌های چپ و راست
                                            </div>
                                        </div>

                                        <div className="text-xl font-bold text-blue-700">
                                            {thermalRequiredWidthMm.toFixed(1)} mm
                                        </div>
                                    </div>

                                    <div className="mt-3 flex flex-wrap items-center gap-2 text-sm text-slate-600">
                                        <span>
                                            عرض رول:{' '}
                                            {thermalSettings.rollWidthMm === null
                                                ? 'طبق تنظیمات درایور'
                                                : `${thermalSettings.rollWidthMm} mm`}
                                        </span>

                                        <span className="text-slate-300">|</span>

                                        <span>
                                            ابعاد چاپی هر لیبل:{' '}
                                            {thermalLabelWidthMm} × {thermalLabelHeightMm} mm
                                        </span>
                                    </div>

                                    {thermalErrors.length > 0 && (
                                        <div className="mt-4 space-y-2 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-700">
                                            <div className="flex items-center gap-2 font-bold">
                                                <AlertCircle className="size-5" />
                                                خطا در تنظیمات چاپ
                                            </div>

                                            {thermalErrors.map((error, index) => (
                                                <p key={index}>
                                                    {index + 1}. {error}
                                                </p>
                                            ))}
                                        </div>
                                    )}
                                </div>

                                <div className="flex justify-end">
                                    <Button
                                        type="button"
                                        variant="outline"
                                        onClick={resetThermalSettings}
                                    >
                                        <RefreshCw className="ms-2 size-4" />
                                        بازنشانی تنظیمات
                                    </Button>
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* 3. Variant selection */}
                    <Card className="overflow-hidden">
                        <CardHeader className="border-b bg-slate-50/80">
                            <CardTitle className="flex items-center gap-3 text-lg">
                                <span className="flex size-8 items-center justify-center rounded-full bg-blue-600 text-sm text-white">
                                    ۳
                                </span>

                                انتخاب تنوع و تعداد
                            </CardTitle>

                            <CardDescription>
                                برای هر سایز و رنگ، تعداد برچسب موردنیاز را مشخص کنید.
                            </CardDescription>
                        </CardHeader>

                        <CardContent className="space-y-4 p-5">
                            {!selectedProductId ? (
                                <p className="text-sm text-muted-foreground">
                                    ابتدا محصول را انتخاب کنید.
                                </p>
                            ) : productQuery.isPending ? (
                                <p className="text-sm text-muted-foreground">
                                    در حال دریافت تنوع‌های محصول...
                                </p>
                            ) : productQuery.isError || !product ? (
                                <div className="space-y-3 text-sm">
                                    <p>دریافت اطلاعات محصول ناموفق بود.</p>

                                    <Button
                                        variant="outline"
                                        onClick={() => void productQuery.refetch()}
                                    >
                                        تلاش دوباره
                                    </Button>
                                </div>
                            ) : variants.length === 0 ? (
                                <p className="text-sm text-muted-foreground">
                                    برای این محصول تنوعی ثبت نشده است.
                                </p>
                            ) : (
                                <>
                                    <div className="font-semibold text-slate-800">
                                        {product.name}
                                    </div>

                                    <div className="overflow-x-auto rounded-lg border">
                                        <table className="w-full min-w-[550px] text-sm">
                                            <thead className="bg-slate-50">
                                                <tr>
                                                    <th className="p-3 text-right">
                                                        تنوع
                                                    </th>

                                                    <th className="p-3 text-right">
                                                        SKU
                                                    </th>

                                                    <th className="p-3 text-right">
                                                        بارکد
                                                    </th>

                                                    <th className="w-28 p-3 text-right">
                                                        تعداد
                                                    </th>
                                                </tr>
                                            </thead>

                                            <tbody>
                                                {variants.map((variant) => (
                                                    <tr
                                                        key={variant.id}
                                                        className="border-t"
                                                    >
                                                        <td className="p-3">
                                                            {variantDescription(variant)}

                                                            {!variant.isActive && (
                                                                <div className="mt-1 text-xs text-red-600">
                                                                    تنوع غیرفعال
                                                                </div>
                                                            )}
                                                        </td>

                                                        <td className="p-3 font-mono text-xs">
                                                            {variant.sku}
                                                        </td>

                                                        <td className="p-3 font-mono text-xs">
                                                            {variant.barcode || (
                                                                <span className="text-amber-700">
                                                                    هنگام افزودن ساخته می‌شود
                                                                </span>
                                                            )}
                                                        </td>

                                                        <td className="p-3">
                                                            <Input
                                                                type="number"
                                                                step="1"
                                                                min="0"
                                                                max="500"
                                                                value={quantities[variant.id] ?? 0}
                                                                disabled={!variant.isActive}
                                                                onChange={(event) => {
                                                                    const raw = event.target.value;

                                                                    if (raw.trim() === '') {
                                                                        setQuantities((current) => ({
                                                                            ...current,
                                                                            [variant.id]: 0,
                                                                        }));
                                                                        return;
                                                                    }

                                                                    const parsed = Number(raw);

                                                                    if (!Number.isFinite(parsed)) {
                                                                        return;
                                                                    }

                                                                    setQuantities((current) => ({
                                                                        ...current,
                                                                        [variant.id]: Math.max(
                                                                            0,
                                                                            Math.min(
                                                                                500,
                                                                                Math.floor(parsed),
                                                                            ),
                                                                        ),
                                                                    }));
                                                                }}
                                                            />
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>

                                    <div className="flex flex-wrap items-center justify-between gap-3">
                                        <p className="text-xs text-muted-foreground">
                                            بارکد تنوع‌هایی که بارکد ندارند، از API موجود ایجاد و ذخیره می‌شود.
                                        </p>

                                        <Button
                                            onClick={() => void addSelectedVariants()}
                                            disabled={
                                                preparing ||
                                                !variants.some(
                                                    (variant) =>
                                                        variant.isActive &&
                                                        (quantities[variant.id] ?? 0) > 0,
                                                )
                                            }
                                        >
                                            <Plus className="ms-2 size-4" />

                                            {preparing
                                                ? 'در حال آماده‌سازی...'
                                                : 'افزودن به فهرست چاپ'}
                                        </Button>
                                    </div>
                                </>
                            )}
                        </CardContent>
                    </Card>

                    {/* 4. Print queue */}
                    <Card className="overflow-hidden">
                        <CardHeader className="border-b bg-slate-50/80">
                            <div className="flex flex-wrap items-center justify-between gap-3">
                                <div>
                                    <CardTitle className="flex items-center gap-3 text-lg">
                                        <span className="flex size-8 items-center justify-center rounded-full bg-blue-600 text-sm text-white">
                                            ۴
                                        </span>

                                        فهرست برچسب‌ها
                                    </CardTitle>

                                    <CardDescription className="mt-2">
                                        تعداد کل قابل چاپ: {totalLabels}
                                    </CardDescription>
                                </div>

                                <Button
                                    type="button"
                                    variant="outline"
                                    disabled={lines.length === 0}
                                    onClick={() => setLines([])}
                                >
                                    <Trash2 className="ms-2 size-4" />
                                    خالی‌کردن فهرست
                                </Button>
                            </div>
                        </CardHeader>

                        <CardContent className="p-5">
                            {lines.length === 0 ? (
                                <div className="rounded-xl border border-dashed p-8 text-center">
                                    <Barcode className="mx-auto size-10 text-slate-300" />

                                    <p className="mt-3 text-sm text-muted-foreground">
                                        هنوز برچسبی به فهرست اضافه نشده است.
                                    </p>
                                </div>
                            ) : (
                                <div className="grid gap-4 sm:grid-cols-2">
                                    {lines.map((line) => (
                                        <div
                                            key={line.variantId}
                                            className="space-y-3 rounded-xl border p-4"
                                        >
                                            <div className="flex items-start justify-between gap-3">
                                                <div className="min-w-0">
                                                    <div className="truncate font-semibold">
                                                        {line.productName}
                                                    </div>

                                                    <div className="mt-1 text-xs text-muted-foreground">
                                                        {line.variantText}
                                                    </div>

                                                    <div className="mt-1 font-mono text-xs">
                                                        {line.barcode}
                                                    </div>
                                                </div>

                                                <Button
                                                    type="button"
                                                    variant="ghost"
                                                    size="icon"
                                                    aria-label="حذف از فهرست"
                                                    onClick={() =>
                                                        removeLine(line.variantId)
                                                    }
                                                >
                                                    <Trash2 className="size-4" />
                                                </Button>
                                            </div>

                                            <div className="rounded-lg border bg-white p-3">
                                                <div className="mx-auto h-[105px] w-full max-w-[230px]">
                                                    <LabelContent
                                                        line={line}
                                                        locale={locale}
                                                    />
                                                </div>
                                            </div>

                                            <div className="flex items-center justify-between gap-3">
                                                <label className="text-sm">
                                                    تعداد نسخه
                                                </label>

                                                <Input
                                                    className="w-24"
                                                    type="number"
                                                    min="1"
                                                    max="500"
                                                    step="1"
                                                    value={line.quantity}
                                                    onChange={(event) =>
                                                        updateLabelQuantity(
                                                            line.variantId,
                                                            event.target.value,
                                                        )
                                                    }
                                                />
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    {/* Actions */}
                    <div className="flex flex-wrap items-center gap-3">
                        <Button
                            type="button"
                            variant="outline"
                            className="h-12 flex-1"
                            disabled={expandedLabels.length === 0}
                            onClick={() => setShowPreview((value) => !value)}
                        >
                            <Eye className="ms-2 size-4" />

                            {showPreview
                                ? 'بستن پیش‌نمایش'
                                : 'پیش‌نمایش چاپ'}
                        </Button>

                        <Button
                            type="button"
                            className="h-12 flex-1"
                            disabled={
                                expandedLabels.length === 0 ||
                                (format === 'thermal' && !thermalLayoutValid)
                            }
                            onClick={printLabels}
                        >
                            <Printer className="ms-2 size-4" />
                            چاپ {totalLabels} برچسب
                        </Button>
                    </div>

                    {showPreview && (
                        <Card>
                            <CardHeader>
                                <CardTitle>
                                    پیش‌نمایش برچسب‌ها
                                </CardTitle>
                            </CardHeader>

                            <CardContent>
                                {format === 'thermal' ? (
                                    <div
                                        className="mx-auto space-y-2 overflow-hidden rounded-lg border bg-slate-100 p-2"
                                        style={{
                                            width: '100%',
                                            maxWidth: `${Math.min(
                                                thermalEffectiveRollWidthMm * 3,
                                                700,
                                            )}px`,
                                        }}
                                    >
                                        {thermalRows.slice(0, 8).map((row, rowIndex) => (
                                            <div
                                                key={rowIndex}
                                                className="grid gap-1"
                                                style={{
                                                    gridTemplateColumns: `repeat(${thermalSettings.columns}, minmax(0, 1fr))`,
                                                }}
                                            >
                                                {row.map((line) => (
                                                    <div
                                                        key={line.printKey}
                                                        className="overflow-hidden rounded border bg-white p-1"
                                                        style={{
                                                            aspectRatio:
                                                                thermalSettings.orientation === 'normal'
                                                                    ? '45 / 30'
                                                                    : '30 / 45',
                                                        }}
                                                    >
                                                        <LabelContent
                                                            line={line}
                                                            locale={locale}
                                                        />
                                                    </div>
                                                ))}
                                            </div>
                                        ))}
                                    </div>
                                ) : (
                                    <div className="grid grid-cols-2 gap-2 md:grid-cols-3">
                                        {expandedLabels.slice(0, 18).map((line) => (
                                            <div
                                                key={line.printKey}
                                                className="h-32 overflow-hidden rounded border bg-white p-2"
                                            >
                                                <LabelContent
                                                    line={line}
                                                    locale={locale}
                                                />
                                            </div>
                                        ))}
                                    </div>
                                )}

                                {expandedLabels.length > 18 && (
                                    <p className="mt-3 text-xs text-muted-foreground">
                                        پیش‌نمایش فقط ۱۸ برچسب اول را نشان می‌دهد؛ چاپ شامل همه {totalLabels} برچسب خواهد بود.
                                    </p>
                                )}
                            </CardContent>
                        </Card>
                    )}
                </div>

                {/* Right guide column */}
                <aside className="min-w-0 space-y-5">

                    {/* Help */}
                    <Card className="overflow-hidden">
                        <div className="flex items-center gap-3 bg-blue-600 px-5 py-4 text-white">
                            <Info className="size-5" />

                            <h2 className="font-bold">
                                راهنمای پارامترها
                            </h2>
                        </div>

                        <CardContent className="space-y-5 p-5">
                            <HelpItem
                                number={1}
                                title="عرض رول"
                                description="عرض کاغذ از لبه چپ تا راست. در رول شما مقدار اولیه ۹۰ میلی‌متر است. اگر خالی باشد، اندازه کاغذ از تنظیمات درایور چاپگر گرفته می‌شود."
                            />

                            <HelpItem
                                number={2}
                                title="تعداد ستون"
                                description="تعداد لیبل‌هایی که در یک ردیف و کنار هم در عرض رول چاپ می‌شوند."
                            />

                            <HelpItem
                                number={3}
                                title="جهت لیبل"
                                description="عادی یعنی ضلع ۴۵ میلی‌متری در عرض رول؛ چرخیده یعنی ضلع ۳۰ میلی‌متری در عرض رول قرار می‌گیرد."
                            />

                            <HelpItem
                                number={4}
                                title="عرض و ارتفاع لیبل"
                                description="اندازه واقعی برچسبی که روی رول چسبانده شده است؛ مقدار اولیه ۴۵ در ۳۰ میلی‌متر."
                            />

                            <HelpItem
                                number={5}
                                title="فاصله افقی و عمودی"
                                description="فاصله افقی بین دو لیبل کنار هم و فاصله عمودی بین دو ردیف در مسیر حرکت رول."
                            />

                            <HelpItem
                                number={6}
                                title="حاشیه‌ها"
                                description="فضای خالی از لبه‌های چپ، راست، بالا و پایین محدوده چاپ."
                            />
                        </CardContent>
                    </Card>

                    {/* Visual layout preview */}
                    <Card className="overflow-hidden">
                        <div className="bg-blue-600 px-5 py-4 text-center font-bold text-white">
                            پیش‌نمایش چیدمان رول
                        </div>

                        <CardContent className="space-y-5 p-5">

                            {/* Two columns */}
                            <div>
                                <div className="mb-3 text-center text-sm font-bold text-slate-700">
                                    دو ستون عادی
                                </div>

                                <div className="rounded-lg border-2 border-slate-300 bg-slate-100 p-2">
                                    <div className="grid grid-cols-2 gap-1">
                                        {[1, 2].map((item) => (
                                            <div
                                                key={item}
                                                className="flex aspect-[45/30] flex-col items-center justify-center rounded border-2 border-blue-300 bg-white p-2"
                                            >
                                                <Barcode className="size-5 text-slate-700" />

                                                <div className="mt-1 text-[10px] font-bold">
                                                    45 × 30
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>

                                <p className="mt-2 text-center text-xs text-muted-foreground">
                                    ۴۵ + ۴۵ = ۹۰ میلی‌متر
                                </p>
                            </div>

                            {/* Three columns */}
                            <div>
                                <div className="mb-3 text-center text-sm font-bold text-slate-700">
                                    سه ستون چرخیده
                                </div>

                                <div className="rounded-lg border-2 border-slate-300 bg-slate-100 p-2">
                                    <div className="grid grid-cols-3 gap-1">
                                        {[1, 2, 3].map((item) => (
                                            <div
                                                key={item}
                                                className="flex aspect-[30/45] flex-col items-center justify-center rounded border-2 border-blue-300 bg-white p-1"
                                            >
                                                <Barcode className="size-5 text-slate-700" />

                                                <div className="mt-1 text-[9px] font-bold">
                                                    30 × 45
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>

                                <p className="mt-2 text-center text-xs text-muted-foreground">
                                    ۳۰ + ۳۰ + ۳۰ = ۹۰ میلی‌متر
                                </p>
                            </div>

                            <div className="rounded-lg bg-slate-50 p-3 text-center text-xs leading-6 text-slate-600">
                                <div>
                                    عرض رول:{' '}
                                    {thermalSettings.rollWidthMm === null
                                        ? 'از درایور چاپگر'
                                        : `${thermalSettings.rollWidthMm} mm`}
                                </div>

                                <div>
                                    اندازه اصلی لیبل: ۴۵ × ۳۰ میلی‌متر
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Printer info */}
                    <Card>
                        <CardContent className="space-y-3 p-5">
                            <div className="flex items-center gap-2 font-bold text-slate-800">
                                <Printer className="size-5 text-blue-600" />
                                چاپگر انتخاب‌شده
                            </div>

                            <div className="text-sm font-semibold">
                                INNOVERS LP-203
                            </div>

                            <p className="text-xs leading-6 text-muted-foreground">
                                اندازه کاغذ، نوع سنسور و کالیبراسیون رول باید در تنظیمات درایور ویندوز نیز با رول واقعی هماهنگ باشد.
                            </p>
                        </CardContent>
                    </Card>
                </aside>
            </div>

            {/* Actual print surface */}
            <div
                className={`barcode-print-surface ${format === 'thermal'
                        ? 'mode-thermal'
                        : 'mode-a4'
                    }`}
                dir="rtl"
            >
                <div className="barcode-print-grid">
                    {format === 'a4'
                        ? expandedLabels.map((line) => (
                            <div
                                className="barcode-label"
                                key={line.printKey}
                            >
                                <LabelContent
                                    line={line}
                                    locale={locale}
                                />
                            </div>
                        ))
                        : thermalRows.map((row, rowIndex) => (
                            <div
                                className="barcode-print-row"
                                key={`thermal-row-${rowIndex}`}
                            >
                                {row.map((line) => (
                                    <div
                                        className={`barcode-label ${thermalSettings.orientation === 'rotated'
                                                ? 'is-rotated'
                                                : ''
                                            }`}
                                        key={line.printKey}
                                    >
                                        <LabelContent
                                            line={line}
                                            locale={locale}
                                        />
                                    </div>
                                ))}
                            </div>
                        ))}
                </div>
            </div>
        </div>
    );
}