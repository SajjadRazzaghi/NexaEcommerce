import {
    useEffect,
    useMemo,
    useState,
} from 'react';

import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    Globe,
    Image,
    Mail,
    Palette,
    Phone,
    Save,
    Search,
    Store,
} from 'lucide-react';

import {
    toast,
} from 'sonner';

import {
    usePermission,
} from '@/hooks/use-permission';

import {
    appearanceApi,
    APPEARANCE_PERM,
} from '@/lib/api/appearance';

import {
    THEMES,
} from '@/lib/themes';

import {
    isApiError,
} from '@/lib/problem';

import {
    FileUpload,
} from '@/components/ui/file-upload';

import {
    Button,
} from '@/components/ui/button';

import {
    Card,
    CardContent,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import {
    Input,
} from '@/components/ui/input';

const appearanceQueryKey = [
    'appearance',
];

const defaultTheme =
    'default';

export default function AppearancePage() {
    const queryClient =
        useQueryClient();

    const canUpdate =
        usePermission(
            APPEARANCE_PERM.manage,
        );

    const appearanceQuery =
        useQuery({
            queryKey:
                appearanceQueryKey,

            queryFn:
                appearanceApi.get,

            staleTime:
                0,
        });

    const [storeName, setStoreName] =
        useState('');

    const [logoUrl, setLogoUrl] =
        useState('');

    const [faviconUrl, setFaviconUrl] =
        useState('');

    const [theme, setTheme] =
        useState(defaultTheme);

    const [brandColor, setBrandColor] =
        useState('');

    const [customTheme, setCustomTheme] =
        useState('');

    const [contactPhone, setContactPhone] =
        useState('');

    const [contactEmail, setContactEmail] =
        useState('');

    const [contactAddress, setContactAddress] =
        useState('');

    const [websiteUrl, setWebsiteUrl] =
        useState('');

    const [seoTitle, setSeoTitle] =
        useState('');

    const [seoDescription, setSeoDescription] =
        useState('');

    const appearance =
        appearanceQuery.data;

    useEffect(() => {
        if (!appearance) {
            return;
        }

        setStoreName(
            appearance.storeName ??
            '',
        );

        setLogoUrl(
            appearance.logoUrl ??
            '',
        );

        setFaviconUrl(
            appearance.faviconUrl ??
            '',
        );

        setTheme(
            appearance.theme ??
            defaultTheme,
        );

        setBrandColor(
            appearance.brandColor ??
            '',
        );

        setCustomTheme(
            appearance.customTheme ??
            '',
        );

        setContactPhone(
            appearance.contactPhone ??
            '',
        );

        setContactEmail(
            appearance.contactEmail ??
            '',
        );

        setContactAddress(
            appearance.contactAddress ??
            '',
        );

        setWebsiteUrl(
            appearance.websiteUrl ??
            '',
        );

        setSeoTitle(
            appearance.seoTitle ??
            '',
        );

        setSeoDescription(
            appearance.seoDescription ??
            '',
        );
    }, [appearance]);

    const save =
        useMutation({
            mutationFn:
                appearanceApi.update,

            onSuccess:
                async result => {
                    queryClient.setQueryData(
                        appearanceQueryKey,
                        result,
                    );

                    await queryClient.invalidateQueries(
                        {
                            queryKey:
                                appearanceQueryKey,
                        },
                    );

                    toast.success(
                        'Brand settings saved successfully.',
                    );
                },

            onError:
                error => {
                    toast.error(
                        isApiError(error)
                            ? (
                                error.problem.detail ??
                                error.message
                            )
                            : 'Unable to save brand settings.',
                    );
                },
        });

    const selectedTheme =
        useMemo(
            () =>
                THEMES.find(
                    item =>
                        item.key ===
                        theme,
                ) ??
                THEMES[0],
            [
                theme,
            ],
        );

    const previewName =
        storeName.trim() ||
        'NexaECommerce';

    const previewLogo =
        logoUrl.trim() ||
        null;

    const previewFavicon =
        faviconUrl.trim() ||
        null;

    const previewColor =
        brandColor.trim() ||
        selectedTheme?.swatch ||
        '#111827';

    const handleSubmit = (
        event:
            React.FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const normalizedStoreName =
            storeName.trim();

        const normalizedLogoUrl =
            logoUrl.trim();

        const normalizedFaviconUrl =
            faviconUrl.trim();

        const normalizedTheme =
            theme.trim() ||
            defaultTheme;

        const normalizedBrandColor =
            brandColor.trim();

        const normalizedCustomTheme =
            customTheme.trim();

        const normalizedContactPhone =
            contactPhone.trim();

        const normalizedContactEmail =
            contactEmail.trim();

        const normalizedContactAddress =
            contactAddress.trim();

        const normalizedWebsiteUrl =
            websiteUrl.trim();

        const normalizedSeoTitle =
            seoTitle.trim();

        const normalizedSeoDescription =
            seoDescription.trim();

        if (
            normalizedStoreName.length >
            128
        ) {
            toast.error(
                'Store name cannot exceed 128 characters.',
            );

            return;
        }

        if (
            normalizedLogoUrl.length >
            2048
        ) {
            toast.error(
                'Logo URL cannot exceed 2048 characters.',
            );

            return;
        }

        if (
            normalizedFaviconUrl.length >
            2048
        ) {
            toast.error(
                'Favicon URL cannot exceed 2048 characters.',
            );

            return;
        }

        if (
            normalizedBrandColor.length >
            64
        ) {
            toast.error(
                'Brand color cannot exceed 64 characters.',
            );

            return;
        }

        if (
            normalizedCustomTheme.length >
            4000
        ) {
            toast.error(
                'Custom theme is too large.',
            );

            return;
        }

        if (
            normalizedContactPhone.length >
            64
        ) {
            toast.error(
                'Contact phone cannot exceed 64 characters.',
            );

            return;
        }

        if (
            normalizedContactEmail.length >
            256
        ) {
            toast.error(
                'Contact email cannot exceed 256 characters.',
            );

            return;
        }

        if (
            normalizedContactAddress.length >
            1000
        ) {
            toast.error(
                'Contact address cannot exceed 1000 characters.',
            );

            return;
        }

        if (
            normalizedWebsiteUrl.length >
            2048
        ) {
            toast.error(
                'Website URL cannot exceed 2048 characters.',
            );

            return;
        }

        if (
            normalizedSeoTitle.length >
            160
        ) {
            toast.error(
                'SEO title cannot exceed 160 characters.',
            );

            return;
        }

        if (
            normalizedSeoDescription.length >
            320
        ) {
            toast.error(
                'SEO description cannot exceed 320 characters.',
            );

            return;
        }

        save.mutate(
            {
                storeName:
                    normalizedStoreName,

                logoUrl:
                    normalizedLogoUrl,

                faviconUrl:
                    normalizedFaviconUrl,

                theme:
                    normalizedTheme,

                brandColor:
                    normalizedBrandColor,

                customTheme:
                    normalizedCustomTheme,

                contactPhone:
                    normalizedContactPhone,

                contactEmail:
                    normalizedContactEmail,

                contactAddress:
                    normalizedContactAddress,

                websiteUrl:
                    normalizedWebsiteUrl,

                seoTitle:
                    normalizedSeoTitle,

                seoDescription:
                    normalizedSeoDescription,
            },
        );
    };

    if (
        appearanceQuery.isLoading
    ) {
        return (
            <div className="grid gap-4">
                <Card>
                    <CardContent className="p-8">
                        <div className="animate-pulse space-y-4">
                            <div className="h-6 w-56 rounded bg-muted" />
                            <div className="h-10 rounded bg-muted" />
                            <div className="h-10 rounded bg-muted" />
                            <div className="h-10 rounded bg-muted" />
                            <div className="h-10 rounded bg-muted" />
                        </div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    if (
        appearanceQuery.isError
    ) {
        return (
            <div className="grid gap-4">
                <Card>
                    <CardContent className="p-8">
                        <p className="text-sm text-destructive">
                            Unable to load store branding.
                        </p>

                        <Button
                            type="button"
                            variant="outline"
                            className="mt-4"
                            onClick={() =>
                                void appearanceQuery.refetch()
                            }
                        >
                            Retry
                        </Button>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="grid gap-5">
            <header>
                <h1 className="text-2xl font-semibold tracking-tight">
                    Store Branding
                </h1>

                <p className="mt-1 text-muted-foreground">
                    Configure the store identity, logo, favicon, contact information, SEO metadata and visual theme used across the storefront.
                </p>
            </header>

            <form
                onSubmit={
                    handleSubmit
                }
                className="grid gap-5 lg:grid-cols-[1fr_360px]"
            >
                <div className="grid gap-5">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Store className="size-5" />
                                Store identity
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid gap-6">
                            <div className="grid gap-2">
                                <label
                                    htmlFor="store-name"
                                    className="text-sm font-medium"
                                >
                                    Store name
                                </label>

                                <Input
                                    id="store-name"
                                    value={
                                        storeName
                                    }
                                    onChange={
                                        event =>
                                            setStoreName(
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={128}
                                    placeholder="My Store"
                                />

                                <p className="text-xs text-muted-foreground">
                                    This is the public name shown across the storefront.
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label className="text-sm font-medium">
                                    Store logo
                                </label>

                                <FileUpload
                                    value={logoUrl}
                                    onChange={setLogoUrl}
                                    onRemove={() =>
                                        setLogoUrl('')
                                    }
                                    accept="image/jpeg,image/png,image/webp,image/gif,image/svg+xml"
                                    maxSize={5}
                                    placeholder="Upload store logo"
                                />

                                <p className="text-xs text-muted-foreground">
                                    Recommended for the storefront header, navigation and footer.
                                    Maximum size: 5 MB.
                                </p>

                                <div className="grid gap-2">
                                    <label
                                        htmlFor="logo-url"
                                        className="text-xs font-medium text-muted-foreground"
                                    >
                                        Or enter an existing logo URL
                                    </label>

                                    <Input
                                        id="logo-url"
                                        value={
                                            logoUrl
                                        }
                                        onChange={
                                            event =>
                                                setLogoUrl(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={2048}
                                        placeholder="/uploads/logo.png"
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label className="text-sm font-medium">
                                    Favicon
                                </label>

                                <FileUpload
                                    value={faviconUrl}
                                    onChange={setFaviconUrl}
                                    onRemove={() =>
                                        setFaviconUrl('')
                                    }
                                    accept="image/png,image/jpeg,image/webp,image/gif,image/svg+xml"
                                    maxSize={2}
                                    placeholder="Upload favicon"
                                />

                                <p className="text-xs text-muted-foreground">
                                    Used for the browser tab, bookmarks and other browser UI.
                                    A square PNG, WEBP or SVG is recommended. Maximum size: 2 MB.
                                </p>

                                <div className="grid gap-2">
                                    <label
                                        htmlFor="favicon-url"
                                        className="text-xs font-medium text-muted-foreground"
                                    >
                                        Or enter an existing favicon URL
                                    </label>

                                    <Input
                                        id="favicon-url"
                                        value={
                                            faviconUrl
                                        }
                                        onChange={
                                            event =>
                                                setFaviconUrl(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={2048}
                                        placeholder="/uploads/favicon.png"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Phone className="size-5" />
                                Contact information
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid gap-5">
                            <div className="grid gap-2">
                                <label
                                    htmlFor="contact-phone"
                                    className="text-sm font-medium"
                                >
                                    Contact phone
                                </label>

                                <div className="relative">
                                    <Phone className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                                    <Input
                                        id="contact-phone"
                                        value={
                                            contactPhone
                                        }
                                        onChange={
                                            event =>
                                                setContactPhone(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={64}
                                        className="pl-9"
                                        placeholder="+98 21 12345678"
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="contact-email"
                                    className="text-sm font-medium"
                                >
                                    Contact email
                                </label>

                                <div className="relative">
                                    <Mail className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                                    <Input
                                        id="contact-email"
                                        type="email"
                                        value={
                                            contactEmail
                                        }
                                        onChange={
                                            event =>
                                                setContactEmail(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={256}
                                        className="pl-9"
                                        placeholder="info@example.com"
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="website-url"
                                    className="text-sm font-medium"
                                >
                                    Website URL
                                </label>

                                <div className="relative">
                                    <Globe className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                                    <Input
                                        id="website-url"
                                        type="url"
                                        value={
                                            websiteUrl
                                        }
                                        onChange={
                                            event =>
                                                setWebsiteUrl(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={2048}
                                        className="pl-9"
                                        placeholder="https://example.com"
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="contact-address"
                                    className="text-sm font-medium"
                                >
                                    Contact address
                                </label>

                                <textarea
                                    id="contact-address"
                                    value={
                                        contactAddress
                                    }
                                    onChange={
                                        event =>
                                            setContactAddress(
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={1000}
                                    rows={4}
                                    placeholder="Store address"
                                    className="border-input bg-background min-h-24 rounded-md border px-3 py-2 text-sm outline-none focus:ring-2"
                                />

                                <p className="text-xs text-muted-foreground">
                                    This information can be displayed in the storefront footer and contact sections.
                                </p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Search className="size-5" />
                                SEO settings
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid gap-5">
                            <div className="grid gap-2">
                                <label
                                    htmlFor="seo-title"
                                    className="text-sm font-medium"
                                >
                                    SEO title
                                </label>

                                <Input
                                    id="seo-title"
                                    value={
                                        seoTitle
                                    }
                                    onChange={
                                        event =>
                                            setSeoTitle(
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={160}
                                    placeholder="My Store | Online Shopping"
                                />

                                <p className="text-xs text-muted-foreground">
                                    Used as the browser page title and search-engine title.
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="seo-description"
                                    className="text-sm font-medium"
                                >
                                    SEO description
                                </label>

                                <textarea
                                    id="seo-description"
                                    value={
                                        seoDescription
                                    }
                                    onChange={
                                        event =>
                                            setSeoDescription(
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={320}
                                    rows={5}
                                    placeholder="Describe your store and products for search engines."
                                    className="border-input bg-background min-h-28 rounded-md border px-3 py-2 text-sm outline-none focus:ring-2"
                                />

                                <p className="text-xs text-muted-foreground">
                                    Used for the page meta description.
                                </p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Palette className="size-5" />
                                Theme
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid gap-5">
                            <div className="grid gap-3">
                                <label
                                    htmlFor="theme"
                                    className="text-sm font-medium"
                                >
                                    Base theme
                                </label>

                                <select
                                    id="theme"
                                    value={
                                        theme
                                    }
                                    onChange={
                                        event =>
                                            setTheme(
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    className="border-input bg-background h-10 rounded-md border px-3 text-sm"
                                >
                                    {THEMES.map(
                                        item => (
                                            <option
                                                key={
                                                    item.key
                                                }
                                                value={
                                                    item.key
                                                }
                                            >
                                                {
                                                    item.name
                                                }
                                            </option>
                                        ),
                                    )}
                                </select>
                            </div>

                            <div className="grid gap-3">
                                <label
                                    htmlFor="brand-color"
                                    className="text-sm font-medium"
                                >
                                    Brand color
                                </label>

                                <div className="flex gap-3">
                                    <input
                                        id="brand-color-picker"
                                        type="color"
                                        value={
                                            /^#[0-9a-fA-F]{6}$/.test(
                                                brandColor,
                                            )
                                                ? brandColor
                                                : '#111827'
                                        }
                                        onChange={
                                            event =>
                                                setBrandColor(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        className="h-10 w-14 cursor-pointer rounded-md border bg-background p-1 disabled:cursor-not-allowed"
                                        aria-label="Pick brand color"
                                    />

                                    <Input
                                        id="brand-color"
                                        value={
                                            brandColor
                                        }
                                        onChange={
                                            event =>
                                                setBrandColor(
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={64}
                                        placeholder="#2563eb"
                                    />
                                </div>

                                <p className="text-xs text-muted-foreground">
                                    Leave empty to use the selected theme accent.
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="custom-theme"
                                    className="text-sm font-medium"
                                >
                                    Custom theme JSON
                                </label>

                                <textarea
                                    id="custom-theme"
                                    value={
                                        customTheme
                                    }
                                    onChange={
                                        event =>
                                            setCustomTheme(
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={4000}
                                    rows={8}
                                    placeholder='{"light":{...},"dark":{...}}'
                                    className="border-input bg-background min-h-40 rounded-md border px-3 py-2 font-mono text-xs outline-none focus:ring-2"
                                />

                                <p className="text-xs text-muted-foreground">
                                    Used only when the selected theme is custom.
                                </p>
                            </div>
                        </CardContent>
                    </Card>

                    <div className="flex items-center justify-end">
                        <Button
                            type="submit"
                            disabled={
                                !canUpdate ||
                                save.isPending
                            }
                        >
                            <Save className="size-4" />

                            {save.isPending
                                ? 'Saving...'
                                : 'Save changes'}
                        </Button>
                    </div>
                </div>

                <Card className="h-fit lg:sticky lg:top-6">
                    <CardHeader>
                        <CardTitle>
                            Live preview
                        </CardTitle>
                    </CardHeader>

                    <CardContent className="grid gap-5">
                        <div className="rounded-2xl border p-5">
                            <div className="flex items-center gap-3">
                                {previewLogo ? (
                                    <img
                                        src={
                                            previewLogo
                                        }
                                        alt=""
                                        className="size-12 rounded-xl border object-cover"
                                    />
                                ) : (
                                    <div
                                        className="grid size-12 place-items-center rounded-xl text-white"
                                        style={{
                                            backgroundColor:
                                                previewColor,
                                        }}
                                    >
                                        <Image className="size-5" />
                                    </div>
                                )}

                                <div className="min-w-0">
                                    <div className="truncate font-semibold">
                                        {
                                            previewName
                                        }
                                    </div>

                                    <div className="text-xs text-muted-foreground">
                                        {
                                            selectedTheme?.name ??
                                            'Default'
                                        }
                                    </div>
                                </div>
                            </div>

                            <div
                                className="mt-5 h-2 rounded-full"
                                style={{
                                    backgroundColor:
                                        previewColor,
                                }}
                            />

                            <div className="mt-5 grid gap-3">
                                <div className="h-4 w-3/4 rounded bg-muted" />
                                <div className="h-4 w-full rounded bg-muted" />
                                <div className="h-10 rounded-lg bg-muted" />
                            </div>
                        </div>

                        <div className="rounded-xl border p-4">
                            <div className="mb-3 flex items-center gap-2 text-sm font-medium">
                                <Image className="size-4" />
                                Browser identity
                            </div>

                            <div className="flex items-center gap-3">
                                {previewFavicon ? (
                                    <img
                                        src={
                                            previewFavicon
                                        }
                                        alt=""
                                        className="size-10 rounded-lg border bg-background object-contain p-1"
                                    />
                                ) : (
                                    <div
                                        className="grid size-10 place-items-center rounded-lg text-white"
                                        style={{
                                            backgroundColor:
                                                previewColor,
                                        }}
                                    >
                                        <Image className="size-4" />
                                    </div>
                                )}

                                <div className="min-w-0">
                                    <div className="truncate text-sm font-medium">
                                        {
                                            seoTitle.trim() ||
                                            previewName
                                        }
                                    </div>

                                    <div className="truncate text-xs text-muted-foreground">
                                        {
                                            previewFavicon ||
                                            'Default favicon'
                                        }
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="rounded-xl border border-dashed p-4 text-sm text-muted-foreground">
                            Changes are stored per tenant and are loaded dynamically by the storefront.
                        </div>

                        <div className="rounded-xl border p-4">
                            <div className="mb-3 text-sm font-medium">
                                Contact preview
                            </div>

                            <div className="grid gap-2 text-sm text-muted-foreground">
                                {contactPhone.trim() && (
                                    <div className="flex items-center gap-2">
                                        <Phone className="size-4 shrink-0" />

                                        <span className="truncate">
                                            {
                                                contactPhone.trim()
                                            }
                                        </span>
                                    </div>
                                )}

                                {contactEmail.trim() && (
                                    <div className="flex items-center gap-2">
                                        <Mail className="size-4 shrink-0" />

                                        <span className="truncate">
                                            {
                                                contactEmail.trim()
                                            }
                                        </span>
                                    </div>
                                )}

                                {websiteUrl.trim() && (
                                    <div className="flex items-center gap-2">
                                        <Globe className="size-4 shrink-0" />

                                        <span className="truncate">
                                            {
                                                websiteUrl.trim()
                                            }
                                        </span>
                                    </div>
                                )}

                                {contactAddress.trim() && (
                                    <p className="leading-6">
                                        {
                                            contactAddress.trim()
                                        }
                                    </p>
                                )}

                                {!contactPhone.trim() &&
                                    !contactEmail.trim() &&
                                    !websiteUrl.trim() &&
                                    !contactAddress.trim() && (
                                        <span>
                                            No contact information configured.
                                        </span>
                                    )}
                            </div>
                        </div>

                        <div className="rounded-xl border p-4">
                            <div className="mb-3 flex items-center gap-2 text-sm font-medium">
                                <Search className="size-4" />
                                SEO preview
                            </div>

                            <div className="grid gap-2">
                                <div className="truncate font-medium">
                                    {
                                        seoTitle.trim() ||
                                        previewName
                                    }
                                </div>

                                <p className="line-clamp-3 text-sm text-muted-foreground">
                                    {
                                        seoDescription.trim() ||
                                        'No SEO description configured.'
                                    }
                                </p>

                                {websiteUrl.trim() && (
                                    <div className="truncate text-xs text-muted-foreground">
                                        {
                                            websiteUrl.trim()
                                        }
                                    </div>
                                )}
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </form>
        </div>
    );
}
