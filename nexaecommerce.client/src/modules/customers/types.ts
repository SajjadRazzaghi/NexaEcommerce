export interface CustomerAddress {
    id: string;
    title: string;
    recipientName: string;
    phoneNumber: string;
    country: string;
    province: string;
    city: string;
    addressLine: string;
    postalCode?: string | null;
    isDefault: boolean;
}

export interface CreateAddressRequest {
    title: string;
    recipientName: string;
    phoneNumber: string;
    country: string;
    province: string;
    city: string;
    addressLine: string;
    postalCode?: string | null;
    isDefault?: boolean;
}

export interface UpdateAddressRequest {
    title: string;
    recipientName: string;
    phoneNumber: string;
    country: string;
    province: string;
    city: string;
    addressLine: string;
    postalCode?: string | null;
}
export interface AdminCustomer {
    id: string;
    displayName: string | null;
    email: string | null;
    phoneNumber: string | null;
    emailConfirmed: boolean;
    phoneNumberConfirmed: boolean;
    createdAt: string;
    totalOrders: number;
    paidOrders: number;
    totalSpent: number;
    lastOrderAt: string | null;
}