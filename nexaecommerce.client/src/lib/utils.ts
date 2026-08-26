// src/lib/utils.ts

import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * ترکیب کلاس‌های Tailwind
 */
export function cn(...inputs: ClassValue[]) {
    return twMerge(clsx(inputs));
}

/**
 * فرمت قیمت
 */
export function formatPrice(
    price: number | null | undefined,
    currency: string = 'تومان'
): string {
    if (price === null || price === undefined) {
        return '-';
    }

    return `${new Intl.NumberFormat('fa-IR').format(price)} ${currency}`;
}

/**
 * فرمت کوتاه تاریخ
 */
export function formatDateShort(
    date: string | Date | null | undefined
): string {
    if (!date) return '—';

    const d = typeof date === 'string'
        ? new Date(date)
        : date;

    if (isNaN(d.getTime())) {
        return '—';
    }

    return new Intl.DateTimeFormat('fa-IR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
    }).format(d);
}

/**
 * فرمت کامل تاریخ
 */
export function formatDate(
    date: string | Date | null | undefined
): string {
    if (!date) return '—';

    const d = typeof date === 'string'
        ? new Date(date)
        : date;

    if (isNaN(d.getTime())) {
        return '—';
    }

    return new Intl.DateTimeFormat('fa-IR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
    }).format(d);
}

/**
 * کوتاه کردن متن
 */
export function truncate(
    text: string | null | undefined,
    length: number = 50
): string {
    if (!text) return '';

    if (text.length <= length) {
        return text;
    }

    return `${text.slice(0, length)}...`;
}