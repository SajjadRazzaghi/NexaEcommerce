
import { useEffect, useMemo, useRef, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
    Barcode,
    Banknote,
    CreditCard,
    Minus,
    Plus,
    Printer,
    RefreshCw,
    Search,
    Trash2,
} from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { appearanceApi } from '@/lib/api/appearance';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { formatMoney } from '@/lib/formatMoney';
import { resolveMediaUrl } from '@/lib/media-url';
import { ApiError } from '@/lib/problem';
import {
    posApi,
    type PosProduct,
    type PosSale,
    type PosVariant,
    type PosCreateSaleRequest,
} from '@/lib/api/pos';
import { QRCodeSVG } from 'qrcode.react';

const canSellPermission = 'pos.sell';

type CartLine = PosVariant & { quantity: number };

function errorMessage(error: unknown): string {
    if (error instanceof ApiError) return error.message || 'درخواست انجام نشد.';
    if (error instanceof Error) return error.message;
    return 'خطای پیش‌بینی‌نشده رخ داد.';
}
function isCompleteBarcode(value: string): boolean {
// بارکدهای عددی متداول و بارکدهای عددی داخلی
    return /^\d{8,20}$/.test(value.trim());
}
function paymentLabel(method: string): string {
    if (method === 'Cash') return 'نقدی';
    if (method === 'Card' || method === 'BankMellat') return 'کارتخوان ملت';
    return method;
}

function isBankPayment(sale: PosSale): boolean {
    return sale.paymentMethod === 'Card' || sale.paymentMethod === 'BankMellat';
}

function isWaitingForBank(status: string): boolean {
    return (
        status === 'PendingPayment' ||
        status === 'PaymentInProgress' ||
        status === 'ReconciliationRequired'
    );
}

export default function PosPage() {
    useDocumentTitle('صندوق فروش حضوری');

    const canSell = usePermission(canSellPermission);
    const canRead = usePermission('pos.read');

    const { i18n } = useTranslation();
    const locale = i18n.resolvedLanguage ?? i18n.language ?? 'fa-IR';

    const searchRef = useRef<HTMLInputElement>(null);

    const [pendingCardSaleId, setPendingCardSaleId] = useState<string | null>(null);
    const [searchText, setSearchText] = useState('');
    const [results, setResults] = useState<PosProduct[]>([]);
    const [cart, setCart] = useState<CartLine[]>([]);
    const [paymentMethod, setPaymentMethod] = useState<'Cash' | 'Card'>('Cash');
    const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
    const [lastSale, setLastSale] = useState<PosSale | null>(null);
    const [printing, setPrinting] = useState(false);

    // برای جلوگیری از نمایش چندباره پیام یک وضعیت بانکی
    const notifiedPaymentStatusesRef = useRef<Set<string>>(new Set());

    const paymentLocked = Boolean(pendingCardSaleId);

    const salesQuery = useQuery({
        queryKey: ['pos', 'sales', 0, 10],
        queryFn: () => posApi.listSales(0, 10),
        enabled: canRead,
        staleTime: 15_000,
    });

    // اگر کاربر صفحه را ببندد یا دوباره باز کند، پرداخت باز قبلی بازیابی می‌شود.
    const openPaymentQuery = useQuery({
        queryKey: ['pos', 'open-payment'],
        queryFn: () => posApi.getOpenPaymentStatus(),
        enabled: canSell,
        staleTime: 0,
        refetchOnWindowFocus: true,
        retry: 1,
    });

    useEffect(() => {
        const openSaleId = openPaymentQuery.data?.saleId;

        if (openSaleId) {
            setPendingCardSaleId(openSaleId);
        }
    }, [openPaymentQuery.data?.saleId]);

    const pendingSaleQuery = useQuery({
        queryKey: ['pos', 'payment-sale', pendingCardSaleId],
        queryFn: () => posApi.getSale(pendingCardSaleId!),
        enabled: Boolean(pendingCardSaleId),
        staleTime: 0,
        retry: 1,
    });

    useEffect(() => {
        if (pendingSaleQuery.data) {
            setLastSale(pendingSaleQuery.data);
        }
    }, [pendingSaleQuery.data]);

    // دریافت وضعیت پرداخت از سرور؛ ارسال پرداخت فقط توسط Bridge انجام می‌شود.
    const paymentStatusQuery = useQuery({
        queryKey: ['pos', 'payment-status', pendingCardSaleId],
        queryFn: () => posApi.getPaymentStatus(pendingCardSaleId!),
        enabled: Boolean(pendingCardSaleId),
        staleTime: 0,
        refetchInterval: (query) => {
            const status = query.state.data?.status;

            if (status === 'ReconciliationRequired') return 10_000;

            if (
                status === 'PendingPayment' ||
                status === 'PaymentInProgress' ||
                !status
            ) {
                return 1_500;
            }

            return false;
        },
    });

    useEffect(() => {
        const status = paymentStatusQuery.data;

        if (!status) return;

        setLastSale((current) =>
            current?.id === status.saleId
                ? {
                    ...current,
                    status: status.status,
                    paymentReference:
                        status.paymentReference ?? current.paymentReference,
                }
                : current,
        );

        const notificationKey = `${status.saleId}:${status.status}`;

        if (status.status === 'Paid') {
            if (notifiedPaymentStatusesRef.current.has(notificationKey)) return;

            notifiedPaymentStatusesRef.current.add(notificationKey);

            setPendingCardSaleId(null);
            setCart([]);
            setSearchText('');
            setResults([]);
            setIdempotencyKey(crypto.randomUUID());

            toast.success('پرداخت کارتخوان تأیید شد و فاکتور نهایی گردید.');

            void openPaymentQuery.refetch();

            if (canRead) {
                void salesQuery.refetch();
            }

            searchRef.current?.focus();
            return;
        }

        if (status.status === 'Cancelled') {
            if (notifiedPaymentStatusesRef.current.has(notificationKey)) return;

            notifiedPaymentStatusesRef.current.add(notificationKey);

            setPendingCardSaleId(null);
            setIdempotencyKey(crypto.randomUUID());

            toast.error(
                'پرداخت کارتخوان تأیید نشد؛ موجودی آزاد شد و سبد فروش حفظ شد.',
            );

            void openPaymentQuery.refetch();
            return;
        }

        if (status.status === 'ReconciliationRequired') {
            if (notifiedPaymentStatusesRef.current.has(notificationKey)) return;

            notifiedPaymentStatusesRef.current.add(notificationKey);

            toast.error(
                'نتیجه بانکی نامشخص است؛ پرداخت مجدد نزنید و از مدیر بخواهید تراکنش را تطبیق دهد.',
            );
        }
    }, [
        paymentStatusQuery.data,
        canRead,
        salesQuery,
        openPaymentQuery,
    ]);

    const appearanceQuery = useQuery({
        queryKey: ['appearance'],
        queryFn: appearanceApi.get,
        staleTime: 5 * 60_000,
    });

    const appearance = appearanceQuery.data;
    const configuredWebsiteUrl = appearance?.websiteUrl?.trim();

    const websiteQrUrl = configuredWebsiteUrl
        ? (/^https?:\/\//i.test(configuredWebsiteUrl)
            ? configuredWebsiteUrl
            : `https://${configuredWebsiteUrl}`)
        : 'https://patletdress.ir';

    const websiteAddress = websiteQrUrl
        .replace(/^https?:\/\//i, '')
        .replace(/\/+$/, '');

    const saleMutation = useMutation({
        mutationFn: (body: PosCreateSaleRequest) => posApi.createSale(body),

        onSuccess: (sale) => {
            setLastSale(sale);

            // پرداخت کارتی باید از طریق Bridge تکمیل شود.
            if (isBankPayment(sale) && isWaitingForBank(sale.status)) {
                setPendingCardSaleId(sale.id);

                toast.message(
                    sale.status === 'ReconciliationRequired'
                        ? `فاکتور ${sale.saleNumber} نیازمند تطبیق بانکی است؛ پرداخت مجدد انجام ندهید.`
                        : `مبلغ فاکتور ${sale.saleNumber} برای ارسال به کارتخوان ثبت شد.`,
                );

                return;
            }

            // فروش نقدی بلافاصله توسط سرور نهایی می‌شود.
            setCart([]);
            setSearchText('');
            setResults([]);
            setIdempotencyKey(crypto.randomUUID());

            toast.success(`فاکتور ${sale.saleNumber} ثبت شد.`);

            if (canRead) {
                void salesQuery.refetch();
            }

            searchRef.current?.focus();
        },

        onError: (error) => {
            toast.error(errorMessage(error));
        },
    });

    const subtotal = useMemo(
        () => cart.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0),
        [cart],
    );

    const itemCount = useMemo(
        () => cart.reduce((sum, line) => sum + line.quantity, 0),
        [cart],
    );

    const currency = cart[0]?.currency ?? lastSale?.currency ?? 'IRR';

    const addVariant = (variant: PosVariant) => {
        if (paymentLocked) {
            toast.error('تا تعیین تکلیف پرداخت قبلی امکان تغییر سبد وجود ندارد.');
            return;
        }

        if (variant.availableQuantity < 1) {
            toast.error('این تنوع موجودی قابل فروش ندارد.');
            return;
        }

        setCart((current) => {
            const existing = current.find((line) => line.id === variant.id);

            if (existing) {
                if (existing.quantity >= variant.availableQuantity) {
                    toast.error(
                        `حداکثر موجودی قابل فروش ${variant.availableQuantity} عدد است.`,
                    );
                    return current;
                }

                return current.map((line) =>
                    line.id === variant.id
                        ? { ...line, quantity: line.quantity + 1 }
                        : line,
                );
            }

            return [...current, { ...variant, quantity: 1 }];
        });
    };

    // انتخاب از نتایج جست‌وجو؛ پس از افزودن کالا، عبارت جست‌وجو پاک می‌شود.
    const selectVariant = (variant: PosVariant) => {
        addVariant(variant);
        setSearchText('');
        setResults([]);
        searchRef.current?.focus();
    };
    const runSearch = async () => {
        if (paymentLocked) {
            toast.error('تا تعیین تکلیف پرداخت قبلی امکان جست‌وجوی فروش جدید وجود ندارد.');
            return;
        }

        const term = searchText.trim();

        if (term.length < 2) {
            toast.error('حداقل دو نویسه یا یک بارکد وارد کنید.');
            return;
        }

        const completeBarcode = isCompleteBarcode(term);

        try {
            // مرحله اول: جست‌وجوی دقیق بارکد
            try {
                const variant = await posApi.findByBarcode(term);

                // فقط همان تنوع مربوط به بارکد وارد سبد می‌شود.
                addVariant(variant);

                // نتایج قبلی و عبارت جست‌وجو پاک می‌شوند.
                setResults([]);
                setSearchText('');

                // آماده اسکن بعدی
                searchRef.current?.focus();

                return;
            } catch (error) {
                // خطای تعارض را به‌عنوان خطای واقعی نمایش بده.
                if (error instanceof ApiError && error.status === 409) {
                    toast.error(errorMessage(error));
                    return;
                }

                // فقط خطای 404 اجازه ادامه جست‌وجوی متنی دارد.
                if (!(error instanceof ApiError && error.status === 404)) {
                    throw error;
                }

                // بارکد کامل پیدا نشده؛ جست‌وجوی کل محصولات ممنوع است.
                if (completeBarcode) {
                    setResults([]);
                    setSearchText('');
                    searchRef.current?.focus();

                    toast.error('هیچ تنوعی با این بارکد پیدا نشد.');
                    return;
                }
            }

            // مرحله دوم: جست‌وجوی عادی بر اساس نام کالا یا SKU
            const products = await posApi.searchProducts(term);

            setResults(products);

            if (products.length === 0) {
                toast.message('کالایی پیدا نشد.');
            }
        } catch (error) {
            toast.error(errorMessage(error));
        }
    };

    const changeQuantity = (variantId: string, next: number) => {
        if (paymentLocked) return;

        setCart((current) =>
            current
                .map((line) =>
                    line.id === variantId
                        ? {
                            ...line,
                            quantity: Math.max(
                                0,
                                Math.min(next, line.availableQuantity),
                            ),
                        }
                        : line,
                )
                .filter((line) => line.quantity > 0),
        );
    };

    const submitSale = () => {
        if (paymentLocked) {
            toast.error(
                'یک پرداخت کارتخوان هنوز تعیین تکلیف نشده است. پرداخت جدید ارسال نکنید.',
            );
            return;
        }

        if (cart.length === 0) {
            toast.error('سبد فروش خالی است.');
            return;
        }

        const request: PosCreateSaleRequest = {
            idempotencyKey,
            paymentMethod,
            // در پرداخت کارتخوان، شماره پیگیری را Bridge و سرور ثبت می‌کنند.
            paymentReference: null,
            items: cart.map((line) => ({
                productVariantId: line.id,
                quantity: line.quantity,
            })),
        };

        saleMutation.mutate(request);
    };

    const reprintSale = async (sale: PosSale) => {
        try {
            const full = await posApi.getSale(sale.id);

            setLastSale(full);

            window.setTimeout(() => window.print(), 100);
        } catch (error) {
            toast.error(errorMessage(error));
        }
    };

    const printLastSale = () => {
        if (!lastSale) return;

        setPrinting(true);
        window.print();

        window.setTimeout(() => setPrinting(false), 700);
    };

    if (!canSell) {
        return (
            <Card>
                <CardContent className="p-6 text-sm">
                    برای استفاده از صندوق، مجوز <code>pos.sell</code> لازم است.
                </CardContent>
            </Card>
        );
    }

    return (
        <div className="space-y-6" dir="rtl">
            <style>{`
                .pos-receipt-print {
                    display: none;
                }

                @media print {
                    @page {
                        size: 80mm auto;
                        margin: 0;
                    }

                    html,
                    body,
                    #root {
                        width: 80mm !important;
                        min-width: 0 !important;
                        margin: 0 !important;
                        padding: 0 !important;
                    }

                    body * {
                        visibility: hidden !important;
                    }

                    .pos-receipt-print,
                    .pos-receipt-print * {
                        visibility: visible !important;
                    }

                    .pos-receipt-print {
                        display: block !important;
                        position: absolute !important;
                        top: 0 !important;
                        left: 0 !important;
                        right: 0 !important;

                        width: 72mm !important;
                        max-width: 72mm !important;
                        box-sizing: border-box !important;

                        margin: 0 auto !important;
                        padding: 1mm !important;

                        background: #fff !important;
                        color: #000 !important;
                        border: 0 !important;
                        border-radius: 0 !important;
                        box-shadow: none !important;

                        direction: rtl !important;
                        font-size: 9pt !important;
                    }

                    .pos-receipt-print > div:first-child {
                        margin-bottom: 2mm !important;
                        padding-bottom: 2mm !important;
                    }

                    .pos-receipt-print .receipt-details {
                        margin-bottom: 2mm !important;
                        padding-bottom: 2mm !important;
                    }

                    .pos-receipt-print .receipt-footer {
                        margin-top: 2mm !important;
                        padding-top: 2mm !important;
                    }

                    .pos-receipt-print * {
                        box-sizing: border-box !important;
                        color: #000 !important;
                    }

                    .pos-receipt-print img {
                        max-width: 32mm !important;
                        max-height: 16mm !important;
                        object-fit: contain !important;
                    }

                    .pos-receipt-print table {
                        width: 100% !important;
                        table-layout: fixed !important;
                        border-collapse: collapse !important;
                    }

                    .pos-receipt-print th,
                    .pos-receipt-print td {
                        padding: 0.5mm !important;
                    }

                    .pos-receipt-print tr {
                        break-inside: avoid !important;
                    }
                }
            `}</style>

            <div className="pos-hide-print flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold">صندوق فروش حضوری</h1>
                    <p className="mt-1 text-sm text-muted-foreground">
                        اسکن بارکد، انتخاب تنوع، ثبت فاکتور و کسر موجودی مشترک فروشگاه
                    </p>
                </div>

                {canRead && (
                    <Button
                        variant="outline"
                        onClick={() => {
                            void salesQuery.refetch();
                        }}
                        disabled={salesQuery.isFetching}
                    >
                        <RefreshCw
                            className={`ms-2 size-4 ${salesQuery.isFetching ? 'animate-spin' : ''}`}
                        />
                        تازه‌سازی فاکتورها
                    </Button>
                )}
            </div>

            {paymentLocked && (
                <Card className="pos-hide-print border-amber-500">
                    <CardContent className="p-4 text-sm">
                        <div className="font-semibold text-amber-700">
                            پرداخت کارتخوان در حال پیگیری است
                        </div>

                        <p className="mt-1 text-muted-foreground">
                            تا دریافت نتیجه قطعی از سرور، فروش جدید ثبت نکنید و
                            کارتخوان را برای همین فاکتور دوباره فعال نکنید.
                        </p>

                        {pendingSaleQuery.data && (
                            <p className="mt-2">
                                شماره فاکتور:{' '}
                                <strong>{pendingSaleQuery.data.saleNumber}</strong>
                                {' — '}
                                وضعیت:{' '}
                                <strong>
                                    {pendingSaleQuery.data.status === 'ReconciliationRequired'
                                        ? 'نیازمند تطبیق بانکی'
                                        : pendingSaleQuery.data.status === 'PaymentInProgress'
                                            ? 'در حال پرداخت'
                                            : 'در انتظار ارسال به کارتخوان'}
                                </strong>
                            </p>
                        )}

                        {paymentStatusQuery.isError && (
                            <p className="mt-2 text-destructive">
                                دریافت وضعیت پرداخت با خطا مواجه شده است. تا بررسی وضعیت
                                بانکی، پرداخت را مجدداً ارسال نکنید.
                            </p>
                        )}
                    </CardContent>
                </Card>
            )}

            <div className="pos-hide-print grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(360px,0.85fr)]">
                <div className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Barcode className="size-5" />
                                جست‌وجوی کالا / اسکن بارکد
                            </CardTitle>
                        </CardHeader>

                        <CardContent>
                            <form
                                className="flex gap-2"
                                onSubmit={(event) => {
                                    event.preventDefault();
                                    void runSearch();
                                }}
                            >
                                <Input
                                    ref={searchRef}
                                    value={searchText}
                                    onChange={(event) => setSearchText(event.target.value)}
                                    placeholder="بارکد، نام کالا یا SKU را اسکن/وارد کنید…"
                                    autoComplete="off"
                                    autoFocus
                                    disabled={paymentLocked}
                                />

                                <Button type="submit" disabled={paymentLocked}>
                                    <Search className="ms-2 size-4" />
                                    جست‌وجو
                                </Button>
                            </form>

                            <p className="mt-2 text-xs text-muted-foreground">
                                با اسکن بارکد، همان تنوع دقیق به سبد اضافه می‌شود.
                                برای جست‌وجوی نام کالا یا SKU، عبارت را وارد کنید.
                            </p>
                        </CardContent>
                    </Card>

                    {results.length > 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle>نتایج جست‌وجو</CardTitle>
                            </CardHeader>

                            <CardContent className="space-y-4">
                                {results.map((product) => (
                                    <div
                                        key={product.id}
                                        className="rounded-lg border p-3"
                                    >
                                        <div className="mb-3 flex items-center gap-3">
                                            {product.imageUrl && (
                                                <img
                                                    src={resolveMediaUrl(product.imageUrl)}
                                                    alt={product.name}
                                                    className="size-14 rounded-md object-cover"
                                                />
                                            )}

                                            <div>
                                                <div className="font-semibold">
                                                    {product.name}
                                                </div>
                                                <div className="text-xs text-muted-foreground">
                                                    {product.variants.length} تنوع قابل انتخاب
                                                </div>
                                            </div>
                                        </div>

                                        <div className="flex flex-wrap gap-2">
                                            {product.variants.map((variant) => (
                                                <Button
                                                    key={variant.id}
                                                    variant="outline"
                                                    size="sm"
                                                    disabled={
                                                        paymentLocked ||
                                                        variant.availableQuantity < 1
                                                    }
                                                    onClick={() => selectVariant(variant)}
                                                >
                                                    <Plus className="ms-1 size-3.5" />

                                                    {variant.attributeLabels.length
                                                        ? variant.attributeLabels.join(' / ')
                                                        : variant.sku}

                                                    {' — '}
                                                    {formatMoney(
                                                        variant.unitPrice,
                                                        variant.currency,
                                                        locale,
                                                    )}

                                                    {' — '}
                                                    موجودی: {variant.availableQuantity}
                                                </Button>
                                            ))}
                                        </div>
                                    </div>
                                ))}
                            </CardContent>
                        </Card>
                    )}

                    {canRead && (
                        <Card>
                            <CardHeader>
                                <CardTitle>فاکتورهای اخیر</CardTitle>
                            </CardHeader>

                            <CardContent>
                                {salesQuery.isLoading ? (
                                    <p className="text-sm text-muted-foreground">
                                        در حال بارگذاری…
                                    </p>
                                ) : salesQuery.isError ? (
                                    <div className="space-y-2 text-sm">
                                        <p>دریافت فاکتورهای اخیر ناموفق بود.</p>
                                        <Button
                                            variant="outline"
                                            onClick={() => {
                                                void salesQuery.refetch();
                                            }}
                                        >
                                            تلاش دوباره
                                        </Button>
                                    </div>
                                ) : (salesQuery.data ?? []).length === 0 ? (
                                    <p className="text-sm text-muted-foreground">
                                        هنوز فاکتوری ثبت نشده است.
                                    </p>
                                ) : (
                                    <div className="space-y-2">
                                        {(salesQuery.data ?? []).map((sale) => (
                                            <div
                                                key={sale.id}
                                                className="flex flex-wrap items-center justify-between gap-3 rounded-md border p-3"
                                            >
                                                <div>
                                                    <div className="font-medium">
                                                        {sale.saleNumber}
                                                    </div>
                                                    <div className="text-xs text-muted-foreground">
                                                        {new Date(sale.createdAt).toLocaleString(locale)}
                                                        {' · '}
                                                        {paymentLabel(sale.paymentMethod)}
                                                    </div>
                                                </div>

                                                <div className="flex items-center gap-3">
                                                    <span className="text-sm font-semibold">
                                                        {formatMoney(
                                                            sale.totalAmount,
                                                            sale.currency,
                                                            locale,
                                                        )}
                                                    </span>

                                                    <Button
                                                        size="sm"
                                                        variant="outline"
                                                        onClick={() => {
                                                            void reprintSale(sale);
                                                        }}
                                                    >
                                                        <Printer className="ms-1 size-4" />
                                                        چاپ
                                                    </Button>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    )}
                </div>

                <Card className="h-fit">
                    <CardHeader>
                        <CardTitle className="flex items-center justify-between gap-3">
                            <span>سبد فروش</span>
                            <Badge variant="secondary">{itemCount} عدد</Badge>
                        </CardTitle>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        {cart.length === 0 ? (
                            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
                                سبد خالی است. محصولی را اسکن کنید یا از نتایج
                                جست‌وجو انتخاب کنید.
                            </div>
                        ) : (
                            <div className="space-y-3">
                                {cart.map((line) => (
                                    <div
                                        key={line.id}
                                        className="rounded-lg border p-3"
                                    >
                                        <div className="flex items-start justify-between gap-3">
                                            <div className="min-w-0">
                                                <div className="font-medium">
                                                    {line.productName}
                                                </div>

                                                <div className="mt-1 text-xs text-muted-foreground">
                                                    {line.attributeLabels.join(' · ') || line.sku}
                                                </div>

                                                <div className="text-xs text-muted-foreground">
                                                    SKU: {line.sku}
                                                    {line.barcode
                                                        ? ` · بارکد: ${line.barcode}`
                                                        : ''}
                                                </div>
                                            </div>

                                            <Button
                                                size="icon"
                                                variant="ghost"
                                                aria-label="حذف کالا"
                                                disabled={paymentLocked}
                                                onClick={() => changeQuantity(line.id, 0)}
                                            >
                                                <Trash2 className="size-4 text-destructive" />
                                            </Button>
                                        </div>

                                        <div className="mt-3 flex items-center justify-between gap-3">
                                            <div className="flex items-center gap-2">
                                                <Button
                                                    size="icon"
                                                    variant="outline"
                                                    aria-label="کاهش تعداد"
                                                    disabled={
                                                        paymentLocked || line.quantity <= 1
                                                    }
                                                    onClick={() =>
                                                        changeQuantity(
                                                            line.id,
                                                            line.quantity - 1,
                                                        )
                                                    }
                                                >
                                                    <Minus className="size-3.5" />
                                                </Button>

                                                <span className="min-w-7 text-center font-semibold">
                                                    {line.quantity}
                                                </span>

                                                <Button
                                                    size="icon"
                                                    variant="outline"
                                                    aria-label="افزایش تعداد"
                                                    disabled={
                                                        paymentLocked ||
                                                        line.quantity >= line.availableQuantity
                                                    }
                                                    onClick={() =>
                                                        changeQuantity(
                                                            line.id,
                                                            line.quantity + 1,
                                                        )
                                                    }
                                                >
                                                    <Plus className="size-3.5" />
                                                </Button>
                                            </div>

                                            <div className="text-left">
                                                <div className="font-semibold">
                                                    {formatMoney(
                                                        line.unitPrice * line.quantity,
                                                        line.currency,
                                                        locale,
                                                    )}
                                                </div>

                                                <div className="text-xs text-muted-foreground">
                                                    هر عدد{' '}
                                                    {formatMoney(
                                                        line.unitPrice,
                                                        line.currency,
                                                        locale,
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}

                        <div className="border-t pt-4">
                            <div className="flex items-center justify-between text-sm">
                                <span>تعداد اقلام</span>
                                <span>{itemCount}</span>
                            </div>

                            <div className="mt-2 flex items-center justify-between text-lg font-bold">
                                <span>مبلغ قابل پرداخت</span>
                                <span>
                                    {formatMoney(subtotal, currency, locale)}
                                </span>
                            </div>
                        </div>

                        <div className="space-y-3 rounded-lg bg-muted/40 p-3">
                            <div className="text-sm font-semibold">
                                روش پرداخت
                            </div>

                            <div className="grid grid-cols-2 gap-2">
                                <Button
                                    type="button"
                                    variant={paymentMethod === 'Cash' ? 'default' : 'outline'}
                                    disabled={paymentLocked}
                                    onClick={() => setPaymentMethod('Cash')}
                                >
                                    <Banknote className="ms-2 size-4" />
                                    نقدی
                                </Button>

                                <Button
                                    type="button"
                                    variant={paymentMethod === 'Card' ? 'default' : 'outline'}
                                    disabled={paymentLocked}
                                    onClick={() => setPaymentMethod('Card')}
                                >
                                    <CreditCard className="ms-2 size-4" />
                                    کارتخوان ملت
                                </Button>
                            </div>

                            {paymentMethod === 'Card' && (
                                <p className="text-xs text-muted-foreground">
                                    پس از ثبت فروش، مبلغ از طریق نرم‌افزار Bridge
                                    به کارتخوان ملت ارسال می‌شود. نتیجه پرداخت
                                    به‌صورت خودکار از سرور دریافت خواهد شد.
                                    شماره پیگیری را دستی وارد نکنید.
                                </p>
                            )}
                        </div>

                        <div className="flex gap-2">
                            <Button
                                className="flex-1"
                                onClick={submitSale}
                                disabled={
                                    cart.length === 0 ||
                                    saleMutation.isPending ||
                                    paymentLocked
                                }
                            >
                                {saleMutation.isPending
                                    ? 'در حال ثبت…'
                                    : paymentLocked
                                        ? 'در انتظار نتیجه کارتخوان…'
                                        : 'ثبت فروش و صدور فاکتور'}
                            </Button>

                            <Button
                                variant="outline"
                                onClick={() => setCart([])}
                                disabled={
                                    cart.length === 0 ||
                                    saleMutation.isPending ||
                                    paymentLocked
                                }
                            >
                                پاک‌کردن
                            </Button>
                        </div>

                        <p className="text-xs text-muted-foreground">
                            قیمت و موجودی نهایی در سرور دوباره بررسی می‌شود.
                            در پرداخت کارتخوان، موجودی تا مشخص‌شدن نتیجه پرداخت
                            رزرو خواهد شد.
                        </p>
                    </CardContent>
                </Card>
            </div>

            {lastSale && (
                <Card className="pos-receipt-print">
                    <div className="mb-3 flex flex-col items-center border-b pb-3 text-center">
                        <div className="text-lg font-bold">
                            {appearance?.storeName?.trim() || 'گالری لباس پاتلت'}
                        </div>

                        {appearance?.contactPhone && (
                            <div className="mt-1 text-xs">
                                {appearance.contactPhone}
                            </div>
                        )}

                        <div className="mt-2 font-bold">
                            فاکتور فروش حضوری
                        </div>
                    </div>

                    <CardContent className="p-0">
                        <div className="receipt-details mb-4 flex flex-wrap items-start justify-between gap-3 border-b pb-4">
                            <div>
                                <div className="text-lg font-bold">
                                    {lastSale.saleNumber}
                                </div>
                                <div className="text-sm">
                                    تاریخ:{' '}
                                    {new Date(lastSale.createdAt).toLocaleString(locale)}
                                </div>
                            </div>

                            <div className="text-sm">
                                روش پرداخت: {paymentLabel(lastSale.paymentMethod)}
                                {lastSale.paymentReference
                                    ? ` · پیگیری: ${lastSale.paymentReference}`
                                    : ''}
                            </div>
                        </div>

                        <table className="w-full table-fixed border-collapse text-[8pt]">
                            <colgroup>
                                <col style={{ width: '40%' }} />
                                <col style={{ width: '8%' }} />
                                <col style={{ width: '26%' }} />
                                <col style={{ width: '26%' }} />
                            </colgroup>

                            <thead>
                                <tr className="text-center">
                                    <th className="border border-black p-0">
                                        کالا
                                    </th>
                                    <th className="border border-black p-0">
                                        تعداد
                                    </th>
                                    <th className="border border-black p-0">
                                        قیمت واحد
                                    </th>
                                    <th className="border border-black p-0">
                                        جمع
                                    </th>
                                </tr>
                            </thead>

                            <tbody>
                                {lastSale.items.map((item, i) => (
                                    <tr key={`${item.productVariantId}-${i}`}>
                                        <td className="border border-black p-0 align-top break-words">
                                            <div className="font-medium">
                                                {item.productName}
                                            </div>

                                            <div className="text-[7pt]">
                                                {item.attributeLabels.join(' · ')}
                                                {' — '}
                                                {item.sku}
                                            </div>
                                        </td>

                                        <td className="border border-black p-0 text-center align-middle">
                                            {item.quantity}
                                        </td>

                                        <td className="border border-black p-0 text-center align-middle break-all">
                                            {formatMoney(
                                                item.unitPrice,
                                                lastSale.currency,
                                                locale,
                                            )}
                                        </td>

                                        <td className="border border-black p-0 text-center align-middle break-all">
                                            {formatMoney(
                                                item.lineTotal,
                                                lastSale.currency,
                                                locale,
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>

                        <div className="mt-4 flex justify-end">
                            <div className="text-lg font-bold">
                                مبلغ کل:{' '}
                                {formatMoney(
                                    lastSale.totalAmount,
                                    lastSale.currency,
                                    locale,
                                )}
                            </div>
                        </div>

                        <div className="receipt-footer mt-5 border-t border-dashed border-black pt-3">
                            <div className="flex items-center gap-2" dir="rtl">
                                <div className="flex w-[23mm] shrink-0 flex-col items-center">
                                    <QRCodeSVG
                                        value={websiteQrUrl}
                                        size={84}
                                        level="M"
                                        includeMargin
                                        bgColor="#FFFFFF"
                                        fgColor="#000000"
                                    />

                                    <span className="mt-1 text-[6pt] font-bold">
                                        اسکن و ورود به سایت
                                    </span>
                                </div>

                                <div className="min-w-0 flex-1 text-center">
                                    <div className="text-[9pt] font-bold">
                                        از خرید شما سپاسگزاریم
                                    </div>

                                    <div className="mt-3 text-[7pt]">
                                        وب‌سایت فروشگاه
                                    </div>

                                    <div
                                        dir="ltr"
                                        className="mt-0.5 text-[8pt] font-bold"
                                    >
                                        {websiteAddress}
                                    </div>

                                    <div className="mt-3 text-[7pt]">
                                        ما را در اینستاگرام دنبال کنید
                                    </div>

                                    <div
                                        dir="ltr"
                                        className="mt-0.5 text-[8pt] font-bold"
                                    >
                                        instagram.com/patlet.dress
                                    </div>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}

            {lastSale && (
                <div className="pos-hide-print flex justify-end">
                    <Button
                        variant="outline"
                        onClick={printLastSale}
                        disabled={printing}
                    >
                        <Printer className="ms-2 size-4" />
                        چاپ فاکتور {lastSale.saleNumber}
                    </Button>
                </div>
            )}
        </div>
    );
}
