import { useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Barcode, Printer, Search, Plus, Trash2 } from 'lucide-react';
import JsBarcode from 'jsbarcode';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';
import { formatMoney } from '@/lib/formatMoney';
import { productsApi, type Product, type ProductVariant } from '@/modules/catalog/api/products';

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

function variantDescription(variant: ProductVariant): string {
    const values = (variant.attributes ?? [])
        .map((attribute) => {
            const value = attribute.displayValue?.trim() || attribute.value?.trim();
            if (!value) return null;
            return `${attribute.attributeName}: ${value}`;
        })
        .filter((value): value is string => Boolean(value));

    return values.join(' / ') || 'تنوع پیش‌فرض';
}

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
                width: 1.25,
                height: 34,
                margin: 0,
            });
        } catch {
            if (svgRef.current) svgRef.current.innerHTML = '';
        }
    }, [value]);

    return (
        <svg
            ref={svgRef}
            aria-label={`بارکد ${value}`}
            className="barcode-svg block w-full max-w-full"
        />
    );
}

function LabelContent({ line, locale }: { line: BarcodeLabelLine; locale: string }) {
    return (
        <div className="barcode-label-content flex h-full w-full flex-col items-center justify-between gap-0.5 overflow-hidden text-black">
            <div className="w-full truncate text-center text-[10px] font-bold leading-tight">
                {line.productName}
            </div>
            <div className="w-full truncate text-center text-[9px] leading-tight">
                {line.variantText}
            </div>
            <BarcodeSvg value={line.barcode} />
            <div className="flex w-full items-center justify-between gap-1 text-[8px] leading-tight">
                <span className="truncate">SKU: {line.sku}</span>
                <span className="shrink-0 font-bold">
                    {formatMoney(line.price, line.currency, locale)}
                </span>
            </div>
            <div className="text-[8px] tracking-wide">{line.barcode}</div>
        </div>
    );
}

export default function BarcodeLabelsPage() {
    useDocumentTitle('چاپ برچسب بارکد');

    const canManageProducts = usePermission(PERM.productsUpdate);
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [selectedProductId, setSelectedProductId] = useState('');
    const [quantities, setQuantities] = useState<Record<string, number>>({});
    const [lines, setLines] = useState<BarcodeLabelLine[]>([]);
    const [format, setFormat] = useState<PrintFormat>('a4');
    const [preparing, setPreparing] = useState(false);

    const productsQuery = useQuery({
        queryKey: ['barcode-labels', 'products', search, page],
        queryFn: () => productsApi.getAdminAll({
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
    const locale = 'fa-IR';

    const totalLabels = useMemo(
        () => lines.reduce((sum, line) => sum + line.quantity, 0),
        [lines],
    );

    const expandedLabels = useMemo(
        () => lines.flatMap((line) =>
            Array.from({ length: line.quantity }, (_, copyIndex) => ({
                ...line,
                printKey: `${line.variantId}-${copyIndex}`,
            })),
        ),
        [lines],
    );

    const changeSearch = (value: string) => {
        setSearch(value);
        setPage(1);
    };

    const addSelectedVariants = async () => {
        if (!product) return;

        const selected = variants.filter((variant) =>
            variant.isActive && (quantities[variant.id] ?? 0) > 0,
        );

        if (selected.length === 0) {
            toast.error('برای حداقل یک تنوع، تعداد برچسب را بیشتر از صفر وارد کنید.');
            return;
        }

        setPreparing(true);
        try {
            const preparedLines: BarcodeLabelLine[] = [];

            for (const variant of selected) {
                const requestedQuantity = Math.max(
                    1,
                    Math.min(500, Math.floor(quantities[variant.id] ?? 1)),
                );

                const barcode = variant.barcode?.trim()
                    || (await productsApi.ensureVariantBarcode(product.id, variant.id)).barcode;

                if (!barcode) {
                    throw new Error(`بارکد تنوع ${variant.sku} دریافت نشد.`);
                }

                const variantBasePrice = variant.priceOverride ?? product.price;
                // اعمال نسبت قیمت نهایی محصول (مثلاً تخفیف فعال) روی قیمت این تنوع.
                const unitPrice = product.price > 0
                    ? variantBasePrice * (product.finalPrice / product.price)
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
                    const index = next.findIndex((line) => line.variantId === incoming.variantId);
                    if (index >= 0) {
                        next[index] = {
                            ...next[index],
                            barcode: incoming.barcode,
                            quantity: Math.min(500, next[index].quantity + incoming.quantity),
                        };
                    } else {
                        next.push(incoming);
                    }
                }
                return next;
            });

            toast.success(`${preparedLines.length} تنوع به فهرست چاپ اضافه شد.`);
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'آماده‌سازی برچسب ناموفق بود.');
        } finally {
            setPreparing(false);
        }
    };

    const updateLabelQuantity = (variantId: string, rawQuantity: string) => {
        const parsed = Number(rawQuantity);
        const quantity = Number.isFinite(parsed)
            ? Math.max(1, Math.min(500, Math.floor(parsed)))
            : 1;
        setLines((current) => current.map((line) =>
            line.variantId === variantId ? { ...line, quantity } : line,
        ));
    };

    const removeLine = (variantId: string) => {
        setLines((current) => current.filter((line) => line.variantId !== variantId));
    };

    const printLabels = () => {
        if (expandedLabels.length === 0) {
            toast.error('فهرست برچسب‌ها خالی است.');
            return;
        }
        window.print();
    };

    if (!canManageProducts) {
        return (
            <Card dir="rtl">
                <CardContent className="p-6 text-sm">
                    برای چاپ و ایجاد بارکد، مجوز <code>products.update</code> لازم است.
                </CardContent>
            </Card>
        );
    }

    return (
        <div dir="rtl" className="space-y-6">
            <style>{`
                .barcode-print-surface { display: none; }
                @page { size: A4; margin: 6mm; }
                @page thermal-label { size: 50mm 30mm; margin: 1mm; }
                @media print {
                    body * { visibility: hidden !important; }
                    .barcode-print-surface,
                    .barcode-print-surface * { visibility: visible !important; }
                    .barcode-print-surface {
                        display: block !important;
                        position: absolute;
                        inset: 0;
                        width: 100%;
                        background: #fff !important;
                        color: #000 !important;
                    }
                    .barcode-print-grid {
                        display: grid;
                        grid-template-columns: repeat(3, minmax(0, 1fr));
                        gap: 2mm;
                    }
                    .barcode-label {
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
                    .barcode-svg { height: 8mm !important; }
                    .barcode-print-surface.mode-thermal .barcode-print-grid { display: block; }
                    .barcode-print-surface.mode-thermal .barcode-label {
                        page: thermal-label;
                        width: 48mm;
                        height: 28mm;
                        padding: 1mm;
                        border: none;
                        break-after: page;
                    }
                    .barcode-print-surface.mode-thermal .barcode-label:last-child { break-after: auto; }
                    .barcode-print-surface.mode-thermal .barcode-svg { height: 10mm !important; }
                }
            `}</style>

            <div className="barcode-screen-only flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="flex items-center gap-2 text-2xl font-bold">
                        <Barcode className="size-6" /> چاپ برچسب بارکد
                    </h1>
                    <p className="mt-1 text-sm text-muted-foreground">
                        تنوع لباس را انتخاب کنید، تعداد برچسب را تعیین کنید و برچسب قابل اسکن برای صندوق چاپ کنید.
                    </p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                    <label className="text-sm" htmlFor="barcode-format">قالب چاپ</label>
                    <select
                        id="barcode-format"
                        className="h-9 rounded-md border bg-background px-3 text-sm"
                        value={format}
                        onChange={(event) => setFormat(event.target.value as PrintFormat)}
                    >
                        <option value="a4">برگه A4، سه ستون</option>
                        <option value="thermal">لیبل حرارتی ۵۰×۳۰ میلی‌متر</option>
                    </select>
                    <Button onClick={printLabels} disabled={expandedLabels.length === 0}>
                        <Printer className="ms-2 size-4" /> چاپ {totalLabels} برچسب
                    </Button>
                </div>
            </div>

            <div className="barcode-screen-only grid gap-6 xl:grid-cols-[minmax(280px,0.8fr)_minmax(0,1.2fr)]">
                <Card>
                    <CardHeader>
                        <CardTitle>۱. انتخاب محصول</CardTitle>
                        <CardDescription>با نام کالا جست‌وجو کنید؛ سپس تنوع و تعداد برچسب را انتخاب کنید.</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex gap-2">
                            <Input
                                value={search}
                                onChange={(event) => changeSearch(event.target.value)}
                                onKeyDown={(event) => {
                                    if (event.key === 'Enter') event.preventDefault();
                                }}
                                placeholder="جست‌وجوی نام محصول یا SKU..."
                            />
                            <Button type="button" variant="outline" aria-label="جست‌وجوی محصولات" disabled={productsQuery.isFetching} onClick={() => void productsQuery.refetch()}>
                                <Search className="size-4" />
                            </Button>
                        </div>

                        {productsQuery.isPending ? (
                            <p className="text-sm text-muted-foreground">در حال بارگذاری محصولات...</p>
                        ) : productsQuery.isError ? (
                            <div className="space-y-2 text-sm">
                                <p>بارگذاری محصولات ناموفق بود.</p>
                                <Button variant="outline" onClick={() => void productsQuery.refetch()}>تلاش دوباره</Button>
                            </div>
                        ) : (
                            <>
                                <label htmlFor="barcode-product-select" className="text-sm font-medium">محصول</label>
                                <select
                                    id="barcode-product-select"
                                    className="min-h-10 w-full rounded-md border bg-background px-3 py-2 text-sm"
                                    value={selectedProductId}
                                    onChange={(event) => {
                                        setSelectedProductId(event.target.value);
                                        setQuantities({});
                                    }}
                                >
                                    <option value="">انتخاب محصول...</option>
                                    {productsQuery.data.items.map((item) => (
                                        <option key={item.id} value={item.id}>
                                            {item.name} {item.sku ? `(${item.sku})` : ''}
                                        </option>
                                    ))}
                                </select>

                                {productsQuery.data.items.length === 0 && (
                                    <p className="text-sm text-muted-foreground">محصولی یافت نشد.</p>
                                )}

                                <div className="flex items-center justify-between border-t pt-3 text-sm">
                                    <span>صفحه {productsQuery.data.page} از {productsQuery.data.totalPages || 1}</span>
                                    <div className="flex gap-2">
                                        <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>قبلی</Button>
                                        <Button variant="outline" size="sm" disabled={page >= productsQuery.data.totalPages} onClick={() => setPage((value) => value + 1)}>بعدی</Button>
                                    </div>
                                </div>
                            </>
                        )}
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>۲. انتخاب تنوع و تعداد</CardTitle>
                        <CardDescription>برای هر سایز/رنگ تعداد برچسب مورد نیاز را وارد کنید. بارکدهای خالی هنگام افزودن به فهرست ایجاد و ذخیره می‌شوند.</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        {!selectedProductId ? (
                            <p className="text-sm text-muted-foreground">ابتدا یک محصول انتخاب کنید.</p>
                        ) : productQuery.isPending ? (
                            <p className="text-sm text-muted-foreground">در حال بارگذاری تنوع‌ها...</p>
                        ) : productQuery.isError || !product ? (
                            <div className="space-y-2 text-sm">
                                <p>جزئیات محصول بارگذاری نشد.</p>
                                <Button variant="outline" onClick={() => void productQuery.refetch()}>تلاش دوباره</Button>
                            </div>
                        ) : variants.length === 0 ? (
                            <p className="text-sm text-muted-foreground">برای این محصول تنوع قابل چاپی وجود ندارد.</p>
                        ) : (
                            <>
                                <div className="font-semibold">{product.name}</div>
                                <div className="overflow-x-auto rounded-md border">
                                    <table className="w-full min-w-[520px] text-sm">
                                        <thead className="bg-muted/50">
                                            <tr>
                                                <th className="p-2 text-right">تنوع</th>
                                                <th className="p-2 text-right">SKU</th>
                                                <th className="p-2 text-right">وضعیت بارکد</th>
                                                <th className="w-24 p-2 text-right">تعداد چاپ</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {variants.map((variant) => (
                                                <tr key={variant.id} className="border-t">
                                                    <td className="p-2">
                                                        <div>{variantDescription(variant)}</div>
                                                        {!variant.isActive && <div className="mt-1 text-xs text-destructive">تنوع غیرفعال</div>}
                                                    </td>
                                                    <td className="p-2 font-mono text-xs">{variant.sku}</td>
                                                    <td className="p-2">
                                                        {variant.barcode ? (
                                                            <span className="text-xs text-green-700">{variant.barcode}</span>
                                                        ) : (
                                                            <span className="text-xs text-amber-700">هنگام افزودن ساخته می‌شود</span>
                                                        )}
                                                    </td>
                                                    <td className="p-2">
                                                        <Input
                                                            type="number"
                                                            min={0}
                                                            max={500}
                                                            step={1}
                                                            value={quantities[variant.id] ?? 0}
                                                            disabled={!variant.isActive}
                                                            onChange={(event) => {
                                                                const parsed = Number(event.target.value);
                                                                setQuantities((current) => ({
                                                                    ...current,
                                                                    [variant.id]: Number.isFinite(parsed)
                                                                        ? Math.max(0, Math.min(500, Math.floor(parsed)))
                                                                        : 0,
                                                                }));
                                                            }}
                                                        />
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                                <Button onClick={() => void addSelectedVariants()} disabled={preparing || !variants.some((variant) => variant.isActive && (quantities[variant.id] ?? 0) > 0)}>
                                    <Plus className="ms-2 size-4" />
                                    {preparing ? 'در حال آماده‌سازی...' : 'افزودن تنوع‌های انتخاب‌شده به چاپ'}
                                </Button>
                            </>
                        )}
                    </CardContent>
                </Card>
            </div>

            <div className="barcode-screen-only space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                        <h2 className="text-lg font-semibold">۳. فهرست برچسب‌ها</h2>
                        <p className="text-sm text-muted-foreground">تعداد کل قابل چاپ: {totalLabels}</p>
                    </div>
                    <Button variant="outline" onClick={() => setLines([])} disabled={lines.length === 0}>خالی‌کردن فهرست</Button>
                </div>

                {lines.length === 0 ? (
                    <Card><CardContent className="p-6 text-sm text-muted-foreground">هنوز برچسبی به فهرست اضافه نشده است.</CardContent></Card>
                ) : (
                    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                        {lines.map((line) => (
                            <Card key={line.variantId}>
                                <CardContent className="space-y-3 p-4">
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0">
                                            <div className="truncate font-semibold">{line.productName}</div>
                                            <div className="mt-1 text-xs text-muted-foreground">{line.variantText}</div>
                                            <div className="mt-1 font-mono text-xs">{line.barcode}</div>
                                        </div>
                                        <Button variant="ghost" size="icon" aria-label="حذف از فهرست" onClick={() => removeLine(line.variantId)}>
                                            <Trash2 className="size-4" />
                                        </Button>
                                    </div>
                                    <div className="rounded border bg-white p-3 text-black">
                                        <LabelContent line={line} locale={locale} />
                                    </div>
                                    <div className="flex items-center justify-between gap-3">
                                        <label className="text-sm" htmlFor={`copies-${line.variantId}`}>تعداد نسخه</label>
                                        <Input
                                            id={`copies-${line.variantId}`}
                                            className="w-24"
                                            type="number"
                                            min={1}
                                            max={500}
                                            value={line.quantity}
                                            onChange={(event) => updateLabelQuantity(line.variantId, event.target.value)}
                                        />
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>
                )}
            </div>

            <div className={`barcode-print-surface ${format === 'thermal' ? 'mode-thermal' : 'mode-a4'}`} dir="rtl">
                <div className="barcode-print-grid">
                    {expandedLabels.map((line) => (
                        <div className="barcode-label" key={line.printKey}>
                            <LabelContent line={line} locale={locale} />
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}