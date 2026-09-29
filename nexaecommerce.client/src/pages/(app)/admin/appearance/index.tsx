import {
    useMemo,
    useState,
    type FormEvent,
} from 'react';

import {
    useTranslation,
} from 'react-i18next';

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

type AppearanceResponse =
    Awaited<
        ReturnType<
            typeof appearanceApi.get
        >
    >;

type AppearanceFormState = {
    storeName: string;
    logoUrl: string;
    faviconUrl: string;
    theme: string;
    brandColor: string;
    customTheme: string;
    contactPhone: string;
    contactEmail: string;
    contactAddress: string;
    websiteUrl: string;
    seoTitle: string;
    seoDescription: string;
};

function createAppearanceFormState(
    appearance:
        | AppearanceResponse
        | undefined,
): AppearanceFormState {
    return {
        storeName:
            appearance?.storeName ??
            '',

        logoUrl:
            appearance?.logoUrl ??
            '',

        faviconUrl:
            appearance?.faviconUrl ??
            '',

        theme:
            appearance?.theme ??
            defaultTheme,

        brandColor:
            appearance?.brandColor ??
            '',

        customTheme:
            appearance?.customTheme ??
            '',

        contactPhone:
            appearance?.contactPhone ??
            '',

        contactEmail:
            appearance?.contactEmail ??
            '',

        contactAddress:
            appearance?.contactAddress ??
            '',

        websiteUrl:
            appearance?.websiteUrl ??
            '',

        seoTitle:
            appearance?.seoTitle ??
            '',

        seoDescription:
            appearance?.seoDescription ??
            '',
    };
}

export default function AppearancePage() {
    const {
        t,
    } = useTranslation();

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

    const appearance =
        appearanceQuery.data;

    /*
     * draft === null
     *   => نمایش مستقیم دادهٔ سرور
     *
     * draft !== null
     *   => کاربر در حال ویرایش است و
     *      تغییرات محلی باید حفظ شوند.
     *
     * این ساختار نیاز به useEffect و setState
     * همزمان با render ندارد.
     */
    const [draft, setDraft] =
        useState<
            AppearanceFormState | null
        >(null);

    const form =
        draft ??
        createAppearanceFormState(
            appearance,
        );

    const updateField = <
        K extends keyof AppearanceFormState,
    >(
        field: K,
        value: AppearanceFormState[K],
    ) => {
        setDraft(current => ({
            ...(current ??
                createAppearanceFormState(
                    appearance,
                )),

            [field]:
                value,
        }));
    };

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

                    /*
                     * بعد از ذخیره، draft پاک می‌شود
                     * تا فرم دوباره از دادهٔ canonical
                     * React Query تغذیه شود.
                     */
                    setDraft(null);

                    await queryClient.invalidateQueries(
                        {
                            queryKey:
                                appearanceQueryKey,
                        },
                    );

                    toast.success(
                        t(
                            'appearance.toast.saved',
                        ),
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
                            : t(
                                'appearance.toast.saveError',
                            ),
                    );
                },
        });

    const selectedTheme =
        useMemo(
            () =>
                THEMES.find(
                    item =>
                        item.key ===
                        form.theme,
                ) ??
                THEMES[0],

            [
                form.theme,
            ],
        );

    const previewName =
        form.storeName.trim() ||
        'NexaECommerce';

    const previewLogo =
        form.logoUrl.trim() ||
        null;

    const previewFavicon =
        form.faviconUrl.trim() ||
        null;

    const previewColor =
        form.brandColor.trim() ||
        selectedTheme?.swatch ||
        '#111827';

    const selectedThemeName =
        t(
            `appearance.theme.names.${
    selectedTheme?.key ??
        defaultTheme
} `,
            {
                defaultValue:
                    selectedTheme?.name ??
                    t(
                        'appearance.theme.names.default',
                    ),
            },
        );

    const handleSubmit = (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const normalizedStoreName =
            form.storeName.trim();

        const normalizedLogoUrl =
            form.logoUrl.trim();

        const normalizedFaviconUrl =
            form.faviconUrl.trim();

        const normalizedTheme =
            form.theme.trim() ||
            defaultTheme;

        const normalizedBrandColor =
            form.brandColor.trim();

        const normalizedCustomTheme =
            form.customTheme.trim();

        const normalizedContactPhone =
            form.contactPhone.trim();

        const normalizedContactEmail =
            form.contactEmail.trim();

        const normalizedContactAddress =
            form.contactAddress.trim();

        const normalizedWebsiteUrl =
            form.websiteUrl.trim();

        const normalizedSeoTitle =
            form.seoTitle.trim();

        const normalizedSeoDescription =
            form.seoDescription.trim();

        if (
            normalizedStoreName.length >
            128
        ) {
            toast.error(
                t(
                    'appearance.validation.storeNameMax',
                ),
            );

            return;
        }

        if (
            normalizedLogoUrl.length >
            2048
        ) {
            toast.error(
                t(
                    'appearance.validation.logoUrlMax',
                ),
            );

            return;
        }

        if (
            normalizedFaviconUrl.length >
            2048
        ) {
            toast.error(
                t(
                    'appearance.validation.faviconUrlMax',
                ),
            );

            return;
        }

        if (
            normalizedBrandColor.length >
            64
        ) {
            toast.error(
                t(
                    'appearance.validation.brandColorMax',
                ),
            );

            return;
        }

        if (
            normalizedCustomTheme.length >
            4000
        ) {
            toast.error(
                t(
                    'appearance.validation.customThemeMax',
                ),
            );

            return;
        }

        if (
            normalizedContactPhone.length >
            64
        ) {
            toast.error(
                t(
                    'appearance.validation.contactPhoneMax',
                ),
            );

            return;
        }

        if (
            normalizedContactEmail.length >
            256
        ) {
            toast.error(
                t(
                    'appearance.validation.contactEmailMax',
                ),
            );

            return;
        }

        if (
            normalizedContactAddress.length >
            1000
        ) {
            toast.error(
                t(
                    'appearance.validation.contactAddressMax',
                ),
            );

            return;
        }

        if (
            normalizedWebsiteUrl.length >
            2048
        ) {
            toast.error(
                t(
                    'appearance.validation.websiteUrlMax',
                ),
            );

            return;
        }

        if (
            normalizedSeoTitle.length >
            160
        ) {
            toast.error(
                t(
                    'appearance.validation.seoTitleMax',
                ),
            );

            return;
        }

        if (
            normalizedSeoDescription.length >
            320
        ) {
            toast.error(
                t(
                    'appearance.validation.seoDescriptionMax',
                ),
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
                            {
                                t(
                                    'appearance.loadError',
                                )
                            }
                        </p>

                        <Button
                            type="button"
                            variant="outline"
                            className="mt-4"
                            onClick={() =>
                                void appearanceQuery.refetch()
                            }
                        >
                            {
                                t(
                                    'appearance.retry',
                                )
                            }
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
                    {
                        t(
                            'appearance.title',
                        )
                    }
                </h1>

                <p className="mt-1 text-muted-foreground">
                    {
                        t(
                            'appearance.description',
                        )
                    }
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

                                {
                                    t(
                                        'appearance.identity.title',
                                    )
                                }
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid gap-6">
                            <div className="grid gap-2">
                                <label
                                    htmlFor="store-name"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.identity.storeName',
                                        )
                                    }
                                </label>

                                <Input
                                    id="store-name"
                                    value={
                                        form.storeName
                                    }
                                    onChange={
                                        event =>
                                            updateField(
                                                'storeName',
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={128}
                                    placeholder={
                                        t(
                                            'appearance.identity.storeNamePlaceholder',
                                        )
                                    }
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.identity.storeNameHint',
                                        )
                                    }
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label className="text-sm font-medium">
                                    {
                                        t(
                                            'appearance.identity.logo',
                                        )
                                    }
                                </label>

                                <FileUpload
                                    value={
                                        form.logoUrl
                                    }
                                    onChange={
                                        value =>
                                            updateField(
                                                'logoUrl',
                                                value,
                                            )
                                    }
                                    onRemove={() =>
                                        updateField(
                                            'logoUrl',
                                            '',
                                        )
                                    }
                                    accept="image/jpeg,image/png,image/webp,image/gif,image/svg+xml"
                                    maxSize={5}
                                    placeholder={
                                        t(
                                            'appearance.identity.logoUpload',
                                        )
                                    }
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.identity.logoHint',
                                        )
                                    }
                                </p>

                                <div className="grid gap-2">
                                    <label
                                        htmlFor="logo-url"
                                        className="text-xs font-medium text-muted-foreground"
                                    >
                                        {
                                            t(
                                                'appearance.identity.logoUrl',
                                            )
                                        }
                                    </label>

                                    <Input
                                        id="logo-url"
                                        value={
                                            form.logoUrl
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'logoUrl',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={2048}
                                        placeholder={
                                            t(
                                                'appearance.identity.logoUrlPlaceholder',
                                            )
                                        }
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label className="text-sm font-medium">
                                    {
                                        t(
                                            'appearance.identity.favicon',
                                        )
                                    }
                                </label>

                                <FileUpload
                                    value={
                                        form.faviconUrl
                                    }
                                    onChange={
                                        value =>
                                            updateField(
                                                'faviconUrl',
                                                value,
                                            )
                                    }
                                    onRemove={() =>
                                        updateField(
                                            'faviconUrl',
                                            '',
                                        )
                                    }
                                    accept="image/png,image/jpeg,image/webp,image/gif,image/svg+xml"
                                    maxSize={2}
                                    placeholder={
                                        t(
                                            'appearance.identity.faviconUpload',
                                        )
                                    }
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.identity.faviconHint',
                                        )
                                    }
                                </p>

                                <div className="grid gap-2">
                                    <label
                                        htmlFor="favicon-url"
                                        className="text-xs font-medium text-muted-foreground"
                                    >
                                        {
                                            t(
                                                'appearance.identity.faviconUrl',
                                            )
                                        }
                                    </label>

                                    <Input
                                        id="favicon-url"
                                        value={
                                            form.faviconUrl
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'faviconUrl',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={2048}
                                        placeholder={
                                            t(
                                                'appearance.identity.faviconUrlPlaceholder',
                                            )
                                        }
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Phone className="size-5" />

                                {
                                    t(
                                        'appearance.contact.title',
                                    )
                                }
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid min-w-0 gap-5">
                            <div className="grid gap-2">
                                <label
                                    htmlFor="contact-phone"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.contact.phone',
                                        )
                                    }
                                </label>

                                <div className="relative">
                                    <Phone className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                                    <Input
                                        id="contact-phone"
                                        value={
                                            form.contactPhone
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'contactPhone',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={64}
                                        className="pl-9"
                                        placeholder={
                                            t(
                                                'appearance.contact.phonePlaceholder',
                                            )
                                        }
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="contact-email"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.contact.email',
                                        )
                                    }
                                </label>

                                <div className="relative">
                                    <Mail className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                                    <Input
                                        id="contact-email"
                                        type="email"
                                        value={
                                            form.contactEmail
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'contactEmail',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={256}
                                        className="pl-9"
                                        placeholder={
                                            t(
                                                'appearance.contact.emailPlaceholder',
                                            )
                                        }
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="website-url"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.contact.website',
                                        )
                                    }
                                </label>

                                <div className="relative">
                                    <Globe className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                                    <Input
                                        id="website-url"
                                        type="url"
                                        value={
                                            form.websiteUrl
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'websiteUrl',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={2048}
                                        className="pl-9"
                                        placeholder={
                                            t(
                                                'appearance.contact.websitePlaceholder',
                                            )
                                        }
                                    />
                                </div>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="contact-address"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.contact.address',
                                        )
                                    }
                                </label>

                                <textarea
                                    id="contact-address"
                                    value={
                                        form.contactAddress
                                    }
                                    onChange={
                                        event =>
                                            updateField(
                                                'contactAddress',
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={1000}
                                    rows={4}
                                    placeholder={
                                        t(
                                            'appearance.contact.addressPlaceholder',
                                        )
                                    }
                                    className="border-input bg-background min-h-24 rounded-md border px-3 py-2 text-sm outline-none focus:ring-2"
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.contact.addressHint',
                                        )
                                    }
                                </p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Search className="size-5" />

                                {
                                    t(
                                        'appearance.seo.title',
                                    )
                                }
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid min-w-0 gap-5">
                            <div className="grid gap-2">
                                <label
                                    htmlFor="seo-title"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.seo.seoTitle',
                                        )
                                    }
                                </label>

                                <Input
                                    id="seo-title"
                                    value={
                                        form.seoTitle
                                    }
                                    onChange={
                                        event =>
                                            updateField(
                                                'seoTitle',
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={160}
                                    placeholder={
                                        t(
                                            'appearance.seo.seoTitlePlaceholder',
                                        )
                                    }
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.seo.seoTitleHint',
                                        )
                                    }
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="seo-description"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.seo.description',
                                        )
                                    }
                                </label>

                                <textarea
                                    id="seo-description"
                                    value={
                                        form.seoDescription
                                    }
                                    onChange={
                                        event =>
                                            updateField(
                                                'seoDescription',
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={320}
                                    rows={5}
                                    placeholder={
                                        t(
                                            'appearance.seo.descriptionPlaceholder',
                                        )
                                    }
                                    className="border-input bg-background min-h-28 rounded-md border px-3 py-2 text-sm outline-none focus:ring-2"
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.seo.descriptionHint',
                                        )
                                    }
                                </p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Palette className="size-5" />

                                {
                                    t(
                                        'appearance.theme.title',
                                    )
                                }
                            </CardTitle>
                        </CardHeader>

                        <CardContent className="grid min-w-0 gap-5">
                            <div className="grid gap-3">
                                <label
                                    htmlFor="theme"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.theme.baseTheme',
                                        )
                                    }
                                </label>

                                <select
                                    id="theme"
                                    value={
                                        form.theme
                                    }
                                    onChange={
                                        event =>
                                            updateField(
                                                'theme',
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
                                                    t(
                                                        `appearance.theme.names.${ item.key } `,
                                                        {
                                                            defaultValue:
                                                                item.name,
                                                        },
                                                    )
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
                                    {
                                        t(
                                            'appearance.theme.brandColor',
                                        )
                                    }
                                </label>

                                <div className="flex gap-3">
                                    <input
                                        id="brand-color-picker"
                                        type="color"
                                        value={
                                            /^#[0-9a-fA-F]{6}$/.test(
                                                form.brandColor,
                                            )
                                                ? form.brandColor
                                                : '#111827'
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'brandColor',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        className="h-10 w-14 cursor-pointer rounded-md border bg-background p-1 disabled:cursor-not-allowed"
                                        aria-label={
                                            t(
                                                'appearance.theme.pickBrandColor',
                                            )
                                        }
                                    />

                                    <Input
                                        id="brand-color"
                                        value={
                                            form.brandColor
                                        }
                                        onChange={
                                            event =>
                                                updateField(
                                                    'brandColor',
                                                    event.target.value,
                                                )
                                        }
                                        disabled={
                                            !canUpdate ||
                                            save.isPending
                                        }
                                        maxLength={64}
                                        placeholder={
                                            t(
                                                'appearance.theme.brandColorPlaceholder',
                                            )
                                        }
                                    />
                                </div>

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.theme.brandColorHint',
                                        )
                                    }
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <label
                                    htmlFor="custom-theme"
                                    className="text-sm font-medium"
                                >
                                    {
                                        t(
                                            'appearance.theme.customTheme',
                                        )
                                    }
                                </label>

                                <textarea
                                    id="custom-theme"
                                    value={
                                        form.customTheme
                                    }
                                    onChange={
                                        event =>
                                            updateField(
                                                'customTheme',
                                                event.target.value,
                                            )
                                    }
                                    disabled={
                                        !canUpdate ||
                                        save.isPending
                                    }
                                    maxLength={4000}
                                    rows={8}
                                    placeholder={
                                        t(
                                            'appearance.theme.customThemePlaceholder',
                                        )
                                    }
                                    className="border-input bg-background min-h-40 rounded-md border px-3 py-2 font-mono text-xs outline-none focus:ring-2"
                                />

                                <p className="text-xs text-muted-foreground">
                                    {
                                        t(
                                            'appearance.theme.customThemeHint',
                                        )
                                    }
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

                            {
                                t(
                                    save.isPending
                                        ? 'appearance.actions.saving'
                                        : 'appearance.actions.save',
                                )
                            }
                        </Button>
                    </div>
                </div>

             
                <Card className="h-fit min-w-0 overflow-hidden lg:sticky lg:top-6">
                    <CardHeader>
                        <CardTitle>
                            {
                                t(
                                    'appearance.preview.title',
                                )
                            }
                        </CardTitle>
                    </CardHeader>

                    <CardContent className="grid min-w-0 gap-5">
                        <div className="min-w-0 overflow-hidden rounded-2xl border p-5">
                            <div className="flex min-w-0 items-center gap-3">
                                {previewLogo ? (
                                    <img
                                        src={previewLogo}
                                        alt=""
                                        className="size-12 shrink-0 rounded-xl border object-cover"
                                    />
                                ) : (
                                    <div
                                        className="grid size-12 shrink-0 place-items-center rounded-xl text-white"
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
                                        {previewName}
                                    </div>

                                    <div className="truncate text-xs text-muted-foreground">
                                        {selectedThemeName}
                                    </div>
                                </div>
                            </div>

                            <div
                                className="mt-5 h-2 w-full rounded-full"
                                style={{
                                    backgroundColor:
                                        previewColor,
                                }}
                            />

                            <div className="mt-5 grid min-w-0 gap-3">
                                <div className="h-4 w-3/4 rounded bg-muted" />
                                <div className="h-4 w-full rounded bg-muted" />
                                <div className="h-10 w-full rounded-lg bg-muted" />
                            </div>
                        </div>

                        <div className="min-w-0 overflow-hidden rounded-xl border p-4">
                            <div className="mb-3 flex min-w-0 items-center gap-2 text-sm font-medium">
                                <Image className="size-4 shrink-0" />

                                {
                                    t(
                                        'appearance.preview.browserIdentity',
                                    )
                                }
                            </div>

                            <div className="flex min-w-0 items-center gap-3">
                                {previewFavicon ? (
                                    <img
                                        src={previewFavicon}
                                        alt=""
                                        className="size-10 shrink-0 rounded-lg border bg-background object-contain p-1"
                                    />
                                ) : (
                                    <div
                                        className="grid size-10 shrink-0 place-items-center rounded-lg text-white"
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
                                            form.seoTitle.trim() ||
                                            previewName
                                        }
                                    </div>

                                    <div className="truncate text-xs text-muted-foreground">
                                        {
                                            previewFavicon ||
                                            t(
                                                'appearance.preview.defaultFavicon',
                                            )
                                        }
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="min-w-0 overflow-hidden rounded-xl border border-dashed p-4 text-sm text-muted-foreground">
                            {
                                t(
                                    'appearance.preview.tenantNote',
                                )
                            }
                        </div>

                        <div className="min-w-0 overflow-hidden rounded-xl border p-4">
                            <div className="mb-3 text-sm font-medium">
                                {
                                    t(
                                        'appearance.preview.contact',
                                    )
                                }
                            </div>

                            <div className="grid min-w-0 gap-2 text-sm text-muted-foreground">
                                {form.contactPhone.trim() && (
                                    <div className="flex min-w-0 items-center gap-2">
                                        <Phone className="size-4 shrink-0" />

                                        <span className="min-w-0 truncate">
                                            {
                                                form.contactPhone.trim()
                                            }
                                        </span>
                                    </div>
                                )}

                                {form.contactEmail.trim() && (
                                    <div className="flex min-w-0 items-center gap-2">
                                        <Mail className="size-4 shrink-0" />

                                        <span className="min-w-0 truncate">
                                            {
                                                form.contactEmail.trim()
                                            }
                                        </span>
                                    </div>
                                )}

                                {form.websiteUrl.trim() && (
                                    <div className="flex min-w-0 items-center gap-2">
                                        <Globe className="size-4 shrink-0" />

                                        <span className="min-w-0 truncate">
                                            {
                                                form.websiteUrl.trim()
                                            }
                                        </span>
                                    </div>
                                )}

                                {form.contactAddress.trim() && (
                                    <p className="min-w-0 break-words leading-6">
                                        {
                                            form.contactAddress.trim()
                                        }
                                    </p>
                                )}

                                {!form.contactPhone.trim() &&
                                    !form.contactEmail.trim() &&
                                    !form.websiteUrl.trim() &&
                                    !form.contactAddress.trim() && (
                                        <span className="min-w-0 break-words">
                                            {
                                                t(
                                                    'appearance.preview.noContact',
                                                )
                                            }
                                        </span>
                                    )}
                            </div>
                        </div>

                        <div className="min-w-0 overflow-hidden rounded-xl border p-4">
                            <div className="mb-3 flex items-center gap-2 text-sm font-medium">
                                <Search className="size-4 shrink-0" />

                                {
                                    t(
                                        'appearance.preview.seo',
                                    )
                                }
                            </div>

                            <div className="grid min-w-0 gap-2">
                                <div className="truncate font-medium">
                                    {
                                        form.seoTitle.trim() ||
                                        previewName
                                    }
                                </div>

                                <p className="line-clamp-3 break-words text-sm text-muted-foreground">
                                    {
                                        form.seoDescription.trim() ||
                                        t(
                                            'appearance.preview.noSeoDescription',
                                        )
                                    }
                                </p>

                                {form.websiteUrl.trim() && (
                                    <div className="truncate text-xs text-muted-foreground">
                                        {
                                            form.websiteUrl.trim()
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

