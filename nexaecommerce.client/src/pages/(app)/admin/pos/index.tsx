import { useMemo, useRef, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Barcode, Banknote, CreditCard, Minus, Plus, Printer, RefreshCw, Search, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { formatMoney } from '@/lib/formatMoney';
import { resolveMediaUrl } from '@/lib/media-url';
import { ApiError } from '@/lib/problem';
import { posApi, type PosProduct, type PosSale, type PosVariant, type PosCreateSaleRequest } from '@/lib/api/pos';

const canSellPermission = 'pos.sell';

type CartLine = PosVariant & { quantity: number };

function errorMessage(error: unknown): string {
    if (error instanceof ApiError) return error.message || 'درخواست انجام نشد.';
    if (error instanceof Error) return error.message;
    return 'خطای پیش‌بینی‌نشده رخ داد.';
}

function paymentLabel(method: string): string {
    if (method === 'Cash') return 'نقدی';
    if (method === 'Card' || method === 'BankMellat') return 'کارتخوان ملت';
    return method;
}

export default function PosPage() {
    useDocumentTitle('صندوق فروش حضوری');
    const canSell = usePermission(canSellPermission);
    const canRead = usePermission('pos.read');
    const { i18n } = useTranslation();
    const locale = i18n.resolvedLanguage ?? i18n.language ?? 'fa-IR';
    const searchRef = useRef<HTMLInputElement>(null);

    const [searchText, setSearchText] = useState('');
    const [results, setResults] = useState<PosProduct[]>([]);
    const [cart, setCart] = useState<CartLine[]>([]);
    const [paymentMethod, setPaymentMethod] = useState<'Cash' | 'Card'>('Cash');
    const [paymentReference, setPaymentReference] = useState('');
    const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
    const [lastSale, setLastSale] = useState<PosSale | null>(null);
    const [printing, setPrinting] = useState(false);

    const salesQuery = useQuery({
        queryKey: ['pos', 'sales', 0, 10],
        queryFn: () => posApi.listSales(0, 10),
        enabled: canRead,
        staleTime: 15_000,
    });

    const saleMutation = useMutation({
        mutationFn: (body: PosCreateSaleRequest) => posApi.createSale(body),
        onSuccess: (sale) => {
            setLastSale(sale);
            setCart([]);
            setPaymentReference('');
            setSearchText('');
            setResults([]);
            setIdempotencyKey(crypto.randomUUID());
            toast.success(`فاکتور ${sale.saleNumber} ثبت شد.`);
            if (canRead) void salesQuery.refetch();
            searchRef.current?.focus();
        },
        onError: (error) => toast.error(errorMessage(error)),
    });

    const subtotal = useMemo(
        () => cart.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0),
        [cart],
    );
    const itemCount = useMemo(() => cart.reduce((sum, line) => sum + line.quantity, 0), [cart]);
    const currency = cart[0]?.currency ?? lastSale?.currency ?? 'IRR';

    const addVariant = (variant: PosVariant) => {
        if (variant.availableQuantity < 1) {
            toast.error('این تنوع موجودی قابل فروش ندارد.');
            return;
        }
        setCart((current) => {
            const existing = current.find((line) => line.id === variant.id);
            if (existing) {
                if (existing.quantity >= variant.availableQuantity) {
                    toast.error(`حداکثر موجودی قابل فروش ${variant.availableQuantity} عدد است.`);
                    return current;
                }
                return current.map((line) => line.id === variant.id
                    ? { ...line, quantity: line.quantity + 1 }
                    : line);
            }
            return [...current, { ...variant, quantity: 1 }];
        });
    };

    const runSearch = async () => {
        const term = searchText.trim();
        if (term.length < 2) {
            toast.error('حداقل دو نویسه یا یک بارکد وارد کنید.');
            return;
        }

        try {
            // اول اسکن دقیق بارکد را امتحان می‌کنیم؛ اگر بارکد یافت نشد، جست‌وجوی متنی انجام می‌شود.
            try {
                const variant = await posApi.findByBarcode(term);
                addVariant(variant);
                setResults([]);
                setSearchText('');
                return;
            } catch (error) {
                if (error instanceof ApiError && error.status === 409) {
                    toast.error(errorMessage(error));
                    return;
                }
                if (!(error instanceof ApiError && error.status === 404)) throw error;
            }

            const products = await posApi.searchProducts(term);
            setResults(products);
            if (products.length === 0) toast.message('کالایی پیدا نشد.');
        } catch (error) {
            toast.error(errorMessage(error));
        }
    };

    const changeQuantity = (variantId: string, next: number) => {
        setCart((current) => current
            .map((line) => line.id === variantId
                ? { ...line, quantity: Math.max(0, Math.min(next, line.availableQuantity)) }
                : line)
            .filter((line) => line.quantity > 0));
    };

    const submitSale = () => {
        if (cart.length === 0) {
            toast.error('سبد فروش خالی است.');
            return;
        }
        if (paymentMethod === 'Card' && !paymentReference.trim()) {
            toast.error('شماره پیگیری رسید کارتخوان را وارد کنید.');
            return;
        }

        saleMutation.mutate({
            idempotencyKey,
            paymentMethod,
            paymentReference: paymentMethod === 'Card' ? paymentReference.trim() : null,
            items: cart.map((line) => ({ productVariantId: line.id, quantity: line.quantity })),
        });
    };

    const reprintSale = async (sale: PosSale) => {
        try {
            const full = await posApi.getSale(sale.id);
            setLastSale(full);
            window.setTimeout(() => window.print(), 50);
        } catch (error) {
            toast.error(errorMessage(error));
        }
    };

    if (!canSell) {
        return <Card><CardContent className="p-6 text-sm">برای استفاده از صندوق، مجوز <code>pos.sell</code> لازم است.</CardContent></Card>;
    }

    return (
        <div className="space-y-6" dir="rtl">
            <style>{`
        .pos-receipt-print { display: none; }
        @media print {
          body * { visibility: hidden !important; }
          .pos-receipt-print, .pos-receipt-print * { visibility: visible !important; }
          .pos-receipt-print { display: block !important; position: absolute; inset: 0; width: 100%; padding: 18px; background: white; color: black; }
          @page { size: A4; margin: 10mm; }
        }
      `}</style>

            <div className="pos-hide-print flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold">صندوق فروش حضوری</h1>
                    <p className="mt-1 text-sm text-muted-foreground">اسکن بارکد، انتخاب تنوع، ثبت فاکتور و کسر موجودی مشترک فروشگاه</p>
                </div>
                {canRead && <Button variant="outline" onClick={() => { void salesQuery.refetch(); }} disabled={salesQuery.isFetching}>
                    <RefreshCw className={`ms-2 size-4 ${salesQuery.isFetching ? 'animate-spin' : ''}`} /> تازه‌سازی فاکتورها
                </Button>}
            </div>

            <div className="pos-hide-print grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(360px,0.85fr)]">
                <div className="space-y-6">
                    <Card>
                        <CardHeader><CardTitle className="flex items-center gap-2"><Barcode className="size-5" /> جست‌وجوی کالا / اسکن بارکد</CardTitle></CardHeader>
                        <CardContent>
                            <form className="flex gap-2" onSubmit={(event) => { event.preventDefault(); void runSearch(); }}>
                                <Input
                                    ref={searchRef}
                                    value={searchText}
                                    onChange={(event) => setSearchText(event.target.value)}
                                    placeholder="بارکد، نام کالا یا SKU را اسکن/وارد کنید…"
                                    autoComplete="off"
                                    autoFocus
                                />
                                <Button type="submit"><Search className="ms-2 size-4" /> جست‌وجو</Button>
                            </form>
                            <p className="mt-2 text-xs text-muted-foreground">پس از اسکن، معمولاً اسکنر خودش Enter می‌فرستد. اگر کالا چند تنوع داشته باشد، تنوع درست را انتخاب کنید.</p>
                        </CardContent>
                    </Card>

                    {results.length > 0 && (
                        <Card>
                            <CardHeader><CardTitle>نتایج جست‌وجو</CardTitle></CardHeader>
                            <CardContent className="space-y-4">
                                {results.map((product) => (
                                    <div key={product.id} className="rounded-lg border p-3">
                                        <div className="mb-3 flex items-center gap-3">
                                            {product.imageUrl && <img src={resolveMediaUrl(product.imageUrl)} alt={product.name} className="size-14 rounded-md object-cover" />}
                                            <div><div className="font-semibold">{product.name}</div><div className="text-xs text-muted-foreground">{product.variants.length} تنوع قابل انتخاب</div></div>
                                        </div>
                                        <div className="flex flex-wrap gap-2">
                                            {product.variants.map((variant) => (
                                                <Button key={variant.id} variant="outline" size="sm" disabled={variant.availableQuantity < 1} onClick={() => addVariant(variant)}>
                                                    <Plus className="ms-1 size-3.5" />
                                                    {variant.attributeLabels.length ? variant.attributeLabels.join(' / ') : variant.sku}
                                                    {' — '}{formatMoney(variant.unitPrice, variant.currency, locale)}
                                                    {' — '}موجودی: {variant.availableQuantity}
                                                </Button>
                                            ))}
                                        </div>
                                    </div>
                                ))}
                            </CardContent>
                        </Card>
                    )}

                    {canRead && <Card>
                        <CardHeader><CardTitle>فاکتورهای اخیر</CardTitle></CardHeader>
                        <CardContent>
                            {salesQuery.isLoading ? <p className="text-sm text-muted-foreground">در حال بارگذاری…</p>
                                : salesQuery.isError ? <div className="space-y-2 text-sm"><p>دریافت فاکتورهای اخیر ناموفق بود.</p><Button variant="outline" onClick={() => { void salesQuery.refetch(); }}>تلاش دوباره</Button></div>
                                    : (salesQuery.data ?? []).length === 0 ? <p className="text-sm text-muted-foreground">هنوز فاکتوری ثبت نشده است.</p>
                                        : <div className="space-y-2">
                                            {(salesQuery.data ?? []).map((sale) => (
                                                <div key={sale.id} className="flex flex-wrap items-center justify-between gap-3 rounded-md border p-3">
                                                    <div><div className="font-medium">{sale.saleNumber}</div><div className="text-xs text-muted-foreground">{new Date(sale.createdAt).toLocaleString(locale)} · {paymentLabel(sale.paymentMethod)}</div></div>
                                                    <div className="flex items-center gap-3"><span className="text-sm font-semibold">{formatMoney(sale.totalAmount, sale.currency, locale)}</span><Button size="sm" variant="outline" onClick={() => { void reprintSale(sale); }}><Printer className="ms-1 size-4" /> چاپ</Button></div>
                                                </div>
                                            ))}
                                        </div>}
                        </CardContent>
                    </Card>}
                </div>

                <Card className="h-fit">
                    <CardHeader>
                        <CardTitle className="flex items-center justify-between gap-3"><span>سبد فروش</span><Badge variant="secondary">{itemCount} عدد</Badge></CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        {cart.length === 0 ? <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">سبد خالی است. محصولی را اسکن کنید یا از نتایج جست‌وجو انتخاب کنید.</div>
                            : <div className="space-y-3">
                                {cart.map((line) => (
                                    <div key={line.id} className="rounded-lg border p-3">
                                        <div className="flex items-start justify-between gap-3">
                                            <div className="min-w-0"><div className="font-medium">{line.productName}</div><div className="mt-1 text-xs text-muted-foreground">{line.attributeLabels.join(' · ') || line.sku}</div><div className="text-xs text-muted-foreground">SKU: {line.sku}{line.barcode ? ` · بارکد: ${line.barcode}` : ''}</div></div>
                                            <Button size="icon" variant="ghost" aria-label="حذف کالا" onClick={() => changeQuantity(line.id, 0)}><Trash2 className="size-4 text-destructive" /></Button>
                                        </div>
                                        <div className="mt-3 flex items-center justify-between gap-3">
                                            <div className="flex items-center gap-2">
                                                <Button size="icon" variant="outline" aria-label="کاهش تعداد" disabled={line.quantity <= 1} onClick={() => changeQuantity(line.id, line.quantity - 1)}><Minus className="size-3.5" /></Button>
                                                <span className="min-w-7 text-center font-semibold">{line.quantity}</span>
                                                <Button size="icon" variant="outline" aria-label="افزایش تعداد" disabled={line.quantity >= line.availableQuantity} onClick={() => changeQuantity(line.id, line.quantity + 1)}><Plus className="size-3.5" /></Button>
                                            </div>
                                            <div className="text-left"><div className="font-semibold">{formatMoney(line.unitPrice * line.quantity, line.currency, locale)}</div><div className="text-xs text-muted-foreground">هر عدد {formatMoney(line.unitPrice, line.currency, locale)}</div></div>
                                        </div>
                                    </div>
                                ))}
                            </div>}

                        <div className="border-t pt-4">
                            <div className="flex items-center justify-between text-sm"><span>تعداد اقلام</span><span>{itemCount}</span></div>
                            <div className="mt-2 flex items-center justify-between text-lg font-bold"><span>مبلغ قابل پرداخت</span><span>{formatMoney(subtotal, currency, locale)}</span></div>
                        </div>

                        <div className="space-y-3 rounded-lg bg-muted/40 p-3">
                            <div className="text-sm font-semibold">روش پرداخت</div>
                            <div className="grid grid-cols-2 gap-2">
                                <Button type="button" variant={paymentMethod === 'Cash' ? 'default' : 'outline'} onClick={() => setPaymentMethod('Cash')}><Banknote className="ms-2 size-4" /> نقدی</Button>
                                <Button type="button" variant={paymentMethod === 'Card' ? 'default' : 'outline'} onClick={() => setPaymentMethod('Card')}><CreditCard className="ms-2 size-4" /> کارتخوان ملت</Button>
                            </div>
                            {paymentMethod === 'Card' && <div className="space-y-2"><label className="text-sm" htmlFor="pos-payment-reference">شماره پیگیری رسید کارتخوان</label><Input id="pos-payment-reference" value={paymentReference} onChange={(event) => setPaymentReference(event.target.value)} maxLength={256} placeholder="شماره پیگیری روی رسید" /><p className="text-xs text-amber-700">این نسخه مستقیماً به دستگاه کارتخوان وصل نیست. فقط پس از تأیید موفق پرداخت روی دستگاه، شماره رسید را ثبت کنید.</p></div>}
                        </div>

                        <div className="flex gap-2">
                            <Button className="flex-1" onClick={submitSale} disabled={cart.length === 0 || saleMutation.isPending || (paymentMethod === 'Card' && !paymentReference.trim())}>
                                {saleMutation.isPending ? 'در حال ثبت…' : 'ثبت فروش و صدور فاکتور'}
                            </Button>
                            <Button variant="outline" onClick={() => setCart([])} disabled={cart.length === 0 || saleMutation.isPending}>پاک‌کردن</Button>
                        </div>
                        <p className="text-xs text-muted-foreground">قیمت و موجودی نهایی در سرور دوباره بررسی می‌شود. اگر فروش آنلاین هم‌زمان موجودی را تغییر دهد، ثبت فاکتور با خطا متوقف می‌شود.</p>
                    </CardContent>
                </Card>
            </div>

            {lastSale && (
                <Card className="pos-receipt-print">
                    <CardHeader><CardTitle className="text-center">فاکتور فروش حضوری</CardTitle></CardHeader>
                    <CardContent>
                        <div className="mb-4 flex flex-wrap items-start justify-between gap-3 border-b pb-4">
                            <div><div className="text-lg font-bold">{lastSale.saleNumber}</div><div className="text-sm">تاریخ: {new Date(lastSale.createdAt).toLocaleString(locale)}</div></div>
                            <div className="text-sm">روش پرداخت: {paymentLabel(lastSale.paymentMethod)}{lastSale.paymentReference ? ` · پیگیری: ${lastSale.paymentReference}` : ''}</div>
                        </div>
                        <table className="w-full border-collapse text-sm">
                            <thead><tr className="border-b text-right"><th className="p-2">کالا</th><th className="p-2">تعداد</th><th className="p-2">قیمت واحد</th><th className="p-2">جمع</th></tr></thead>
                            <tbody>{lastSale.items.map((item, i) => <tr key={`${item.productVariantId}-${i}`} className="border-b"><td className="p-2">{item.productName}<div className="text-xs">{item.attributeLabels.join(' · ')} — {item.sku}</div></td><td className="p-2">{item.quantity}</td><td className="p-2">{formatMoney(item.unitPrice, lastSale.currency, locale)}</td><td className="p-2">{formatMoney(item.lineTotal, lastSale.currency, locale)}</td></tr>)}</tbody>
                        </table>
                        <div className="mt-4 flex justify-end"><div className="text-lg font-bold">مبلغ کل: {formatMoney(lastSale.totalAmount, lastSale.currency, locale)}</div></div>
                        <div className="mt-6 text-center text-xs text-muted-foreground">از خرید شما سپاسگزاریم</div>
                    </CardContent>
                </Card>
            )}

            {lastSale && <div className="pos-hide-print flex justify-end"><Button variant="outline" onClick={() => { setPrinting(true); window.print(); window.setTimeout(() => setPrinting(false), 700); }} disabled={printing}><Printer className="ms-2 size-4" /> چاپ فاکتور {lastSale.saleNumber}</Button></div>}
        </div>
    );
}