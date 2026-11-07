import { useState, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Heart, TrendingDown, TrendingUp } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { favoritesApi } from '../api/favorites';
import { resolveMediaUrl } from '@/lib/media-url';

const PERIODS = [7, 30, 90, 0] as const;

export default function FavoritesReportPage() {
    const { i18n } = useTranslation();
    const isFa = i18n.language?.toLowerCase().startsWith('fa') ?? false;
    const [periodDays, setPeriodDays] = useState<number>(30);

    const report = useQuery({
        queryKey: ['favorites', 'popular', periodDays],
        queryFn: () => favoritesApi.popular(periodDays),
        staleTime: 30_000,
    });

    const number = (value: number) =>
        value.toLocaleString(isFa ? 'fa-IR' : 'en-US');

    const periodLabel = (value: number) => {
        if (value === 0) return isFa ? 'کل دوره' : 'All time';
        return isFa ? `${number(value)} روز اخیر` : `Last ${value} days`;
    };

    if (report.isLoading) {
        return <div className="grid gap-4"><div className="h-8 w-56 animate-pulse rounded bg-muted" /><div className="h-40 animate-pulse rounded-2xl bg-muted" /></div>;
    }

    if (report.isError || !report.data) {
        return (
            <div className="rounded-xl border p-8 text-center">
                <p className="text-sm text-muted-foreground">
                    {isFa ? 'گزارش محبوبیت محصولات دریافت نشد. مجوز و اتصال سرور را بررسی کنید.' : 'Could not load the favorites report. Check access and server connection.'}
                </p>
                <button onClick={() => void report.refetch()} className="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-primary-foreground">
                    {isFa ? 'تلاش دوباره' : 'Try again'}
                </button>
            </div>
        );
    }

    const data = report.data;

    return (
        <div className="grid gap-6" dir={isFa ? 'rtl' : 'ltr'}>
            <header className="flex flex-wrap items-end justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-black">{isFa ? 'محبوب‌ترین محصولات' : 'Most loved products'}</h1>
                    <p className="mt-1 text-sm text-muted-foreground">
                        {isFa ? 'گزارش علاقه‌مندی‌های ثبت‌شده توسط مشتریان' : 'Customer favorite and product popularity report'}
                    </p>
                </div>
                <select
                    value={periodDays}
                    onChange={(event) => setPeriodDays(Number(event.target.value))}
                    className="h-10 rounded-xl border border-border bg-card px-3 text-sm"
                    aria-label={isFa ? 'بازه گزارش' : 'Report period'}
                >
                    {PERIODS.map((period) => (
                        <option key={period} value={period}>{periodLabel(period)}</option>
                    ))}
                </select>
            </header>

            <div className="grid gap-3 sm:grid-cols-3">
                <Metric icon={<Heart className="size-5" />} label={isFa ? 'علاقه‌مندی‌های فعال' : 'Active favorites'} value={data.totalActiveFavorites} />
                <Metric icon={<TrendingUp className="size-5" />} label={isFa ? 'لایک‌های ثبت‌شده در بازه' : 'Favorites added in period'} value={data.totalAddedDuringPeriod} />
                <Metric icon={<TrendingDown className="size-5" />} label={isFa ? 'لایک‌های برداشته‌شده در بازه' : 'Favorites removed in period'} value={data.totalRemovedDuringPeriod} />
            </div>

            <section className="overflow-hidden rounded-2xl border bg-card">
                <div className="border-b px-4 py-3">
                    <h2 className="font-bold">{isFa ? 'رتبه‌بندی محصولات بر اساس لایک فعال' : 'Products ranked by active favorites'}</h2>
                </div>
                {data.items.length === 0 ? (
                    <div className="p-10 text-center text-sm text-muted-foreground">
                        {isFa ? 'هنوز علاقه‌مندی ثبت نشده است.' : 'No favorites have been recorded yet.'}
                    </div>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="w-full min-w-[760px] text-sm">
                            <thead className="bg-muted/50 text-muted-foreground">
                                <tr>
                                    <th className="px-4 py-3 text-start">{isFa ? 'محصول' : 'Product'}</th>
                                    <th className="px-4 py-3 text-center">{isFa ? 'لایک فعال' : 'Active likes'}</th>
                                    <th className="px-4 py-3 text-center">{isFa ? 'ثبت‌شده' : 'Added'}</th>
                                    <th className="px-4 py-3 text-center">{isFa ? 'برداشته‌شده' : 'Removed'}</th>
                                    <th className="px-4 py-3 text-start">SKU</th>
                                </tr>
                            </thead>
                            <tbody>
                                {data.items.map((item, index) => (
                                    <tr key={item.id} className="border-t">
                                        <td className="px-4 py-3">
                                            <div className="flex items-center gap-3">
                                                <span className="w-6 text-center font-bold text-muted-foreground">{number(index + 1)}</span>
                                                {item.mainImage ? (
                                                    <img src={resolveMediaUrl(item.mainImage)} alt={item.name} className="size-12 rounded-lg border object-cover" loading="lazy" />
                                                ) : (
                                                    <span className="size-12 rounded-lg bg-muted" />
                                                )}
                                                <div className="min-w-0">
                                                    <a href={`/products/${encodeURIComponent(item.slug)}`} className="line-clamp-2 font-semibold hover:text-primary">{item.name}</a>
                                                    <p className="mt-1 text-xs text-muted-foreground">{number(item.price)} {item.currency}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3 text-center font-black text-primary">{number(item.activeFavoriteCount)}</td>
                                        <td className="px-4 py-3 text-center text-emerald-700">{number(item.addedDuringPeriod)}</td>
                                        <td className="px-4 py-3 text-center text-muted-foreground">{number(item.removedDuringPeriod)}</td>
                                        <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{item.sku}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
                <div className="border-t px-4 py-3 text-xs text-muted-foreground">
                    {isFa
                        ? 'تعداد لایک فعال شامل علاقه‌مندی‌های فعلی است؛ ستون‌های ثبت و حذف، رویدادهای بازه انتخاب‌شده را نشان می‌دهند.'
                        : 'Active likes show current saved favorites; added and removed columns show events within the selected period.'}
                </div>
            </section>
        </div>
    );
}

function Metric({ icon, label, value }: { icon: ReactNode; label: string; value: number }) {
    return (
        <div className="flex items-center gap-3 rounded-2xl border bg-card p-4">
            <span className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary">{icon}</span>
            <div>
                <p className="text-xs text-muted-foreground">{label}</p>
                <p className="mt-1 text-2xl font-black tabular-nums">{value.toLocaleString()}</p>
            </div>
        </div>
    );
}
