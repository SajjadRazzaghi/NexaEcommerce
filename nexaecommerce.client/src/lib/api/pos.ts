
import { api } from '@/lib/api/client';

// --------------------------------------------------
// Product and variant
// --------------------------------------------------

export type PosVariant = {
    id: string;
    productId: string;
    productName: string;
    imageUrl: string | null;
    sku: string;
    barcode: string | null;
    unitPrice: number;
    currency: string;
    availableQuantity: number;
    attributeLabels: string[];
};

export type PosProduct = {
    id: string;
    name: string;
    imageUrl: string | null;
    variants: PosVariant[];
};

// --------------------------------------------------
// Sale
// --------------------------------------------------

export type PosSaleLine = {
    productVariantId: string;
    productName: string;
    sku: string;
    barcode: string | null;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
    attributeLabels: string[];
};

export type PosSale = {
    id: string;
    saleNumber: string;
    createdAt: string;
    currency: string;
    status: string;
    paymentMethod: string;
    paymentReference: string | null;
    subtotal: number;
    totalAmount: number;
    items: PosSaleLine[];
};
export type PosRestoreCartLine = {
    productVariantId: string;
    variant: PosVariant | null;
    requestedQuantity: number;
    restoreQuantity: number;
};
// --------------------------------------------------
// Create sale request
// --------------------------------------------------

export type PosCreateSaleRequest = {
    idempotencyKey: string;
    paymentMethod: 'Cash' | 'Card' | 'ManualCard';
    paymentReference?: string | null;
    items: {
        productVariantId: string;
        quantity: number;
    }[];
};

// --------------------------------------------------
// Payment status
// --------------------------------------------------

export type PosSalePaymentStatus = {
    saleId: string;
    status: string;
    paymentReference: string | null;
};

// --------------------------------------------------
// Bridge types
// --------------------------------------------------
export type PosBridgeAvailability = {
    isConfigured: boolean;
    isOnline: boolean;
    deviceName: string | null;
    lastSeenAtUtc: string | null;
    message: string | null;
};
export type PosBridgeHealth = {
    ok: boolean;
    deviceName: string;
};

export type PosBridgePaymentWorkItem = {
    saleId: string;
    saleNumber: string;
    amount: number;
    currency: string;
};

export type PosBridgeCompletionRequest = {
    outcome: 'Succeeded' | 'Declined' | 'Unknown';
    amount: number;
    paymentReference?: string | null;
    message?: string | null;
};

export type PosBridgeCompletionResponse = {
    saleId: string;
    status: string;
};

// --------------------------------------------------
// POS API
// --------------------------------------------------
export const posApi = {
    // جست‌وجوی کالا بر اساس نام یا SKU
    searchProducts: (search: string, signal?: AbortSignal) =>
        api.get<PosProduct[]>('/pos/products', {
            params: { search },
            signal,
        }),
    getBridgeAvailability: () =>
        api.get<PosBridgeAvailability>(
            '/pos/bridge/availability',
        ),

    cancelUnclaimedPayment: (saleId: string) =>
        api.post<PosBridgeCompletionResponse>(
            `/pos/bridge/sales/${encodeURIComponent(saleId)}/cancel-unclaimed`,
            {},
        ),
    // پیدا کردن تنوع دقیق با بارکد
    findByBarcode: (barcode: string) =>
        api.get<PosVariant>(
            `/pos/barcodes/${encodeURIComponent(barcode)}`,
        ),

    // --------------------------------------------------
    // Sales
    // --------------------------------------------------

    // ایجاد فروش نقدی یا ایجاد فروش در انتظار پرداخت کارتخوان
    createSale: (body: PosCreateSaleRequest) =>
        api.post<PosSale>('/pos/sales', body),

    // فهرست فروش‌های اخیر
    listSales: (skip = 0, take = 10) =>
        api.get<PosSale[]>('/pos/sales', {
            params: { skip, take },
        }),

    // دریافت جزئیات یک فاکتور
    getSale: (saleId: string) =>
        api.get<PosSale>(
            `/pos/sales/${encodeURIComponent(saleId)}`,
        ),
    // بازیابی اقلام فاکتور لغوشده با قیمت و موجودی فعلی
    restoreCancelledSaleItems: (saleId: string) =>
        api.post<PosRestoreCartLine[]>(
            `/pos/sales/${encodeURIComponent(saleId)}/restore-items`,
        ),

    // --------------------------------------------------
    // POS Bridge - Seller endpoints
    // --------------------------------------------------

    // دریافت پرداخت باز فروشنده جاری
    // اگر پرداخت بازی وجود نداشته باشد، سرور null برمی‌گرداند.
    getOpenPaymentStatus: () =>
        api.get<PosSalePaymentStatus | null>(
            '/pos/bridge/sales/open',
        ),

    // دریافت وضعیت یک پرداخت مشخص
    getPaymentStatus: (saleId: string) =>
        api.get<PosSalePaymentStatus>(
            `/pos/bridge/sales/${encodeURIComponent(saleId)}/status`,
        ),

    // --------------------------------------------------
    // POS Bridge - Device endpoints
    // --------------------------------------------------

    // بررسی سلامت ارتباط Bridge
    // این endpoint به هدر X-Nexa-Pos-Bridge نیاز دارد.
    // از این متد در مرورگر صندوق استفاده نکن؛ مخصوص برنامه Bridge است.
    getBridgeHealth: (bridgeToken: string) =>
        api.get<PosBridgeHealth>('/pos/bridge/health', {
            headers: {
                'X-Nexa-Pos-Bridge': bridgeToken,
            },
        }),

    // دریافت پرداخت بعدی توسط Bridge
    // فقط برنامه Bridge باید این endpoint را فراخوانی کند.
    getNextBridgePayment: (bridgeToken: string) =>
        api.get<PosBridgePaymentWorkItem | null>(
            '/pos/bridge/next',
            {
                headers: {
                    'X-Nexa-Pos-Bridge': bridgeToken,
                },
            },
        ),

    // اعلام نتیجه پرداخت توسط Bridge
    // فقط برنامه Bridge باید این endpoint را فراخوانی کند.
    completeBridgePayment: (
        bridgeToken: string,
        saleId: string,
        body: PosBridgeCompletionRequest,
    ) =>
        api.post<PosBridgeCompletionResponse>(
            `/pos/bridge/${encodeURIComponent(saleId)}/complete`,
            body,
            {
                headers: {
                    'X-Nexa-Pos-Bridge': bridgeToken,
                },
            },
        ),
};
