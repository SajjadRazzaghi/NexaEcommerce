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
    Image,
    Palette,
    Save,
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

const defaultTheme = 'default';

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

    const [theme, setTheme] =
        useState(defaultTheme);

    const [brandColor, setBrandColor] =
        useState('');

    const [customTheme, setCustomTheme] =
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

                    await queryClient.invalidateQueries(
                        {
                            queryKey:
                                [
                                    'appearance',
                                ],
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

        const normalizedTheme =
            theme.trim() ||
            defaultTheme;

        const normalizedBrandColor =
            brandColor.trim();

        const normalizedCustomTheme =
            customTheme.trim();

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

        save.mutate(
            {
                storeName:
                    normalizedStoreName,

                logoUrl:
                    normalizedLogoUrl,

                theme:
                    normalizedTheme,

                brandColor:
                    normalizedBrandColor,

                customTheme:
                    normalizedCustomTheme,
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
                    Configure the store name, logo, brand color and visual theme used across the storefront.
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

                        <CardContent className="grid gap-5">
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
                                    This is the public name shown in the storefront.
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="logo-url"
                                    className="text-sm font-medium"
                                >
                                    Logo URL
                                </label>

                                <div className="flex gap-2">
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
                                        placeholder="https://..."
                                    />
                                </div>

                                <p className="text-xs text-muted-foreground">
                                    HTTP, HTTPS or a root-relative upload path can be used.
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

                        <div className="rounded-xl border border-dashed p-4 text-sm text-muted-foreground">
                            Changes are stored per tenant and are loaded by the storefront dynamically.
                        </div>
                    </CardContent>
                </Card>
            </form>
        </div>
    );
}