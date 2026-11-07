import { Heart, HeartOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import ProductCard from '../components/ProductCard';
import { useFavorites } from '../hooks/useFavorites';

export default function FavoritesPage() {
    const { t, i18n } = useTranslation();
    const isFa = i18n.language?.toLowerCase().startsWith('fa') ?? false;
    const { items, isLoading, isError, refetch } = useFavorites();

    const title = t('storefront.favorites.title', {
        defaultValue: isFa ? 'علاقه‌مندی‌های من' : 'My favorites',
    });

    if (isLoading) {
        return (
            <div className="mx-auto grid max-w-7xl gap-6 px-4 py-8 sm:px-6">
                <div className="h-8 w-48 animate-pulse rounded-lg bg-muted" />
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
                    {Array.from({ length: 5 }).map((_, index) => (
                        <div key={index} className="aspect-[3/5] animate-pulse rounded-2xl bg-muted" />
                    ))}
                </div>
            </div>
        );
    }

    if (isError) {
        return (
            <div className="mx-auto max-w-3xl px-4 py-16 text-center">
                <Heart className="mx-auto size-10 text-muted-foreground" />
                <h1 className="mt-4 text-xl font-black">{title}</h1>
                <p className="mt-2 text-sm text-muted-foreground">
                    {isFa ? 'دریافت علاقه‌مندی‌ها ناموفق بود.' : 'Could not load your favorites.'}
                </p>
                <button onClick={() => void refetch()} className="mt-5 rounded-xl bg-primary px-5 py-2.5 text-sm font-bold text-primary-foreground">
                    {isFa ? 'تلاش دوباره' : 'Try again'}
                </button>
            </div>
        );
    }

    return (
        <div className="mx-auto grid max-w-7xl gap-6 px-4 py-8 sm:px-6">
            <header className="flex items-center gap-3">
                <span className="flex size-11 items-center justify-center rounded-2xl bg-primary/10 text-primary">
                    <Heart className="size-5" />
                </span>
                <div>
                    <h1 className="text-2xl font-black tracking-tight">{title}</h1>
                    <p className="mt-1 text-sm text-muted-foreground">
                        {isFa
                            ? `${items.length.toLocaleString('fa-IR')} محصول ذخیره‌شده`
                            : `${items.length.toLocaleString()} saved products`}
                    </p>
                </div>
            </header>

            {items.length === 0 ? (
                <div className="rounded-3xl border border-dashed px-6 py-16 text-center">
                    <HeartOff className="mx-auto size-12 text-muted-foreground" />
                    <h2 className="mt-4 text-lg font-bold">
                        {isFa ? 'هنوز محصولی را لایک نکرده‌اید' : 'You have no favorites yet'}
                    </h2>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {isFa
                            ? 'روی علامت قلب لباس‌های موردعلاقه‌تان بزنید تا اینجا ذخیره شوند.'
                            : 'Tap the heart on a product to save it here.'}
                    </p>
                    <a href="/products" className="mt-5 inline-flex rounded-xl bg-primary px-5 py-2.5 text-sm font-bold text-primary-foreground">
                        {isFa ? 'مشاهده محصولات' : 'Browse products'}
                    </a>
                </div>
            ) : (
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4 xl:grid-cols-5">
                    {items.map((product) => (
                        <ProductCard key={product.id} product={product} />
                    ))}
                </div>
            )}
        </div>
    );
}
