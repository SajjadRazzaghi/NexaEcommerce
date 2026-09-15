import { api } from './client';

/** Permission gating appearance/branding changes. */
export const APPEARANCE_PERM = {
    manage: 'appearance.manage',
} as const;

export interface Appearance {
    /** Active tenant display name. */
    storeName: string | null;

    /** Active tenant logo URL. */
    logoUrl: string | null;

    /** Curated theme key. */
    theme: string | null;

    /** Explicit brand accent color. */
    brandColor: string | null;

    /** Custom palette JSON. */
    customTheme: string | null;
}

export type AppearanceUpdate = {
    storeName?: string | null;
    logoUrl?: string | null;
    theme?: string | null;
    brandColor?: string | null;
    customTheme?: string | null;
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