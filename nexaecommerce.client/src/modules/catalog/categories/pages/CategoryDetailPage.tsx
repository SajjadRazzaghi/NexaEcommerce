import { useNavigate, useParams } from 'react-router-dom';
import {
    ArrowLeft,
    Calendar,
    FolderTree,
    Pencil,
    Search,
    Star,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';

import {
    ErrorState,
    LoadingSkeleton,
    PageHeader,
} from '@/components/data-states';

import { Badge } from '@/components/ui/badge';

import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import { Button } from '@/components/ui/button';

import { useCategory } from '../hooks';

import { useDocumentTitle } from '@/hooks/use-document-title';

import { formatDate } from '@/lib/date';

export default function CategoryDetailPage() {
    const { t } =
        useTranslation();

    const { id } =
        useParams<{
            id: string;
        }>();

    const navigate =
        useNavigate();

    const {
        data: category,
        isLoading,
        isError,
        refetch,
    } = useCategory(id);

    useDocumentTitle(
        category?.name
            ? t(
                'catalogCategories.editTitle',
                {
                    name: category.name,
                },
            )
            : t(
                'catalogCategories.detailsTitle',
            ),
    );

    if (isLoading) {
        return (
            <LoadingSkeleton
                variant="cards"
                rows={5}
            />
        );
    }

    if (isError || !category) {
        return (
            <ErrorState
                error={
                    new Error(
                        t(
                            'catalogCategories.emptyTitle',
                        ),
                    )
                }
                onRetry={() =>
                    refetch()
                }
                message={t(
                    'catalogCategories.actionFailed',
                )}
            />
        );
    }

    return (
        <div className="space-y-6">
            <PageHeader
                title={
                    category.name
                }
                description={t(
                    'catalogCategories.editDescription',
                )}
                actions={
                    <div className="flex gap-2">
                        <Button
                            variant="outline"
                            onClick={() =>
                                navigate(
                                    '/admin/categories',
                                )
                            }
                        >
                            <ArrowLeft />
                            {t(
                                'catalogCategories.back',
                            )}
                        </Button>

                        <Button
                            onClick={() =>
                                navigate(
                                    `/admin/categories/${id}/edit`,
                                )
                            }
                        >
                            <Pencil />
                            {t(
                                'catalogCategories.edit',
                            )}
                        </Button>
                    </div>
                }
            />

            <div className="grid gap-6 md:grid-cols-2">
                {/* =================================================
                    Basic Information
                ================================================= */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogCategories.basicInfo',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'catalogCategories.basicInfoDescription',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        {/* -------------------------------------------------
                            Identity
                        ------------------------------------------------- */}

                        <div className="flex items-center gap-4">
                            <div className="bg-muted grid size-16 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                {category.imageUrl ? (
                                    <img
                                        src={
                                            category.imageUrl
                                        }
                                        alt={
                                            category.name
                                        }
                                        className="size-full object-contain"
                                    />
                                ) : (
                                    <FolderTree className="text-muted-foreground size-8" />
                                )}
                            </div>

                            <div className="min-w-0">
                                <div className="text-lg font-semibold">
                                    {
                                        category.name
                                    }
                                </div>

                                <div className="text-muted-foreground text-sm">
                                    {t(
                                        'catalogCategories.slug',
                                    )}
                                    :{' '}
                                    {category.slug ||
                                        '—'}
                                </div>
                            </div>
                        </div>

                        {/* -------------------------------------------------
                            Status
                        ------------------------------------------------- */}

                        <div className="space-y-2">
                            <div className="flex flex-wrap items-center gap-2">
                                <Badge
                                    variant={
                                        category.isActive
                                            ? 'default'
                                            : 'secondary'
                                    }
                                >
                                    {category.isActive
                                        ? t(
                                            'catalogCategories.active',
                                        )
                                        : t(
                                            'catalogCategories.inactive',
                                        )}
                                </Badge>

                                <Badge
                                    variant={
                                        category.isPublished
                                            ? 'default'
                                            : 'outline'
                                    }
                                >
                                    {category.isPublished
                                        ? t(
                                            'catalogCategories.published',
                                        )
                                        : t(
                                            'catalogCategories.draft',
                                        )}
                                </Badge>

                                {category.isFeatured && (
                                    <Badge variant="default">
                                        <Star className="mr-1 size-3" />
                                        {t(
                                            'catalogCategories.featured',
                                        )}
                                    </Badge>
                                )}
                            </div>

                            {/* -------------------------------------------------
                                Display Order
                            ------------------------------------------------- */}

                            <div className="text-muted-foreground text-sm">
                                {
                                    t(
                                        'catalogCategories.displayOrder',
                                    )
                                }
                                :{' '}
                                {
                                    category.displayOrder ??
                                    0
                                }
                            </div>

                            {/* -------------------------------------------------
                                Parent
                            ------------------------------------------------- */}

                            <div className="flex items-center gap-2 text-sm">
                                <FolderTree className="size-4" />

                                <span className="text-muted-foreground">
                                    {
                                        t(
                                            'catalogCategories.parent',
                                        )
                                    }
                                    :
                                </span>

                                <span>
                                    {
                                        category.parentCategoryName ||
                                        t(
                                            'catalogCategories.rootCategory',
                                        )
                                    }
                                </span>
                            </div>
                        </div>

                        {/* -------------------------------------------------
                            Description
                        ------------------------------------------------- */}

                        <div className="border-t pt-4">
                            <div className="text-sm font-medium">
                                {
                                    t(
                                        'catalogCategories.descriptionLabel',
                                    )
                                }
                            </div>

                            <div className="text-muted-foreground mt-1 text-sm">
                                {
                                    category.description ||
                                    t(
                                        'catalogCategories.noDescription',
                                    )
                                }
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/* =================================================
                    Category Image
                ================================================= */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {
                                t(
                                    'catalogCategories.image',
                                )
                            }
                        </CardTitle>

                        <CardDescription>
                            {
                                t(
                                    'catalogCategories.basicInfoDescription',
                                )
                            }
                        </CardDescription>
                    </CardHeader>

                    <CardContent>
                        {category.imageUrl ? (
                            <img
                                src={
                                    category.imageUrl
                                }
                                alt={
                                    category.name
                                }
                                className="max-h-64 w-auto rounded-lg border object-contain"
                            />
                        ) : (
                            <div className="text-muted-foreground text-sm">
                                {
                                    t(
                                        'catalogCategories.noImage',
                                    )
                                }
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* =================================================
                    SEO
                ================================================= */}

                <Card className="md:col-span-2">
                    <CardHeader>
                        <div className="flex items-center gap-2">
                            <Search className="size-5" />

                            <CardTitle>
                                {
                                    t(
                                        'catalogCategories.seo',
                                    )
                                }
                            </CardTitle>
                        </div>

                        <CardDescription>
                            {
                                t(
                                    'catalogCategories.seoDescription',
                                )
                            }
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="grid gap-4 md:grid-cols-3">
                        {/* SEO Title */}

                        <div className="space-y-1">
                            <div className="text-sm font-medium">
                                {
                                    t(
                                        'catalogCategories.seoTitle',
                                    )
                                }
                            </div>

                            <div className="text-muted-foreground text-sm break-words">
                                {
                                    category.seoTitle ||
                                    t(
                                        'catalogCategories.notSet',
                                    )
                                }
                            </div>
                        </div>

                        {/* SEO Description */}

                        <div className="space-y-1">
                            <div className="text-sm font-medium">
                                {
                                    t(
                                        'catalogCategories.seoMetaDescription',
                                    )
                                }
                            </div>

                            <div className="text-muted-foreground text-sm break-words">
                                {
                                    category.seoDescription ||
                                    t(
                                        'catalogCategories.notSet',
                                    )
                                }
                            </div>
                        </div>

                        {/* SEO Keywords */}

                        <div className="space-y-1">
                            <div className="text-sm font-medium">
                                {
                                    t(
                                        'catalogCategories.seoKeywords',
                                    )
                                }
                            </div>

                            <div className="text-muted-foreground text-sm break-words">
                                {
                                    category.seoKeywords ||
                                    t(
                                        'catalogCategories.notSet',
                                    )
                                }
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/* =================================================
                    System Information
                ================================================= */}

                <Card className="md:col-span-2">
                    <CardHeader>
                        <CardTitle>
                            {
                                t(
                                    'catalogCategories.systemInfo',
                                )
                            }
                        </CardTitle>

                        <CardDescription>
                            {
                                t(
                                    'catalogCategories.systemInfoDescription',
                                )
                            }
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="grid gap-4 md:grid-cols-2">
                        {/* Created */}

                        <div className="flex items-center gap-2 text-sm">
                            <Calendar className="size-4 shrink-0" />

                            <span className="text-muted-foreground">
                                {
                                    t(
                                        'catalogCategories.created',
                                    )
                                }
                                :
                            </span>

                            <span>
                                {
                                    category.createdAt
                                        ? formatDate(
                                            category.createdAt,
                                        )
                                        : t(
                                            'catalogCategories.notSet',
                                        )
                                }
                            </span>
                        </div>

                        {/* Updated */}

                        <div className="flex items-center gap-2 text-sm">
                            <Calendar className="size-4 shrink-0" />

                            <span className="text-muted-foreground">
                                {
                                    t(
                                        'catalogCategories.updated',
                                    )
                                }
                                :
                            </span>

                            <span>
                                {
                                    category.updatedAt
                                        ? formatDate(
                                            category.updatedAt,
                                        )
                                        : t(
                                            'catalogCategories.notUpdated',
                                        )
                                }
                            </span>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}