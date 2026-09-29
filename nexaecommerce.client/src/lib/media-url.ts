// nexaecommerce.client/src/lib/media-url.ts

const configuredApiUrl =
    (import.meta.env.VITE_API_URL ?? '').trim();

function getApiOrigin(): string {
    if (!configuredApiUrl) {
        return '';
    }

    try {
        return new URL(
            configuredApiUrl,
            window.location.origin,
        ).origin;
    } catch {
        return '';
    }
}

export function resolveMediaUrl(
    value?: string | null,
): string {
    const url = value?.trim();

    if (!url) {
        return '';
    }

    if (
        url.startsWith('http://') ||
        url.startsWith('https://') ||
        url.startsWith('//') ||
        url.startsWith('data:') ||
        url.startsWith('blob:')
    ) {
        return url;
    }

    const path = url.startsWith('/')
        ? url
        : `/${url}`;

    const apiOrigin = getApiOrigin();

    if (
        apiOrigin &&
        path.startsWith('/uploads/')
    ) {
        return `${apiOrigin}${path}`;
    }

    return path;
}