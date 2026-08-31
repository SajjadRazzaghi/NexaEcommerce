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