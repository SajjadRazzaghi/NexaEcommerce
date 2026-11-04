
export function formatMoney(
    amount: number,
    currency: string = 'IRT',
    locale: string = 'fa-IR',
): string {
    const safeLocale =
        typeof locale === 'string' && locale.trim()
            ? locale
            : 'fa-IR';

    const formattedAmount =
        new Intl.NumberFormat(safeLocale, {
            maximumFractionDigits: 0,
        }).format(amount);

    const normalizedCurrency =
        typeof currency === 'string'
            ? currency.trim().toUpperCase()
            : 'IRT';

    const currencyLabels: Record<string, string> = {
        IRT: 'تومان',
        IRR: 'ریال',
        USD: 'دلار',
        EUR: 'یورو',
    };

    const displayCurrency =
        currencyLabels[normalizedCurrency] ??
        currency;

    return `${formattedAmount} ${displayCurrency}`;
}
