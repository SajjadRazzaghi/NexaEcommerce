import { useState, type MouseEvent } from 'react';
import { Heart } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useFavorites } from '../hooks/useFavorites';

type FavoriteButtonProps = {
    productId: string;
    variant?: 'floating' | 'inline';
    showLabel?: boolean;
};

export default function FavoriteButton({
    productId,
    variant = 'floating',
    showLabel = false,
}: FavoriteButtonProps) {
    const { i18n } = useTranslation();
    const isFa = i18n.language?.toLowerCase().startsWith('fa') ?? false;
    const { ids, toggleFavorite, isLoading } = useFavorites();
    const [busy, setBusy] = useState(false);
    const [failed, setFailed] = useState(false);

    const isFavorite = ids.has(productId);

    const handleClick = async (event: MouseEvent<HTMLButtonElement>) => {
        event.preventDefault();
        event.stopPropagation();

        if (busy) return;

        setBusy(true);
        setFailed(false);
        try {
            await toggleFavorite(productId, isFavorite);
        } catch {
            setFailed(true);
        } finally {
            setBusy(false);
        }
    };

    const label = isFavorite
        ? (isFa ? 'حذف از علاقه‌مندی‌ها' : 'Remove from favorites')
        : (isFa ? 'افزودن به علاقه‌مندی‌ها' : 'Add to favorites');

    const floatingClasses = [
        'pointer-events-auto absolute start-3 bottom-3 z-20 flex size-10 items-center justify-center rounded-full shadow-md backdrop-blur transition',
        isFavorite
            ? 'bg-red-50 text-red-600'
            : 'bg-card/95 text-muted-foreground hover:bg-card hover:text-red-500',
    ].join(' ');

    const inlineClasses = [
        'pointer-events-auto inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border px-3 py-2 text-sm font-semibold transition',
        isFavorite
            ? 'border-red-200 bg-red-50 text-red-600'
            : 'border-border bg-card text-foreground hover:border-primary/40 hover:bg-primary/5 hover:text-primary',
    ].join(' ');

    return (
        <button
            type="button"
            onClick={handleClick}
            disabled={busy || isLoading}
            aria-label={label}
            aria-pressed={isFavorite}
            title={failed ? (isFa ? 'ذخیره علاقه‌مندی انجام نشد' : 'Could not update favorites') : label}
            className={variant === 'floating' ? floatingClasses : inlineClasses}
        >
            <Heart
                className={variant === 'floating' ? 'size-5' : 'size-4'}
                fill={isFavorite ? 'currentColor' : 'none'}
            />
            {showLabel && (
                <span>
                    {isFavorite
                        ? (isFa ? 'ذخیره‌شده' : 'Saved')
                        : (isFa ? 'افزودن به علاقه‌مندی‌ها' : 'Save to favorites')}
                </span>
            )}
            {busy && <span className="sr-only">{isFa ? 'در حال ذخیره' : 'Saving'}</span>}
        </button>
    );
}
