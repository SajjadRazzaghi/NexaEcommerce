export interface AddCartItemRequest {
    productVariantId: string;
    quantity: number;
}

export interface SetCartItemQuantityRequest {
    productVariantId: string;
    quantity: number;
}

export interface CartItem {
    productVariantId: string;
    sku: string;
    productName: string;
    unitPrice: number;
    quantity: number;
    lineTotal: number;
    availableStock: number;
    imageUrl?: string | null;
}

export interface CartResponse {
    id: string;
    userId?: string | null;
    guestToken?: string | null;
    items: CartItem[];
    subtotal: number;
    totalAmount: number;
    currency: string;
}