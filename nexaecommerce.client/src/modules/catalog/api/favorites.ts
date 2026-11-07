import { api } from '@/lib/api/client';
import type { ProductListItem } from './products';

export type FavoriteProduct = ProductListItem & {
    favoritedAt: string;
};

export type FavoritesResponse = {
    ids: string[];
    items: FavoriteProduct[];
    total: number;
};

export type PopularFavoriteProduct = {
    id: string;
    name: string;
    sku: string;
    slug: string;
    price: number;
    currency: string;
    mainImage?: string | null;
    activeFavoriteCount: number;
    addedDuringPeriod: number;
    removedDuringPeriod: number;
};

export type FavoritesReport = {
    periodDays: number;
    since: string | null;
    generatedAt: string;
    totalActiveFavorites: number;
    totalAddedDuringPeriod: number;
    totalRemovedDuringPeriod: number;
    items: PopularFavoriteProduct[];
};

export const favoritesApi = {
    mine: () => api.get<FavoritesResponse>('/favorites/'),
    add: (productId: string) =>
        api.put<{ productId: string; isFavorite: boolean }>(
            `/favorites/${encodeURIComponent(productId)}`,
            {},
        ),
    remove: (productId: string) =>
        api.del<void>(`/favorites/${encodeURIComponent(productId)}`),
    popular: (periodDays = 30) =>
        api.get<FavoritesReport>('/favorites/admin/popular', {
            params: { periodDays },
        }),
};
