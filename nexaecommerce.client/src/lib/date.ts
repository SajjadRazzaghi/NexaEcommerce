// src/lib/date.ts
export function formatDate(date: string | Date | null | undefined): string {
    if (!date) return '—';

    const d = typeof date === 'string' ? new Date(date) : date;

    if (isNaN(d.getTime())) return '—';

    return new Intl.DateTimeFormat('fa-IR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    }).format(d);
}

export function formatDateShort(date: string | Date | null | undefined): string {
    if (!date) return '—';

    const d = typeof date === 'string' ? new Date(date) : date;

    if (isNaN(d.getTime())) return '—';

    return new Intl.DateTimeFormat('fa-IR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
    }).format(d);
}