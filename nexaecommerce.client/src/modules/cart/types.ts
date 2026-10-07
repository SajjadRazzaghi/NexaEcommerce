export interface AddCartItemRequest {
    productVariantId: string;
    quantity: number;
}

export interface SetCartItemQuantityRequest {
    productVariantId: string;
    quantity: number;
}

export interface CartItemAttribute {
    attributeValueId: string;

    productAttributeId: string;

    catalogAttributeId?: string | null;

    catalogAttributeValueId?: string | null;

    attributeCode: string;

    attributeName: string;

    roleValue: number;

    value: string;

    displayValue?: string | null;

    colorHex?: string | null;
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

    attributes: CartItemAttribute[];
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