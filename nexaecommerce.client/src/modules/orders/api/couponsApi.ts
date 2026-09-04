import api from '@/services/api';

export interface CouponValidationResult {
    code: string;
    isValid: boolean;
    discountAmount: number;
    message?: string | null;
}

export async function validateCoupon(
    code: string,
    orderAmount: number,
): Promise<CouponValidationResult> {
    if (!code.trim()) {
        throw new Error(
            'Coupon code is required.',
        );
    }

    if (orderAmount < 0) {
        throw new Error(
            'Order amount cannot be negative.',
        );
    }

    const { data } =
        await api.get<CouponValidationResult>(
            '/api/coupons/validate',
            {
                params: {
                    code: code.trim(),
                    orderAmount,
                },
            },
        );

    return data;
}
