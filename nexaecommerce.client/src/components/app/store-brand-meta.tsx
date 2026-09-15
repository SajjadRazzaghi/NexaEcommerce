import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';

import { appearanceApi } from '@/lib/api/appearance';

const DEFAULT_TITLE = 'NexaECommerce';
const DEFAULT_THEME_COLOR = '#863bff';
const FAVICON_LINK_ID = 'store-dynamic-favicon';
const THEME_COLOR_META_ID = 'store-theme-color';

function isValidColor(value: string | null | undefined): boolean {
    if (!value || value.length > 64) {
        return false;
    }

    try {
        return typeof CSS === 'undefined' || CSS.supports('color', value);
    } catch {
        return false;
    }
}

function upsertMeta(
    id: string,
    name: string,
    content: string,
): HTMLMetaElement {
    let meta = document.getElementById(id) as HTMLMetaElement | null;

    if (!meta) {
        meta = document.createElement('meta');
        meta.id = id;
        meta.name = name;
        document.head.appendChild(meta);
    }

    meta.content = content;
    return meta;
}

function upsertFavicon(href: string): HTMLLinkElement {
    let link = document.getElementById(
        FAVICON_LINK_ID,
    ) as HTMLLinkElement | null;

    if (!link) {
        link = document.createElement('link');
        link.id = FAVICON_LINK_ID;
        link.rel = 'icon';
        document.head.appendChild(link);
    }

    link.type = 'image/png';
    link.href = href;

    return link;
}

export function StoreBrandMeta() {
    const { data } = useQuery({
        queryKey: ['appearance'],
        queryFn: appearanceApi.get,
        staleTime: 5 * 60_000,
        retry: 1,
    });

    useEffect(() => {
        const storeName = data?.storeName?.trim() || DEFAULT_TITLE;
        const brandColor = isValidColor(data?.brandColor)
            ? data?.brandColor
            : DEFAULT_THEME_COLOR;
        const logoUrl = data?.logoUrl?.trim() || null;

        document.title = storeName;

        upsertMeta(
            THEME_COLOR_META_ID,
            'theme-color',
            brandColor,
        );

        if (logoUrl) {
            upsertFavicon(logoUrl);
        }

        return () => {
            document.title = DEFAULT_TITLE;
        };
    }, [data?.storeName, data?.brandColor, data?.logoUrl]);

    return null;
}
