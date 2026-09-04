export interface CheckoutLine {
    productVariantId: string;
    quantity: number;
}

export interface CheckoutRequest {
    items: CheckoutLine[];
    shippingFullName: string;
    shippingPhone: string;
    shippingAddress: string;
    shippingCity: string;
    shippingPostalCode?: string | null;
    shippingMethodId: string;
    couponCode?: string | null;
}

export interface OrderItemDto {
    productVariantId: string;
    sku: string;
    productName: string;
    unitPrice: number;
    quantity: number;
    lineTotal: number;
}

export type OrderStatus =
    | 'PendingPayment'
    | 'Paid'
    | 'Processing'
    | 'Shipped'
    | 'Delivered'
    | 'Cancelled';

export interface OrderDto {
    id: string;
    orderNumber: string;
    status: OrderStatus;
    currency: string;

    subtotal: number;
    shippingAmount: number;
    discountAmount: number;
    couponCode?: string | null;
    totalAmount: number;

    shippingFullName: string;
    shippingPhone: string;
    shippingAddress: string;
    shippingCity: string;
    shippingPostalCode?: string | null;

    items: OrderItemDto[];
}

export interface OrderListItemDto {
    id: string;
    orderNumber: string;
    userId: string;
    status: OrderStatus;
    currency: string;
    totalAmount: number;
    itemCount: number;
    createdAt: string;
}

export interface OrderListDto {
    items: OrderListItemDto[];
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
}

export interface PaymentAttemptDto {
    id: string;
    orderId: string;
    status: string;
    amount: number;
    currency: string;
    gatewayName?: string | null;
    gatewayReference?: string | null;
    failureCode?: string | null;
    failureMessage?: string | null;
    createdAt: string;
    completedAt?: string | null;
}

export type ShipmentStatus =
    | 'Pending'
    | 'Shipped'
    | 'Delivered'
    | 'Cancelled';

export interface ShipmentDto {
    id: string;
    orderId: string;
    shippingMethod: string;
    carrier: string;
    trackingNumber?: string | null;
    status: ShipmentStatus;
    createdAt: string;
    shippedAt?: string | null;
    deliveredAt?: string | null;
}
