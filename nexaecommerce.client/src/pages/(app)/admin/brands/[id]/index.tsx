import {
    useNavigate,
    useParams,
} from 'react-router';
import {
    ArrowLeft,
    Pencil,
    Globe,
    Star,
    Power,
    PowerOff,
    Trash2,
} from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

import {
    PageHeader,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
    useBrand,
    useBrandAction,
} from '@/modules/catalog/brands/hooks';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function BrandDetailsPage() {
    const { t, i18n } =
        useTranslation();

    const { id } =
        useParams<{
            id: string;
        }>();

    const navigate =
        useNavigate();

    const query =
        useBrand(id);

    const activate =
        useBrandAction('activate');

    const deactivate =
        useBrandAction('deactivate');

    const publish =
        useBrandAction('publish');

    const unpublish =
        useBrandAction('unpublish');

    const feature =
        useBrandAction('feature');

    const unfeature =
        useBrandAction('unfeature');

    const remove =
        useBrandAction('remove');

    const canUpdate =
        usePermission(
            PERM.brandsUpdate,
        );

    const canDelete =
        usePermission(
            PERM.brandsDelete,
        );

    const canStatus =
        usePermission(
            PERM.brandsStatus,
        );

    const canPublish =
        usePermission(
            PERM.brandsPublish,
        );

    const canFeature =
        usePermission(
            PERM.brandsFeature,
        );

    useDocumentTitle(
        query.data?.name ??
        t(
            'catalogBrands.detailsTitle',
        ),
    );

    if (query.isLoading) {
        return (
            <LoadingSkeleton
                variant="cards"
                rows={3}
            />
        );
    }

    if (
        query.isError ||
        !query.data
    ) {
        return (
            <ErrorState
                error={query.error}
                onRetry={() =>
                    query.refetch()
                }
                message={t(
                    'catalogBrands.loadError',
                )}
            />
        );
    }

    const brand = query.data;

    const run = async (
        promise: Promise<unknown>,
        message: string,
    ) => {
        try {
            await promise;
            toast.success(message);
            await query.refetch();
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : t(
                        'catalogBrands.actionFailed',
                    ),
            );
        }
    };

    const locale =
        i18n.resolvedLanguage ||
        i18n.language ||
        'en';

    const formatDate =
        (
            value: string,
        ) =>
            new Intl.DateTimeFormat(
                locale,
                {
                    dateStyle:
                        'medium',
                    timeStyle:
                        'short',
                },
            ).format(
                new Date(value),
            );

    return (
        <div className="space-y-6">
            <PageHeader
                title={brand.name}
                description={
                    brand.slug
                }
                actions={
                    <div className="flex gap-2">
                        <Button
                            variant="outline"
                            onClick={() =>
                                navigate(
                                    '/admin/brands',
                                )
                            }
                        >
                            <ArrowLeft />
                            {t(
                                'catalogBrands.backToBrands',
                            )}
                        </Button>

                        {canUpdate && (
                            <Button
                                onClick={() =>
                                    navigate(
                                        `/admin/brands/${brand.id}/edit`,
                                    )
                                }
                            >
                                <Pencil />
                                {t(
                                    'catalogBrands.actions.edit',
                                )}
                            </Button>
                        )}
                    </div>
                }
            />

            <div className="grid gap-6 lg:grid-cols-[1.4fr_0.6fr]">
                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogBrands.details.information',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'catalogBrands.details.informationDescription',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-6">
                        <div className="flex flex-wrap gap-2">
                            <Badge
                                variant={
                                    brand.isActive
                                        ? 'default'
                                        : 'secondary'
                                }
                            >
                                {brand.isActive
                                    ? t(
                                        'catalogBrands.status.active',
                                    )
                                    : t(
                                        'catalogBrands.status.inactive',
                                    )}
                            </Badge>

                            <Badge
                                variant={
                                    brand.isPublished
                                        ? 'default'
                                        : 'outline'
                                }
                            >
                                {brand.isPublished
                                    ? t(
                                        'catalogBrands.status.published',
                                    )
                                    : t(
                                        'catalogBrands.status.draft',
                                    )}
                            </Badge>

                            <Badge
                                variant={
                                    brand.isFeatured
                                        ? 'default'
                                        : 'outline'
                                }
                            >
                                {brand.isFeatured
                                    ? t(
                                        'catalogBrands.status.featured',
                                    )
                                    : t(
                                        'catalogBrands.status.normal',
                                    )}
                            </Badge>
                        </div>

                        <dl className="grid gap-4 sm:grid-cols-2">
                            <Info
                                label={t(
                                    'catalogForms.name',
                                )}
                                value={
                                    brand.name
                                }
                            />

                            <Info
                                label={t(
                                    'catalogForms.slug',
                                )}
                                value={
                                    brand.slug
                                }
                            />

                            <Info
                                label={t(
                                    'catalogForms.website',
                                )}
                                value={
                                    brand.website
                                }
                                href={
                                    brand.website
                                }
                            />

                            <Info
                                label={t(
                                    'catalogForms.displayOrder',
                                )}
                                value={String(
                                    brand.displayOrder,
                                )}
                            />

                            <Info
                                label={t(
                                    'catalogForms.seoTitle',
                                )}
                                value={
                                    brand.seoTitle
                                }
                            />

                            <Info
                                label={t(
                                    'catalogForms.seoDescription',
                                )}
                                value={
                                    brand.seoDescription
                                }
                            />

                            <Info
                                label={t(
                                    'catalogForms.seoKeywords',
                                )}
                                value={
                                    brand.seoKeywords
                                }
                            />

                            <Info
                                label={t(
                                    'catalogBrands.details.created',
                                )}
                                value={formatDate(
                                    brand.createdAt,
                                )}
                            />

                            <Info
                                label={t(
                                    'catalogBrands.details.updated',
                                )}
                                value={
                                    brand.updatedAt
                                        ? formatDate(
                                            brand.updatedAt,
                                        )
                                        : '—'
                                }
                            />
                        </dl>

                        <div>
                            <h3 className="font-medium">
                                {t(
                                    'catalogForms.description',
                                )}
                            </h3>

                            <p className="text-muted-foreground mt-2 whitespace-pre-wrap">
                                {brand.description ||
                                    t(
                                        'catalogBrands.details.noDescription',
                                    )}
                            </p>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogBrands.details.actions',
                            )}
                        </CardTitle>
                    </CardHeader>

                    <CardContent className="space-y-2">
                        {canStatus &&
                            (brand.isActive ? (
                                <Button
                                    className="w-full justify-start"
                                    variant="outline"
                                    onClick={() =>
                                        run(
                                            deactivate.mutateAsync(
                                                brand.id,
                                            ),
                                            t(
                                                'catalogBrands.toast.deactivated',
                                            ),
                                        )
                                    }
                                >
                                    <PowerOff />
                                    {t(
                                        'catalogBrands.actions.deactivate',
                                    )}
                                </Button>
                            ) : (
                                <Button
                                    className="w-full justify-start"
                                    variant="outline"
                                    onClick={() =>
                                        run(
                                            activate.mutateAsync(
                                                brand.id,
                                            ),
                                            t(
                                                'catalogBrands.toast.activated',
                                            ),
                                        )
                                    }
                                >
                                    <Power />
                                    {t(
                                        'catalogBrands.actions.activate',
                                    )}
                                </Button>
                            ))}

                        {canPublish &&
                            (brand.isPublished ? (
                                <Button
                                    className="w-full justify-start"
                                    variant="outline"
                                    onClick={() =>
                                        run(
                                            unpublish.mutateAsync(
                                                brand.id,
                                            ),
                                            t(
                                                'catalogBrands.toast.unpublished',
                                            ),
                                        )
                                    }
                                >
                                    <Globe />
                                    {t(
                                        'catalogBrands.actions.unpublish',
                                    )}
                                </Button>
                            ) : (
                                <Button
                                    className="w-full justify-start"
                                    variant="outline"
                                    disabled={
                                        !brand.isActive
                                    }
                                    onClick={() =>
                                        run(
                                            publish.mutateAsync(
                                                brand.id,
                                            ),
                                            t(
                                                'catalogBrands.toast.published',
                                            ),
                                        )
                                    }
                                >
                                    <Globe />
                                    {t(
                                        'catalogBrands.actions.publish',
                                    )}
                                </Button>
                            ))}

                        {canFeature &&
                            (brand.isFeatured ? (
                                <Button
                                    className="w-full justify-start"
                                    variant="outline"
                                    onClick={() =>
                                        run(
                                            unfeature.mutateAsync(
                                                brand.id,
                                            ),
                                            t(
                                                'catalogBrands.toast.unfeatured',
                                            ),
                                        )
                                    }
                                >
                                    <Star />
                                    {t(
                                        'catalogBrands.actions.unfeature',
                                    )}
                                </Button>
                            ) : (
                                <Button
                                    className="w-full justify-start"
                                    variant="outline"
                                    disabled={
                                        !brand.isActive
                                    }
                                    onClick={() =>
                                        run(
                                            feature.mutateAsync(
                                                brand.id,
                                            ),
                                            t(
                                                'catalogBrands.toast.featured',
                                            ),
                                        )
                                    }
                                >
                                    <Star />
                                    {t(
                                        'catalogBrands.actions.feature',
                                    )}
                                </Button>
                            ))}

                        {canDelete && (
                            <Button
                                className="w-full justify-start"
                                variant="destructive"
                                onClick={() =>
                                    run(
                                        remove.mutateAsync(
                                            brand.id,
                                        ),
                                        t(
                                            'catalogBrands.toast.deleted',
                                        ),
                                    ).then(
                                        () =>
                                            navigate(
                                                '/admin/brands',
                                            ),
                                    )
                                }
                            >
                                <Trash2 />
                                {t(
                                    'catalogBrands.actions.delete',
                                )}
                            </Button>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}

function Info({
    label,
    value,
    href,
}: {
    label: string;
    value?: string | null;
    href?: string | null;
}) {
    return (
        <div className="rounded-lg border p-3">
            <dt className="text-muted-foreground text-xs">
                {label}
            </dt>

            <dd className="mt-1 break-words text-sm">
                {href && value ? (
                    <a
                        className="text-primary underline"
                        href={href}
                        target="_blank"
                        rel="noreferrer"
                    >
                        {value}
                    </a>
                ) : (
                    value || '—'
                )}
            </dd>
        </div>
    );
}