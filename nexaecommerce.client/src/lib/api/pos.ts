import { api } from '@/lib/api/client';

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

export type PosCreateSaleRequest = {
    idempotencyKey: string;
    paymentMethod: 'Cash' | 'Card';
    paymentReference?: string | null;
    items: { productVariantId: string; quantity: number }[];
};

export const posApi = {
    searchProducts: (search: string, signal?: AbortSignal) =>
        api.get<PosProduct[]>('/pos/products', { params: { search }, signal }),

    findByBarcode: (barcode: string) =>
        api.get<PosVariant>(`/pos/barcodes/${encodeURIComponent(barcode)}`),

    createSale: (body: PosCreateSaleRequest) =>
        api.post<PosSale>('/pos/sales', body),

    listSales: (skip = 0, take = 10) =>
        api.get<PosSale[]>('/pos/sales', { params: { skip, take } }),

    getSale: (id: string) =>
        api.get<PosSale>(`/pos/sales/${encodeURIComponent(id)}`),
};