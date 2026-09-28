import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import {
    Plus,
    Search,
    MoreHorizontal,
    Eye,
    Pencil,
    Trash2,
    Power,
    PowerOff,
    Star,
    StarOff,
    Globe,
    Globe2,
} from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

import {
    PageHeader,
    EmptyState,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { ConfirmDialog } from '@/components/confirm-dialog';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';
import type { BrandListItem } from '@/modules/catalog/api/brands';
import {
    useBrandAction,
    useBrands,
} from '@/modules/catalog/brands/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

type BrandFilter =
    | 'all'
    | 'active'
    | 'inactive'
    | 'published'
    | 'featured';

export default function BrandsPage() {
    const { t } = useTranslation();

    useDocumentTitle(
        t('catalogBrands.title'),
    );

    const navigate = useNavigate();

    const [search, setSearch] =
        useState('');

    const [page, setPage] =
        useState(1);

    const [filter, setFilter] =
        useState<BrandFilter>('all');

    const [confirm, setConfirm] =
        useState<{
            type: 'delete';
            brand: BrandListItem;
        } | null>(null);

    const canCreate =
        usePermission(
            PERM.brandsCreate,
        );

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

    const queryFilter = useMemo(
        () => ({
            page,
            pageSize: 20,
            search,
            isActive:
                filter === 'active'
                    ? true
                    : filter === 'inactive'
                        ? false
                        : undefined,
            isPublished:
                filter === 'published'
                    ? true
                    : undefined,
            isFeatured:
                filter === 'featured'
                    ? true
                    : undefined,
            sortBy: 'name',
            desc: false,
        }),
        [page, search, filter],
    );

    const query =
        useBrands(queryFilter);

    const action =
        useBrandAction('remove');

    const activateAction =
        useBrandAction('activate');

    const deactivateAction =
        useBrandAction('deactivate');

    const publishAction =
        useBrandAction('publish');

    const unpublishAction =
        useBrandAction('unpublish');

    const featureAction =
        useBrandAction('feature');

    const unfeatureAction =
        useBrandAction('unfeature');

    const run = async (
        promise: Promise<unknown>,
        success: string,
    ) => {
        try {
            await promise;
            toast.success(success);
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : t('catalogBrands.actionFailed'),
            );
        }
    };

    const onConfirm =
        async () => {
            if (!confirm) {
                return;
            }

            if (
                confirm.type ===
                'delete'
            ) {
                await run(
                    action.mutateAsync(
                        confirm.brand.id,
                    ),
                    t(
                        'catalogBrands.toast.deleted',
                    ),
                );
            }

            setConfirm(null);
        };

    const filters: BrandFilter[] =
        [
            'all',
            'active',
            'inactive',
            'published',
            'featured',
        ];

    return (
        <div className="space-y-6">
            <PageHeader
                title={t(
                    'catalogBrands.title',
                )}
                description={t(
                    'catalogBrands.description',
                )}
                actions={
                    canCreate ? (
                        <Button asChild>
                            <Link to="/admin/brands/new">
                                <Plus />
                                {t(
                                    'catalogBrands.newBrand',
                                )}
                            </Link>
                        </Button>
                    ) : null
                }
            />

            <Card>
                <CardContent className="space-y-4 pt-6">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                        <div className="relative w-full md:max-w-md">
                            <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />

                            <Input
                                className="ps-9"
                                value={search}
                                onChange={event => {
                                    setSearch(
                                        event.target.value,
                                    );
                                    setPage(1);
                                }}
                                placeholder={t(
                                    'catalogBrands.search',
                                )}
                            />
                        </div>

                        <div className="flex flex-wrap gap-2">
                            {filters.map(
                                item => (
                                    <Button
                                        key={item}
                                        size="sm"
                                        variant={
                                            filter ===
                                                item
                                                ? 'default'
                                                : 'outline'
                                        }
                                        onClick={() => {
                                            setFilter(item);
                                            setPage(1);
                                        }}
                                    >
                                        {t(
                                            `catalogBrands.filters.${item}`,
                                        )}
                                    </Button>
                                ),
                            )}
                        </div>
                    </div>

                    {query.isLoading && (
                        <LoadingSkeleton
                            variant="table"
                            rows={8}
                            cols={6}
                        />
                    )}

                    {query.isError && (
                        <ErrorState
                            error={query.error}
                            onRetry={() =>
                                query.refetch()
                            }
                            message={t(
                                'catalogBrands.loadError',
                            )}
                        />
                    )}

                    {query.isSuccess &&
                        query.data.items
                            .length === 0 && (
                            <EmptyState
                                icon={Globe2}
                                title={t(
                                    'catalogBrands.empty.title',
                                )}
                                description={
                                    search
                                        ? t(
                                            'catalogBrands.empty.searchDescription',
                                        )
                                        : t(
                                            'catalogBrands.empty.description',
                                        )
                                }
                                action={
                                    canCreate ? (
                                        <Button asChild>
                                            <Link to="/admin/brands/new">
                                                <Plus />
                                                {t(
                                                    'catalogBrands.newBrand',
                                                )}
                                            </Link>
                                        </Button>
                                    ) : undefined
                                }
                            />
                        )}

                    {query.isSuccess &&
                        query.data.items
                            .length > 0 && (
                            <>
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>
                                                {t(
                                                    'catalogBrands.table.brand',
                                                )}
                                            </TableHead>

                                            <TableHead>
                                                {t(
                                                    'catalogBrands.table.slug',
                                                )}
                                            </TableHead>

                                            <TableHead>
                                                {t(
                                                    'catalogBrands.table.status',
                                                )}
                                            </TableHead>

                                            <TableHead>
                                                {t(
                                                    'catalogBrands.table.publishing',
                                                )}
                                            </TableHead>

                                            <TableHead>
                                                {t(
                                                    'catalogBrands.table.featured',
                                                )}
                                            </TableHead>

                                            <TableHead className="text-end">
                                                {t(
                                                    'catalogBrands.table.actions',
                                                )}
                                            </TableHead>
                                        </TableRow>
                                    </TableHeader>

                                    <TableBody>
                                        {query.data.items.map(
                                            brand => (
                                                <TableRow
                                                    key={brand.id}
                                                >
                                                    <TableCell>
                                                        <div className="flex items-center gap-3">
                                                            <div className="bg-muted grid size-10 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                                                {brand.logoUrl ? (
                                                                    <img
                                                                        src={
                                                                            brand.logoUrl
                                                                        }
                                                                        alt=""
                                                                        className="size-full object-contain"
                                                                    />
                                                                ) : (
                                                                    <Globe className="text-muted-foreground size-5" />
                                                                )}
                                                            </div>

                                                            <div className="min-w-0">
                                                                <div className="max-w-56 truncate font-medium">
                                                                    {brand.name}
                                                                </div>

                                                                <div className="text-muted-foreground text-xs">
                                                                    {t(
                                                                        'catalogBrands.table.order',
                                                                        {
                                                                            order:
                                                                                brand.displayOrder,
                                                                        },
                                                                    )}
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </TableCell>

                                                    <TableCell className="text-muted-foreground">
                                                        {brand.slug}
                                                    </TableCell>

                                                    <TableCell>
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
                                                    </TableCell>

                                                    <TableCell>
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
                                                    </TableCell>

                                                    <TableCell>
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
                                                    </TableCell>

                                                    <TableCell className="text-end">
                                                        <DropdownMenu>
                                                            <DropdownMenuTrigger
                                                                asChild
                                                            >
                                                                <Button
                                                                    variant="ghost"
                                                                    size="icon"
                                                                    aria-label={t(
                                                                        'catalogBrands.actions.menu',
                                                                    )}
                                                                >
                                                                    <MoreHorizontal />
                                                                </Button>
                                                            </DropdownMenuTrigger>

                                                            <DropdownMenuContent align="end">
                                                                <DropdownMenuItem
                                                                    onClick={() =>
                                                                        navigate(
                                                                            `/admin/brands/${brand.id}`,
                                                                        )
                                                                    }
                                                                >
                                                                    <Eye />
                                                                    {t(
                                                                        'catalogBrands.actions.view',
                                                                    )}
                                                                </DropdownMenuItem>

                                                                {canUpdate && (
                                                                    <DropdownMenuItem
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
                                                                    </DropdownMenuItem>
                                                                )}

                                                                <DropdownMenuSeparator />

                                                                {canStatus &&
                                                                    (brand.isActive ? (
                                                                        <DropdownMenuItem
                                                                            onClick={() =>
                                                                                run(
                                                                                    deactivateAction.mutateAsync(
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
                                                                        </DropdownMenuItem>
                                                                    ) : (
                                                                        <DropdownMenuItem
                                                                            onClick={() =>
                                                                                run(
                                                                                    activateAction.mutateAsync(
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
                                                                        </DropdownMenuItem>
                                                                    ))}

                                                                {canPublish &&
                                                                    (brand.isPublished ? (
                                                                        <DropdownMenuItem
                                                                            onClick={() =>
                                                                                run(
                                                                                    unpublishAction.mutateAsync(
                                                                                        brand.id,
                                                                                    ),
                                                                                    t(
                                                                                        'catalogBrands.toast.unpublished',
                                                                                    ),
                                                                                )
                                                                            }
                                                                        >
                                                                            <Globe2 />
                                                                            {t(
                                                                                'catalogBrands.actions.unpublish',
                                                                            )}
                                                                        </DropdownMenuItem>
                                                                    ) : (
                                                                        <DropdownMenuItem
                                                                            disabled={
                                                                                !brand.isActive
                                                                            }
                                                                            onClick={() =>
                                                                                run(
                                                                                    publishAction.mutateAsync(
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
                                                                        </DropdownMenuItem>
                                                                    ))}

                                                                {canFeature &&
                                                                    (brand.isFeatured ? (
                                                                        <DropdownMenuItem
                                                                            onClick={() =>
                                                                                run(
                                                                                    unfeatureAction.mutateAsync(
                                                                                        brand.id,
                                                                                    ),
                                                                                    t(
                                                                                        'catalogBrands.toast.unfeatured',
                                                                                    ),
                                                                                )
                                                                            }
                                                                        >
                                                                            <StarOff />
                                                                            {t(
                                                                                'catalogBrands.actions.unfeature',
                                                                            )}
                                                                        </DropdownMenuItem>
                                                                    ) : (
                                                                        <DropdownMenuItem
                                                                            disabled={
                                                                                !brand.isActive
                                                                            }
                                                                            onClick={() =>
                                                                                run(
                                                                                    featureAction.mutateAsync(
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
                                                                        </DropdownMenuItem>
                                                                    ))}

                                                                {canDelete && (
                                                                    <DropdownMenuItem
                                                                        className="text-destructive"
                                                                        onClick={() =>
                                                                            setConfirm(
                                                                                {
                                                                                    type: 'delete',
                                                                                    brand,
                                                                                },
                                                                            )
                                                                        }
                                                                    >
                                                                        <Trash2 />
                                                                        {t(
                                                                            'catalogBrands.actions.delete',
                                                                        )}
                                                                    </DropdownMenuItem>
                                                                )}
                                                            </DropdownMenuContent>
                                                        </DropdownMenu>
                                                    </TableCell>
                                                </TableRow>
                                            ),
                                        )}
                                    </TableBody>
                                </Table>

                                <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
                                    <div className="text-muted-foreground text-sm">
                                        {t(
                                            'catalogBrands.pagination.summary',
                                            {
                                                total:
                                                    query.data
                                                        .totalItems,
                                                page:
                                                    query.data
                                                        .page,
                                                pages: Math.max(
                                                    query.data
                                                        .totalPages,
                                                    1,
                                                ),
                                            },
                                        )}
                                    </div>

                                    <div className="flex gap-2">
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            disabled={
                                                !query.data
                                                    .hasPrev
                                            }
                                            onClick={() =>
                                                setPage(
                                                    current =>
                                                        Math.max(
                                                            1,
                                                            current -
                                                            1,
                                                        ),
                                                )
                                            }
                                        >
                                            {t(
                                                'catalogBrands.pagination.previous',
                                            )}
                                        </Button>

                                        <Button
                                            size="sm"
                                            variant="outline"
                                            disabled={
                                                !query.data
                                                    .hasNext
                                            }
                                            onClick={() =>
                                                setPage(
                                                    current =>
                                                        current + 1,
                                                )
                                            }
                                        >
                                            {t(
                                                'catalogBrands.pagination.next',
                                            )}
                                        </Button>
                                    </div>
                                </div>
                            </>
                        )}
                </CardContent>
            </Card>

            <ConfirmDialog
                open={!!confirm}
                onOpenChange={open =>
                    !open &&
                    setConfirm(null)
                }
                title={t(
                    'catalogBrands.delete.title',
                    {
                        name:
                            confirm?.brand
                                .name ?? '',
                    },
                )}
                description={t(
                    'catalogBrands.delete.description',
                )}
                confirmLabel={t(
                    'catalogBrands.delete.confirm',
                )}
                destructive
                pending={action.isPending}
                onConfirm={onConfirm}
            />
        </div>
    );
}