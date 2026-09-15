import {
    useEffect,
} from 'react';

import {
    useQuery,
} from '@tanstack/react-query';

import {
    appearanceApi,
} from '@/lib/api/appearance';

const DEFAULT_TITLE =
    'NexaECommerce';

const DEFAULT_THEME_COLOR =
    '#863bff';

const FAVICON_LINK_ID =
    'store-dynamic-favicon';

const THEME_COLOR_META_ID =
    'store-theme-color';

const DESCRIPTION_META_ID =
    'store-meta-description';

function normalizeStoreName(
    value: string | null | undefined,
): string {
    const normalized =
        value?.trim();

    if (
        !normalized ||
        normalized.toLowerCase() ===
            'default'
    ) {
        return DEFAULT_TITLE;
    }

    return normalized;
}

function isValidColor(
    value: string | null | undefined,
): boolean {
    if (
        !value ||
        value.length > 64
    ) {
        return false;
    }

    try {
        return (
            typeof CSS === 'undefined' ||
            CSS.supports(
                'color',
                value,
            )
        );
    } catch {
        return false;
    }
}

function upsertMeta(
    id: string,
    name: string,
    content: string,
): HTMLMetaElement {
    let meta =
        document.getElementById(
            id,
        ) as HTMLMetaElement | null;

    if (!meta) {
        meta =
            document.createElement(
                'meta',
            );

        meta.id =
            id;

        meta.name =
            name;

        document.head.appendChild(
            meta,
        );
    }

    meta.content =
        content;

    return meta;
}

function getFaviconType(
    href: string,
): string {
    const clean =
        href
            .split('?')[0]
            .split('#')[0]
            .toLowerCase();

    if (
        clean.endsWith('.svg')
    ) {
        return 'image/svg+xml';
    }

    if (
        clean.endsWith('.webp')
    ) {
        return 'image/webp';
    }

    if (
        clean.endsWith('.jpg') ||
        clean.endsWith('.jpeg')
    ) {
        return 'image/jpeg';
    }

    if (
        clean.endsWith('.gif')
    ) {
        return 'image/gif';
    }

    return 'image/png';
}

function getVersionedFaviconUrl(
    href: string,
): string {
    const separator =
        href.includes('?')
            ? '&'
            : '?';

    return (
        `${ href }${ separator } ` +
        `v = ${ encodeURIComponent(href) } `
    );
}

function upsertFavicon(
    href: string,
): HTMLLinkElement {
    let link =
        document.getElementById(
            FAVICON_LINK_ID,
        ) as HTMLLinkElement | null;

    if (!link) {
        link =
            document.createElement(
                'link',
            );

        link.id =
            FAVICON_LINK_ID;

        link.rel =
            'icon';

        document.head.appendChild(
            link,
        );
    }

    link.type =
        getFaviconType(
            href,
        );

    link.href =
        getVersionedFaviconUrl(
            href,
        );

    return link;
}

function removeFavicon(): void {
    const link =
        document.getElementById(
            FAVICON_LINK_ID,
        );

    link?.remove();
}

export function StoreBrandMeta() {
    const {
        data,
    } = useQuery({
        queryKey: [
            'appearance',
        ],

        queryFn:
            appearanceApi.get,

        staleTime:
            5 * 60_000,

        retry: 1,
    });

    useEffect(() => {
        const storeName =
            normalizeStoreName(
                data?.storeName,
            );

        const pageTitle =
            data?.seoTitle?.trim() ||
            storeName;

        const brandColor =
            isValidColor(
                data?.brandColor,
            )
                ? data.brandColor!
                : DEFAULT_THEME_COLOR;

        const faviconUrl =
            data?.faviconUrl?.trim() ||
            null;

        const description =
            data?.seoDescription?.trim() ||
            '';

        document.title =
            pageTitle;

        upsertMeta(
            THEME_COLOR_META_ID,
            'theme-color',
            brandColor,
        );

        upsertMeta(
            DESCRIPTION_META_ID,
            'description',
            description,
        );

        if (faviconUrl) {
            upsertFavicon(
                faviconUrl,
            );
        } else {
            removeFavicon();
        }

        return () => {
            document.title =
                DEFAULT_TITLE;
        };
    }, [
        data?.storeName,
        data?.seoTitle,
        data?.seoDescription,
        data?.brandColor,
        data?.faviconUrl,
    ]);

    return null;
}
