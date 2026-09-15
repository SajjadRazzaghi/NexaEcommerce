import {
    api,
} from './client';

/**
 * Permission required to manage store appearance.
 */
export const APPEARANCE_PERM = {
    manage: 'appearance.manage',
} as const;

export interface Appearance {
    storeName: string | null;
    logoUrl: string | null;
    faviconUrl: string | null;

    theme: string | null;
    brandColor: string | null;
    customTheme: string | null;

    contactPhone: string | null;
    contactEmail: string | null;
    contactAddress: string | null;

    websiteUrl: string | null;

    seoTitle: string | null;
    seoDescription: string | null;
}

export type AppearanceUpdate = {
    storeName?: string | null;
    logoUrl?: string | null;
    faviconUrl?: string | null;

    theme?: string | null;
    brandColor?: string | null;
    customTheme?: string | null;

    contactPhone?: string | null;
    contactEmail?: string | null;
    contactAddress?: string | null;

    websiteUrl?: string | null;

    seoTitle?: string | null;
    seoDescription?: string | null;
};

export const appearanceApi = {
    get: () =>
        api.get<Appearance>(
            '/appearance/',
        ),

    update: (
        body: AppearanceUpdate,
    ) =>
        api.put<Appearance>(
            '/appearance/',
            body,
        ),
};
