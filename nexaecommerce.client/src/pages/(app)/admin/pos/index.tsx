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
const POS_RESTORED_SALES_KEY = 'nexa:pos:restored-sales:v1';
const canSellPermission = 'pos.sell';

type CartLine = PosVariant & { quantity: number };
const POS_CART_SESSION_KEY = 'nexa:pos:cart:v1';
const POS_LAST_SALE_SESSION_KEY = 'nexa:pos:last-sale:v1';

function readPosSessionValue<T>(key: string, fallback: T): T {
    try {
        if (typeof window === 'undefined') return fallback;

        const raw = window.sessionStorage.getItem(key);

        return raw ? (JSON.parse(raw) as T) : fallback;
    } catch {
        return fallback;
    }
}

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

    if (method === 'ManualCard') {
        return 'کارتخوان دستی';
    }

    if (method === 'Card' || method === 'BankMellat') {
        return 'کارتخوان ملت';
    }

    return method;
}
function saleStatusLabel(status: string): string {
    switch (status) {
        case 'Paid':
            return 'پرداخت‌شده';

        case 'Cancelled':
            return 'لغوشده ـ پرداخت قطعی نشده';

        case 'PendingPayment':
            return 'در انتظار ارسال به کارتخوان';

        case 'PaymentInProgress':
            return 'در حال پرداخت';

        case 'ReconciliationRequired':
            return 'نیازمند تطبیق بانکی';

        default:
            return 'وضعیت نامشخص';
    }
}

function saleStatusClass(status: string): string {
    switch (status) {
        case 'Paid':
            return 'bg-emerald-100 text-emerald-800';

        case 'Cancelled':
        case 'ReconciliationRequired':
            return 'bg-red-100 text-red-800';

        case 'PaymentInProgress':
        case 'PendingPayment':
            return 'bg-amber-100 text-amber-800';

        default:
            return 'bg-muted text-muted-foreground';
    }
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
    const [cart, setCart] = useState<CartLine[]>(() => {
        const saved = readPosSessionValue<unknown>(
            POS_CART_SESSION_KEY,
            [],
        );

        return Array.isArray(saved)
            ? (saved as CartLine[])
            : [];
    });

    const [lastSale, setLastSale] = useState<PosSale | null>(() => {
        const saved = readPosSessionValue<unknown>(
            POS_LAST_SALE_SESSION_KEY,
            null,
        );

        if (
            saved &&
            typeof saved === 'object' &&
            'id' in saved &&
            'items' in saved
        ) {
            return saved as PosSale;
        }

        return null;
    });
    const [paymentMethod, setPaymentMethod] =
        useState<'Cash' | 'Card' | 'ManualCard'>('Cash');

    const [manualPaymentReference, setManualPaymentReference] =
        useState('');

    const [separateSaleUnlocked, setSeparateSaleUnlocked] =
        useState(false);
    const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());

    const [printing, setPrinting] = useState(false);

    // نگهداری فاکتورهایی که در همین نشست، اقلامشان به سبد بازگردانده شده است.
    const [restoredSaleIds, setRestoredSaleIds] = useState<Set<string>>(() => {
        const saved = readPosSessionValue<unknown>(
            POS_RESTORED_SALES_KEY,
            [],
        );

        return new Set(
            Array.isArray(saved)
                ? saved.filter(
                    (id): id is string => typeof id === 'string',
                )
                : [],
        );
    });

    // برای جلوگیری از نمایش چندباره پیام یک وضعیت بانکی
    const notifiedPaymentStatusesRef = useRef<Set<string>>(new Set());

    const hasUnresolvedCardPayment =
        Boolean(pendingCardSaleId);

    const paymentLocked =
        hasUnresolvedCardPayment && !separateSaleUnlocked;
    // حفظ سبد در Refresh صفحه؛ فقط در نشست همین تب مرورگر
    useEffect(() => {
        try {
            if (cart.length > 0) {
                sessionStorage.setItem(
                    POS_CART_SESSION_KEY,
                    JSON.stringify(cart),
                );
            } else {
                sessionStorage.removeItem(POS_CART_SESSION_KEY);
            }
        } catch {
            // در صورت غیرفعال بودن Storage، صندوق همچنان کار می‌کند.
        }
    }, [cart]);

    // حفظ آخرین فاکتور و وضعیت پرداخت آن
    useEffect(() => {
        try {
            if (lastSale) {
                sessionStorage.setItem(
                    POS_LAST_SALE_SESSION_KEY,
                    JSON.stringify(lastSale),
                );
            } else {
                sessionStorage.removeItem(POS_LAST_SALE_SESSION_KEY);
            }
        } catch {
            // عدم دسترسی به Storage نباید مانع فروش شود.
        }
    }, [lastSale]);

    // فاکتورهای بازیابی‌شده تا پایان نشست فعلی حفظ می‌شوند؛
    // خالی‌شدن سبد نباید باعث امکان بازیابی دوباره همان فاکتور شود.
    useEffect(() => {
        try {
            if (restoredSaleIds.size === 0) {
                sessionStorage.removeItem(POS_RESTORED_SALES_KEY);
            } else {
                sessionStorage.setItem(
                    POS_RESTORED_SALES_KEY,
                    JSON.stringify(Array.from(restoredSaleIds)),
                );
            }
        } catch {
            // عدم دسترسی به Storage نباید مانع فروش شود.
        }
    }, [restoredSaleIds]);

    const salesQuery = useQuery({
        queryKey: ['pos', 'sales', 0, 100],
        queryFn: () => posApi.listSales(0, 100),
        enabled: canRead,
        staleTime: 15_000,
    });
    const bridgeAvailabilityQuery = useQuery({
        queryKey: ['pos', 'bridge', 'availability'],
        queryFn: () => posApi.getBridgeAvailability(),
        enabled: canSell,
        staleTime: 0,
        refetchInterval: 15_000,
        refetchOnWindowFocus: true,
        retry: false,
    });

    const bridgeOnline =
        bridgeAvailabilityQuery.data?.isOnline === true;
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
    const lastSaleStatusQuery = useQuery({
        queryKey: ['pos', 'last-sale-status', lastSale?.id],
        queryFn: () => posApi.getPaymentStatus(lastSale!.id),
        enabled: Boolean(lastSale?.id) && !pendingCardSaleId && canSell,
        staleTime: 0,
        refetchOnWindowFocus: true,
        retry: 1,
    });
    useEffect(() => {
        const status = lastSaleStatusQuery.data;

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
    }, [lastSaleStatusQuery.data]);
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
            if (notifiedPaymentStatusesRef.current.has(notificationKey)) {
                return;
            }

            notifiedPaymentStatusesRef.current.add(notificationKey);

            const hasSeparateCart = separateSaleUnlocked;

            setPendingCardSaleId(null);
            setSeparateSaleUnlocked(false);

            // اگر فروشنده فاکتور جدیدی شروع کرده باشد،
            // پرداخت فاکتور قبلی نباید سبد جدید را پاک کند.
            if (!hasSeparateCart) {
                setCart([]);
                setSearchText('');
                setResults([]);
                setIdempotencyKey(crypto.randomUUID());
            }

            toast.success(
                'پرداخت کارتخوان تأیید شد و فاکتور نهایی گردید.',
            );

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
                'فاکتور لغو شد؛ پرداخت قطعی نیست و موجودی رزروشده آزاد شده است.',
            );

            void openPaymentQuery.refetch();

            if (canRead) {
                void salesQuery.refetch();
            }

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
        openPaymentQuery, separateSaleUnlocked,
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
            setManualPaymentReference('');
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
    const cancelUnclaimedPaymentMutation = useMutation({
        mutationFn: (saleId: string) =>
            posApi.cancelUnclaimedPayment(saleId),

        onSuccess: async (result) => {
            if (result.status !== 'Cancelled') {
                toast.error(
                    'درخواست لغو تأیید نشد؛ وضعیت فاکتور را دوباره بررسی کنید.',
                );
                return;
            }

            setPendingCardSaleId(null);
            setSeparateSaleUnlocked(false);

            // سبد قبلی حفظ می‌شود، اما کلید فاکتور باید جدید باشد.
            // وگرنه API فاکتور لغوشده قبلی را برمی‌گرداند.
            setIdempotencyKey(crypto.randomUUID());

            setPaymentMethod('ManualCard');
            setManualPaymentReference('');
            setLastSale(null);

            await Promise.all([
                openPaymentQuery.refetch(),
                salesQuery.refetch(),
            ]);

            toast.success(
                'درخواست ارسال‌نشده با موفقیت لغو شد. سبد حفظ شده است؛ پس از دریافت وجه با کارتخوان دستی، فروش را ثبت کنید.',
            );

            searchRef.current?.focus();
        },

        onError: (error) => {
            toast.error(errorMessage(error));
        },
    });
    const restoreCancelledSaleMutation = useMutation({
        mutationFn: (saleId: string) =>
            posApi.restoreCancelledSaleItems(saleId),

        onSuccess: (items, saleId) => {
            // از تکرار بازیابی همان فاکتور در همین نشست جلوگیری کن.
            if (restoredSaleIds.has(saleId)) {
                toast.message('اقلام این فاکتور قبلاً به سبد بازگردانده شده‌اند.');
                return;
            }

            const requestedTotal = items.reduce(
                (sum, item) => sum + item.requestedQuantity,
                0,
            );

            const restoredTotal = items.reduce(
                (sum, item) => sum + item.restoreQuantity,
                0,
            );

            if (restoredTotal === 0) {
                toast.error(
                    'هیچ‌یک از اقلام این فاکتور در حال حاضر موجودی قابل فروش ندارند.',
                );
                return;
            }

            // تعداد واقعی قابل افزودن به سبد را با درنظرگرفتن اقلام فعلی محاسبه کن.
            const currentQuantities = new Map(
                cart.map((line) => [line.id, line.quantity]),
            );

            const actualRestoredTotal = items.reduce((sum, item) => {
                const variant = item.variant;
                if (!variant || item.restoreQuantity <= 0) return sum;

                const currentQuantity =
                    currentQuantities.get(item.productVariantId) ?? 0;

                const additionalQuantity = Math.max(
                    0,
                    Math.min(
                        item.restoreQuantity,
                        variant.availableQuantity - currentQuantity,
                    ),
                );

                return sum + additionalQuantity;
            }, 0);

            if (actualRestoredTotal === 0) {
                toast.message(
                    'سبد فعلی برای این تنوع‌ها به سقف موجودی قابل فروش رسیده است؛ تعداد جدیدی اضافه نشد.',
                );
                return;
            }

            setCart((current) => {
                const merged = new Map<string, CartLine>();

                for (const line of current) {
                    merged.set(line.id, line);
                }

                for (const item of items) {
                    const variant = item.variant;

                    // تنوع غیرفعال یا ناموجود قابل بازیابی نیست.
                    // اقلامی را که از قبل در سبد بوده‌اند حذف نکن.
                    if (!variant || item.restoreQuantity <= 0) {
                        continue;
                    }

                    const existing = merged.get(item.productVariantId);

                    // مقدار این فاکتور به تعداد موجود در سبد اضافه می‌شود.
                    // کل تعداد در نهایت به موجودی قابل فروش فعلی محدود می‌ماند.
                    const alreadyRestored = restoredSaleIds.has(saleId);

                    const desiredQuantity = alreadyRestored
                        ? Math.max(
                            existing?.quantity ?? 0,
                            item.restoreQuantity,
                        )
                        : (existing?.quantity ?? 0) + item.restoreQuantity;
                    const quantity = Math.min(
                        desiredQuantity,
                        variant.availableQuantity,
                    );

                    if (quantity <= 0) {
                        merged.delete(item.productVariantId);
                        continue;
                    }

                    // قیمت و موجودی از اطلاعات تازه سرور جایگزین می‌شود.
                    merged.set(item.productVariantId, {
                        ...variant,
                        quantity,
                    });
                }

                return Array.from(merged.values());
            });

            // کلید قبلی ممکن است متعلق به فاکتور لغوشده باشد.
            setIdempotencyKey(crypto.randomUUID());

            setSearchText('');
            setResults([]);

            // حتی بازیابی ناقص نیز ثبت می‌شود تا همین فاکتور دوباره
            // به سبد اضافه نشود و تعدادها ناخواسته افزایش پیدا نکنند.
            setRestoredSaleIds((current) =>
                new Set([...current, saleId]),
            );

            if (actualRestoredTotal < requestedTotal) {
                toast.message(
                    `${actualRestoredTotal} عدد از ${requestedTotal} عدد به دلیل محدودیت موجودی یا تعداد فعلی سبد اضافه شد. فاکتور برای جلوگیری از بازیابی تکراری علامت‌گذاری شد.`,
                );
            } else {
                toast.success(
                    `اقلام فاکتور ${saleId.slice(0, 8)} با قیمت و موجودی فعلی به سبد بازگشت.`,
                );
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

    const currency = cart[0]?.currency ?? lastSale?.currency ?? 'IRT';

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
        if (paymentMethod === 'Card') {
            if (bridgeAvailabilityQuery.isError) {
                toast.error(
                    'وضعیت Bridge از سرور قابل دریافت نیست. ابتدا ارتباط با سرور نکسا را بررسی کنید.',
                );
                return;
            }

            if (!bridgeOnline) {
                toast.error(
                    'Bridge در دسترس نیست. لطفاً کارتخوان فیزیکی را به‌صورت دستی استفاده و روش پرداخت «کارتخوان دستی» را انتخاب کنید.',
                );
                return;
            }

            if (hasUnresolvedCardPayment) {
                toast.error(
                    'یک پرداخت کارتخوان قبلی هنوز تعیین تکلیف نشده است. پرداخت متصل جدید مجاز نیست.',
                );
                return;
            }
        }
        if (cart.length === 0) {
            toast.error('سبد فروش خالی است.');
            return;
        }

        const request: PosCreateSaleRequest = {
            idempotencyKey,
            paymentMethod,
            // در پرداخت کارتخوان، شماره پیگیری را Bridge و سرور ثبت می‌کنند.
            paymentReference:
                paymentMethod === 'ManualCard'
                    ? manualPaymentReference.trim() || null
                    : null,
            items: cart.map((line) => ({
                productVariantId: line.id,
                quantity: line.quantity,
            })),
        };

        saleMutation.mutate(request);
    };
    const startSeparateSale = () => {
        if (!pendingCardSaleId) return;

        setSeparateSaleUnlocked(true);

        // سبد جدید باید کاملاً از سبد فاکتور قبلی جدا باشد.
        setCart([]);
        setSearchText('');
        setResults([]);
        setIdempotencyKey(crypto.randomUUID());
        setManualPaymentReference('');

        // اگر Bridge قطع است، کارتخوان دستی گزینه پیش‌فرض باشد.
        setPaymentMethod(
            bridgeOnline ? 'Cash' : 'ManualCard',
        );

        toast.message(
            'فاکتور قبلی همچنان در حال پیگیری است؛ سبد جدید مستقل از آن فعال شد.',
        );

        searchRef.current?.focus();
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
            {bridgeAvailabilityQuery.data &&
                !bridgeAvailabilityQuery.data.isOnline && (
                    <Card className="pos-hide-print border-amber-500">
                        <CardContent className="p-4">
                            <div className="font-semibold text-amber-700">
                                کارتخوان متصل به نکسا در دسترس نیست
                            </div>

                            <p className="mt-2 text-sm text-muted-foreground">
                                {bridgeAvailabilityQuery.data.message}
                            </p>

                            <p className="mt-2 text-sm">
                                می‌توانید با کارتخوان فیزیکی و خارج از اتصال نکسا
                                پرداخت را انجام دهید. سپس روش پرداخت
                                «کارتخوان دستی» را انتخاب و فاکتور را ثبت کنید.
                            </p>
                        </CardContent>
                    </Card>
                )}

            {bridgeAvailabilityQuery.isError && (
                <Card className="pos-hide-print border-amber-500">
                    <CardContent className="p-4 text-sm">
                        <div className="font-semibold text-amber-700">
                            وضعیت ارتباط Bridge قابل بررسی نیست
                        </div>

                        ارتباط با سرور برای بررسی وضعیت کارتخوان برقرار نشد.
                        قبل از شروع پرداخت متصل، ارتباط با سرور نکسا را بررسی کنید.
                    </CardContent>
                </Card>
            )}
            {hasUnresolvedCardPayment && (
                <Card className="pos-hide-print border-amber-500">
                    <CardContent className="space-y-3 p-4 text-sm">
                        <div className="font-semibold text-amber-700">
                            پرداخت کارتخوان نیازمند تعیین تکلیف است
                        </div>

                        <p className="text-muted-foreground">
                            {pendingSaleQuery.data?.status === 'PendingPayment'
                                ? !bridgeOnline
                                    ? 'Bridge در دسترس نیست و این فاکتور هنوز از صف Bridge دریافت نشده است. می‌توانید درخواست ارسال‌نشده را امن لغو کنید و همین سبد را به‌صورت دستی ثبت کنید.'
                                    : 'فاکتور هنوز توسط Bridge دریافت نشده است. تا مشخص‌شدن وضعیت آن، پرداخت متصل دیگری ارسال نکنید.'
                                : 'درخواست پرداخت ممکن است به کارتخوان رسیده باشد. قبل از هر اقدام، وضعیت تراکنش را بررسی کنید و برای همین فاکتور پرداخت مجدد انجام ندهید.'}
                        </p>

                        {pendingSaleQuery.data && (
                            <p>
                                شماره فاکتور:
                                {' '}
                                <strong>
                                    {pendingSaleQuery.data.saleNumber}
                                </strong>
                                {' — '}
                                وضعیت:
                                {' '}
                                <strong>
                                    {pendingSaleQuery.data.status === 'ReconciliationRequired'
                                        ? 'نیازمند تطبیق بانکی'
                                        : pendingSaleQuery.data.status === 'PaymentInProgress'
                                            ? 'در حال پرداخت'
                                            : pendingSaleQuery.data.status === 'Paid'
                                                ? 'پرداخت موفق'
                                                : pendingSaleQuery.data.status === 'Cancelled'
                                                    ? 'لغوشده'
                                                    : 'در انتظار ارسال به کارتخوان'}
                                </strong>
                            </p>
                        )}

                        {paymentStatusQuery.isError && (
                            <p className="text-destructive">
                                دریافت وضعیت پرداخت با خطا مواجه شده است.
                                پرداخت را مجدداً ارسال نکنید.
                            </p>
                        )}

                        {!separateSaleUnlocked &&
                            pendingSaleQuery.data?.status === 'PendingPayment' &&
                            bridgeAvailabilityQuery.data &&
                            !bridgeAvailabilityQuery.data.isOnline && (
                                <Button
                                    variant="outline"
                                    className="w-full"
                                    disabled={cancelUnclaimedPaymentMutation.isPending}
                                    onClick={() =>
                                        cancelUnclaimedPaymentMutation.mutate(
                                            pendingCardSaleId!,
                                        )
                                    }
                                >
                                    {cancelUnclaimedPaymentMutation.isPending
                                        ? 'در حال لغو امن درخواست…'
                                        : 'لغو امن درخواست و ادامه همین سبد به‌صورت دستی'}
                                </Button>
                            )}

                        {!separateSaleUnlocked && (
                            <Button
                                variant="outline"
                                className="w-full"
                                onClick={startSeparateSale}
                            >
                                شروع فاکتور جدید بدون لغو پرداخت قبلی
                            </Button>
                        )}

                        {separateSaleUnlocked && (
                            <p className="text-xs text-muted-foreground">
                                سبد فروش جدید مستقل از فاکتور معلق فعال است.
                                پرداخت متصل به Bridge تا تعیین تکلیف فاکتور قبلی غیرفعال خواهد بود.
                            </p>
                        )}
                    </CardContent>
                </Card>
            )}
            {lastSale && (
                <Card className="pos-hide-print">
                    <CardContent className="p-4 space-y-3">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <div className="font-semibold">
                                آخرین فاکتور صندوق
                            </div>

                            <span
                                className={`rounded-md px-3 py-1 text-sm font-semibold ${saleStatusClass(lastSale.status)}`}
                            >
                                {saleStatusLabel(lastSale.status)}
                            </span>
                        </div>

                        <div className="text-sm text-muted-foreground">
                            شماره فاکتور: <strong>{lastSale.saleNumber}</strong>
                            {' ـ '}
                            روش پرداخت: {paymentLabel(lastSale.paymentMethod)}
                            {' ـ '}
                            مبلغ: {formatMoney(
                                lastSale.totalAmount,
                                lastSale.currency,
                                locale,
                            )}
                        </div>

                        {lastSale.status === 'Paid' && (
                            <p className="text-sm text-emerald-700">
                                پرداخت با موفقیت تأیید شده است. این فاکتور فروش قطعی محسوب می‌شود.
                            </p>
                        )}

                        {lastSale.status === 'Cancelled' && (
                            <div className="space-y-3 rounded-md border border-red-200 bg-red-50 p-3">
                                <p className="text-sm font-semibold text-red-800">
                                    این فاکتور لغو شده است و پرداخت قطعی برای آن ثبت نشده است.
                                </p>

                                <p className="text-sm text-red-700">
                                    موجودی رزروشده آزاد شده است. برای فروش مجدد، اقلام را با
                                    قیمت و موجودی فعلی به سبد برگردانید.
                                </p>

                                {restoredSaleIds.has(lastSale.id) ? (
                                    <span className="inline-flex rounded-md bg-emerald-100 px-3 py-2 text-sm font-semibold text-emerald-800">
                                        اقلام این فاکتور قبلاً بازیابی شده‌اند
                                    </span>
                                ) : (
                                    <Button
                                        onClick={() =>
                                            restoreCancelledSaleMutation.mutate(lastSale.id)
                                        }
                                        disabled={restoreCancelledSaleMutation.isPending || paymentLocked}
                                    >
                                        {restoreCancelledSaleMutation.isPending
                                            ? 'در حال بررسی موجودی…'
                                            : 'بازگرداندن اقلام به سبد فروش'}
                                    </Button>
                                )}
                            </div>
                        )}

                        {lastSale.status === 'ReconciliationRequired' && (
                            <p className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800">
                                نتیجه پرداخت بانکی مشخص نیست. این فاکتور را لغوشده یا پرداخت‌نشده
                                فرض نکنید و تا تطبیق بانکی، پرداخت مجدد انجام ندهید.
                            </p>
                        )}

                        {(lastSale.status === 'PendingPayment' ||
                            lastSale.status === 'PaymentInProgress') && (
                                <p className="text-sm text-amber-700">
                                    نتیجه پرداخت هنوز قطعی نشده است. فاکتور جدیدی برای همین خرید
                                    ثبت نکنید.
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
                                        {(salesQuery.data ?? []).map((sale) => {
                                            const isCancelled = sale.status === 'Cancelled';
                                            const alreadyRestored = restoredSaleIds.has(sale.id);

                                            return (
                                                <div
                                                    key={sale.id}
                                                    className={`flex flex-wrap items-center justify-between gap-3 rounded-md border p-3 ${isCancelled
                                                        ? 'border-red-200 bg-red-50/50'
                                                        : ''
                                                        }`}
                                                >
                                                    <div className="min-w-0">
                                                        <div className="flex flex-wrap items-center gap-2">
                                                            <span className="font-medium">
                                                                {sale.saleNumber}
                                                            </span>

                                                            <span
                                                                className={`rounded-md px-2 py-0.5 text-xs font-semibold ${saleStatusClass(sale.status)}`}
                                                            >
                                                                {saleStatusLabel(sale.status)}
                                                            </span>
                                                        </div>

                                                        <div className="mt-1 text-xs text-muted-foreground">
                                                            {new Date(sale.createdAt).toLocaleString(locale)}
                                                            {' · '}
                                                            {paymentLabel(sale.paymentMethod)}
                                                        </div>
                                                    </div>

                                                    <div className="flex flex-wrap items-center gap-2">
                                                        <span className="text-sm font-semibold">
                                                            {formatMoney(
                                                                sale.totalAmount,
                                                                sale.currency,
                                                                locale,
                                                            )}
                                                        </span>

                                                        {isCancelled && (
                                                            <Button
                                                                size="sm"
                                                                variant={alreadyRestored ? 'secondary' : 'outline'}
                                                                disabled={
                                                                    paymentLocked ||
                                                                    restoreCancelledSaleMutation.isPending
                                                                }
                                                                onClick={() => {
                                                                    restoreCancelledSaleMutation.mutate(sale.id);
                                                                }}
                                                            >
                                                                {restoreCancelledSaleMutation.isPending &&
                                                                    restoreCancelledSaleMutation.variables === sale.id
                                                                    ? 'در حال بررسی موجودی…'
                                                                    : alreadyRestored
                                                                        ? 'بازیابی مجدد'
                                                                        : 'بازگرداندن اقلام'}
                                                            </Button>
                                                        )}

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
                                            );
                                        })}
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

                            <div className="grid grid-cols-3 gap-2">
                                <Button
                                    type="button"
                                    variant={
                                        paymentMethod === 'Cash'
                                            ? 'default'
                                            : 'outline'
                                    }
                                    disabled={paymentLocked}
                                    onClick={() => setPaymentMethod('Cash')}
                                >
                                    <Banknote className="ms-1 size-4" />
                                    نقدی
                                </Button>

                                <Button
                                    type="button"
                                    variant={
                                        paymentMethod === 'ManualCard'
                                            ? 'default'
                                            : 'outline'
                                    }
                                    disabled={paymentLocked}
                                    onClick={() => setPaymentMethod('ManualCard')}
                                >
                                    <CreditCard className="ms-1 size-4" />
                                    کارتخوان دستی
                                </Button>

                                <Button
                                    type="button"
                                    variant={
                                        paymentMethod === 'Card'
                                            ? 'default'
                                            : 'outline'
                                    }
                                    disabled={
                                        paymentLocked ||
                                        !bridgeOnline ||
                                        hasUnresolvedCardPayment
                                    }
                                    onClick={() => setPaymentMethod('Card')}
                                >
                                    <CreditCard className="ms-1 size-4" />
                                    کارتخوان متصل
                                </Button>
                            </div>

                            {paymentMethod === 'Card' && (
                                <p className="text-xs text-muted-foreground">
                                    مبلغ فاکتور از طریق نرم‌افزار Bridge به کارتخوان ملت
                                    ارسال می‌شود. نتیجه پرداخت به‌صورت خودکار از سرور
                                    دریافت خواهد شد.
                                </p>
                            )}

                            {paymentMethod === 'ManualCard' && (
                                <div className="space-y-2">
                                    <p className="text-sm text-muted-foreground">
                                        پرداخت را خارج از اتصال نکسا و با کارتخوان فیزیکی
                                        انجام دهید. پس از اطمینان از موفقیت پرداخت،
                                        فاکتور را ثبت کنید.
                                    </p>

                                    <Input
                                        value={manualPaymentReference}
                                        onChange={(event) =>
                                            setManualPaymentReference(event.target.value)
                                        }
                                        placeholder="شماره پیگیری بانکی (اختیاری)"
                                        autoComplete="off"
                                    />
                                </div>
                            )}

                            {!bridgeOnline && (
                                <p className="text-xs text-amber-700">
                                    پرداخت متصل به Bridge در حال حاضر قابل استفاده نیست.
                                    از گزینه کارتخوان دستی استفاده کنید.
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
                                    paymentLocked ||
                                    (
                                        paymentMethod === 'Card' &&
                                        (
                                            !bridgeOnline ||
                                            hasUnresolvedCardPayment
                                        )
                                    )
                                }
                            >
                                {saleMutation.isPending
                                    ? 'در حال ثبت…'
                                    : paymentLocked
                                        ? 'در انتظار تعیین تکلیف پرداخت قبلی…'
                                        : paymentMethod === 'ManualCard'
                                            ? 'ثبت پرداخت دستی و صدور فاکتور'
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

                        <div className="mt-2 text-center font-bold">
                            {lastSale.status === 'Paid'
                                ? 'فاکتور فروش پرداخت‌شده'
                                : lastSale.status === 'Cancelled'
                                    ? 'رسید لغو فاکتور ـ پرداخت نشده'
                                    : 'فاکتور فروش ـ وضعیت پرداخت غیرقطعی'}
                        </div>

                        <div className="mt-2 text-sm font-bold">
                            وضعیت: {saleStatusLabel(lastSale.status)}
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
                                <div className="text-sm font-bold">
                                    وضعیت پرداخت: {saleStatusLabel(lastSale.status)}
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
                                        {lastSale.status === 'Paid'
                                            ? 'از خرید شما سپاسگزاریم'
                                            : lastSale.status === 'Cancelled'
                                                ? 'این فاکتور لغو شده و رسید فروش معتبر نیست'
                                                : 'این برگه رسید قطعی پرداخت نیست'}
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