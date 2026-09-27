export function formatMoney(
    amount: number,
    currency: string = 'IRR',
    locale: string = 'en-US',
): string {
    const safeLocale =
        typeof locale === 'string' && locale.trim()
            ? locale
            : 'en-US';

    const formattedAmount = new Intl.NumberFormat(
        safeLocale,
        {
            maximumFractionDigits: 0,
        },
    ).format(amount);

    const normalizedCurrency =
        typeof currency === 'string'
            ? currency.toUpperCase()
            : 'IRR';

    let displayCurrency = currency;

    if (normalizedCurrency === 'IRR') {
        displayCurrency = safeLocale
            .toLowerCase()
            .startsWith('fa')
            ? 'ریال'
            : 'IRR';
    }

    return `${ formattedAmount } ${ displayCurrency } `;
}

