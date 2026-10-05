import {
    useMemo,
    useState,
} from 'react';

import {
    Link,
    useNavigate,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

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
    Package,
} from 'lucide-react';

import {
    toast,
} from 'sonner';

import {
    resolveMediaUrl,
} from '@/lib/media-url';

import {
    PageHeader,
    EmptyState,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';

import {
    Button,
} from '@/components/ui/button';

import {
    Input,
} from '@/components/ui/input';

import {
    Card,
    CardContent,
} from '@/components/ui/card';

import {
    Badge,
} from '@/components/ui/badge';

import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';

import {
    ConfirmDialog,
} from '@/components/confirm-dialog';

import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

import {
    usePermission,
} from '@/hooks/use-permission';

import {
    PERM,
} from '@/lib/api/admin';

import {
    useAdminProducts,
    useDeleteProduct,
    useToggleActive,
    useToggleFeatured,
} from '../hooks';

import {
    useDocumentTitle,
} from '@/hooks/use-document-title';

import {
    formatPrice,
} from '@/lib/utils';

import type {
    ProductListItem,
} from '@/modules/catalog/api/products';

export default function ProductListPage() {
    const {
        i18n,
    } = useTranslation();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa') ?? false;

    const text = isFa
        ? {
            title:
                'مدیریت محصولات',

            description:
                'مدیریت محصولات و وضعیت فروش فروشگاه',

            create:
                'محصول جدید',

            searchPlaceholder:
                'جستجوی محصولات...',

            all:
                'همه',

            active:
                'فعال',

            inactive:
                'غیرفعال',

            featured:
                'ویژه',

            inStock:
                'موجود',

            outOfStock:
                'ناموجود',

            loadError:
                'بارگذاری محصولات با خطا مواجه شد.',

            retry:
                'تلاش دوباره',

            noProducts:
                'محصولی یافت نشد',

            searchAgain:
                'جستجوی دیگری انجام دهید.',

            firstProduct:
                'اولین محصول را ایجاد کنید.',

            product:
                'محصول',

            price:
                'قیمت',

            categories:
                'دسته‌بندی',

            status:
                'وضعیت',

            special:
                'ویژه',

            inventory:
                'موجودی',

            actions:
                'عملیات',

            activeLabel:
                'فعال',

            inactiveLabel:
                'غیرفعال',

            featuredLabel:
                'ویژه',

            normalLabel:
                'عادی',

            stockCount:
                'موجود',

            sku:
                'SKU',

            view:
                'مشاهده',

            edit:
                'ویرایش',

            disable:
                'غیرفعال‌سازی',

            enable:
                'فعال‌سازی',

            removeFeatured:
                'لغو ویژه',

            makeFeatured:
                'ویژه',

            delete:
                'حذف',

            previous:
                'قبلی',

            next:
                'بعدی',

            total:
                'کل',

            page:
                'صفحه',

            deleteTitle:
                'حذف محصول',

            deleteDescription:
                'محصول به‌صورت نرم حذف می‌شود.',

            deleteConfirm:
                'حذف محصول',

            created:
                'محصول ایجاد شد.',

            disabled:
                'محصول غیرفعال شد.',

            enabled:
                'محصول فعال شد.',

            featuredSuccess:
                'محصول ویژه شد.',

            unfeaturedSuccess:
                'محصول از حالت ویژه خارج شد.',

            deleted:
                'محصول حذف شد.',

            operationFailed:
                'عملیات ناموفق بود.',
        }
        : {
            title:
                'Product Management',

            description:
                'Manage your store products and selling status.',

            create:
                'New Product',

            searchPlaceholder:
                'Search products...',

            all:
                'All',

            active:
                'Active',

            inactive:
                'Inactive',

            featured:
                'Featured',

            inStock:
                'In stock',

            outOfStock:
                'Out of stock',

            loadError:
                'We could not load the products.',

            retry:
                'Retry',

            noProducts:
                'No products found',

            searchAgain:
                'Try a different search.',

            firstProduct:
                'Create your first product to start selling.',

            product:
                'Product',

            price:
                'Price',

            categories:
                'Categories',

            status:
                'Status',

            special:
                'Featured',

            inventory:
                'Inventory',

            actions:
                'Actions',

            activeLabel:
                'Active',

            inactiveLabel:
                'Inactive',

            featuredLabel:
                'Featured',

            normalLabel:
                'Standard',

            stockCount:
                'In stock',

            sku:
                'SKU',

            view:
                'View',

            edit:
                'Edit',

            disable:
                'Deactivate',

            enable:
                'Activate',

            removeFeatured:
                'Remove featured',

            makeFeatured:
                'Mark featured',

            delete:
                'Delete',

            previous:
                'Previous',

            next:
                'Next',

            total:
                'total',

            page:
                'Page',

            deleteTitle:
                'Delete product',

            deleteDescription:
                'The product will be soft-deleted.',

            deleteConfirm:
                'Delete product',

            created:
                'Product created.',

            disabled:
                'Product deactivated.',

            enabled:
                'Product activated.',

            featuredSuccess:
                'Product marked as featured.',

            unfeaturedSuccess:
                'Product removed from featured.',

            deleted:
                'Product deleted.',

            operationFailed:
                'The operation failed.',
        };

    useDocumentTitle(
        text.title,
    );

    const navigate =
        useNavigate();

    const [
        search,
        setSearch,
    ] = useState('');

    const [
        page,
        setPage,
    ] = useState(1);

    const [
        filter,
        setFilter,
    ] = useState<
        | 'all'
        | 'active'
        | 'inactive'
        | 'featured'
        | 'inStock'
        | 'outOfStock'
    >('all');

    const [
        confirm,
        setConfirm,
    ] = useState<{
        type: 'delete';
        product: ProductListItem;
    } | null>(null);

    const canCreate =
        usePermission(
            PERM.productsCreate,
        );

    const canUpdate =
        usePermission(
            PERM.productsUpdate,
        );

    const canDelete =
        usePermission(
            PERM.productsDelete,
        );

    const canStatus =
        usePermission(
            PERM.productsStatus,
        );

    const queryFilter =
        useMemo(
            () => ({
                page,
                pageSize: 20,

                search,

                isActive:
                    filter ===
                        'active'
                        ? true
                        : filter ===
                            'inactive'
                            ? false
                            : undefined,

                isFeatured:
                    filter ===
                        'featured'
                        ? true
                        : undefined,

                isInStock:
                    filter ===
                        'inStock'
                        ? true
                        : filter ===
                            'outOfStock'
                            ? false
                            : undefined,

                sortBy:
                    'newest' as const,
            }),
            [
                page,
                search,
                filter,
            ],
        );

    const query =
        useAdminProducts(
            queryFilter,
        );

    const deleteMutation =
        useDeleteProduct();

    const toggleActive =
        useToggleActive();

    const toggleFeatured =
        useToggleFeatured();

    const run =
        async (
            promise: Promise<unknown>,
            success: string,
        ) => {
            try {
                await promise;

                toast.success(
                    success,
                );
            } catch (error) {
                toast.error(
                    error instanceof Error
                        ? error.message
                        : text.operationFailed,
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
                    deleteMutation.mutateAsync(
                        confirm.product.id,
                    ),
                    text.deleted,
                );
            }

            setConfirm(
                null,
            );
        };

    return (
        <div
            className="space-y-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <PageHeader
                title={
                    text.title
                }
                description={
                    text.description
                }
                actions={
                    canCreate
                        ? (
                            <Button
                                asChild
                            >
                                <Link
                                    to="/admin/products/new"
                                >
                                    <Plus />

                                    {
                                        text.create
                                    }
                                </Link>
                            </Button>
                        )
                        : null
                }
            />

            <Card>
                <CardContent className="space-y-4 pt-6">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                        <div className="relative w-full md:max-w-md">
                            <Search className="absolute start-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />

                            <Input
                                className="ps-9"
                                value={
                                    search
                                }
                                onChange={
                                    event => {
                                        setSearch(
                                            event.target.value,
                                        );

                                        setPage(
                                            1,
                                        );
                                    }
                                }
                                placeholder={
                                    text.searchPlaceholder
                                }
                            />
                        </div>

                        <div className="flex flex-wrap gap-2">
                            {(
                                [
                                    'all',
                                    'active',
                                    'inactive',
                                    'featured',
                                    'inStock',
                                    'outOfStock',
                                ] as const
                            ).map(
                                item => (
                                    <Button
                                        key={
                                            item
                                        }
                                        size="sm"
                                        variant={
                                            filter ===
                                                item
                                                ? 'default'
                                                : 'outline'
                                        }
                                        onClick={() => {
                                            setFilter(
                                                item,
                                            );

                                            setPage(
                                                1,
                                            );
                                        }}
                                    >
                                        {item ===
                                            'all' &&
                                            text.all}

                                        {item ===
                                            'active' &&
                                            text.active}

                                        {item ===
                                            'inactive' &&
                                            text.inactive}

                                        {item ===
                                            'featured' &&
                                            text.featured}

                                        {item ===
                                            'inStock' &&
                                            text.inStock}

                                        {item ===
                                            'outOfStock' &&
                                            text.outOfStock}
                                    </Button>
                                ),
                            )}
                        </div>
                    </div>

                    {query.isLoading && (
                        <LoadingSkeleton
                            variant="table"
                            rows={8}
                            cols={7}
                        />
                    )}

                    {query.isError && (
                        <ErrorState
                            error={
                                query.error
                            }
                            onRetry={() =>
                                query.refetch()
                            }
                            message={
                                text.loadError
                            }
                        />
                    )}

                    {query.isSuccess &&
                        query.data
                            ?.items
                            ?.length ===
                        0 && (
                            <EmptyState
                                icon={
                                    Package
                                }
                                title={
                                    text.noProducts
                                }
                                description={
                                    search
                                        ? text.searchAgain
                                        : text.firstProduct
                                }
                                action={
                                    canCreate
                                        ? (
                                            <Button
                                                asChild
                                            >
                                                <Link
                                                    to="/admin/products/new"
                                                >
                                                    <Plus />

                                                    {
                                                        text.create
                                                    }
                                                </Link>
                                            </Button>
                                        )
                                        : undefined
                                }
                            />
                        )}

                    {query.isSuccess &&
                        query.data
                            ?.items
                            ?.length >
                        0 && (
                            <>
                                <div className="overflow-x-auto">
                                    <Table>
                                        <TableHeader>
                                            <TableRow>
                                                <TableHead>
                                                    {
                                                        text.product
                                                    }
                                                </TableHead>

                                                <TableHead>
                                                    {
                                                        text.price
                                                    }
                                                </TableHead>

                                                <TableHead>
                                                    {
                                                        text.categories
                                                    }
                                                </TableHead>

                                                <TableHead>
                                                    {
                                                        text.status
                                                    }
                                                </TableHead>

                                                <TableHead>
                                                    {
                                                        text.special
                                                    }
                                                </TableHead>

                                                <TableHead>
                                                    {
                                                        text.inventory
                                                    }
                                                </TableHead>

                                                <TableHead className="text-end">
                                                    {
                                                        text.actions
                                                    }
                                                </TableHead>
                                            </TableRow>
                                        </TableHeader>

                                        <TableBody>
                                            {query.data.items.map(
                                                product => (
                                                    <TableRow
                                                        key={
                                                            product.id
                                                        }
                                                    >
                                                        <TableCell>
                                                            <div className="flex items-center gap-3">
                                                                <div className="grid size-10 shrink-0 place-items-center overflow-hidden rounded-lg border bg-muted">
                                                                    {product.mainImage ? (
                                                                        <img
                                                                            src={resolveMediaUrl(
                                                                                product.mainImage,
                                                                            )}
                                                                            alt=""
                                                                            className="size-full object-contain"
                                                                        />
                                                                    ) : (
                                                                        <Package className="size-5 text-muted-foreground" />
                                                                    )}
                                                                </div>

                                                                <div className="min-w-0">
                                                                    <div className="max-w-48 truncate font-medium">
                                                                        {
                                                                            product.name
                                                                        }
                                                                    </div>

                                                                    <div className="text-xs text-muted-foreground">
                                                                        {
                                                                            text.sku
                                                                        }
                                                                        :{' '}
                                                                        {product.sku ||
                                                                            '—'}
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </TableCell>

                                                        <TableCell>
                                                            <div className="flex flex-col">
                                                                <span className="font-medium">
                                                                    {formatPrice(
                                                                        product.finalPrice,
                                                                    )}
                                                                </span>

                                                                {product.price !==
                                                                    product.finalPrice && (
                                                                        <span className="text-xs text-muted-foreground line-through">
                                                                            {formatPrice(
                                                                                product.price,
                                                                            )}
                                                                        </span>
                                                                    )}
                                                            </div>
                                                        </TableCell>

                                                        <TableCell className="text-muted-foreground">
                                                            {product.categoryNames
                                                                ?.slice(
                                                                    0,
                                                                    2,
                                                                )
                                                                .join(
                                                                    ', ',
                                                                )}

                                                            {product
                                                                .categoryNames
                                                                ?.length >
                                                                2 &&
                                                                ' ...'}
                                                        </TableCell>

                                                        <TableCell>
                                                            <Badge
                                                                variant={
                                                                    product.isActive
                                                                        ? 'default'
                                                                        : 'secondary'
                                                                }
                                                            >
                                                                {product.isActive
                                                                    ? text.activeLabel
                                                                    : text.inactiveLabel}
                                                            </Badge>
                                                        </TableCell>

                                                        <TableCell>
                                                            <Badge
                                                                variant={
                                                                    product.isFeatured
                                                                        ? 'default'
                                                                        : 'outline'
                                                                }
                                                            >
                                                                {product.isFeatured
                                                                    ? text.featuredLabel
                                                                    : text.normalLabel}
                                                            </Badge>
                                                        </TableCell>

                                                        <TableCell>
                                                            <Badge
                                                                variant={
                                                                    product.isInStock
                                                                        ? 'default'
                                                                        : 'destructive'
                                                                }
                                                            >
                                                                {product.isInStock
                                                                    ? `${text.stockCount} (${product.stockQuantity})`
                                                                    : text.outOfStock}
                                                            </Badge>
                                                        </TableCell>

                                                        <TableCell className="text-end">
                                                            <DropdownMenu>
                                                                <DropdownMenuTrigger asChild>
                                                                    <Button
                                                                        variant="ghost"
                                                                        size="icon"
                                                                        aria-label={
                                                                            text.actions
                                                                        }
                                                                    >
                                                                        <MoreHorizontal />
                                                                    </Button>
                                                                </DropdownMenuTrigger>

                                                                <DropdownMenuContent align="end">
                                                                    <DropdownMenuItem
                                                                        onClick={() =>
                                                                            navigate(
                                                                                `/admin/products/${product.id}`,
                                                                            )
                                                                        }
                                                                    >
                                                                        <Eye />

                                                                        {
                                                                            text.view
                                                                        }
                                                                    </DropdownMenuItem>

                                                                    {canUpdate && (
                                                                        <DropdownMenuItem
                                                                            onClick={() =>
                                                                                navigate(
                                                                                    `/admin/products/${product.id}/edit`,
                                                                                )
                                                                            }
                                                                        >
                                                                            <Pencil />

                                                                            {
                                                                                text.edit
                                                                            }
                                                                        </DropdownMenuItem>
                                                                    )}

                                                                    <DropdownMenuSeparator />

                                                                    {canStatus && (
                                                                        product.isActive
                                                                            ? (
                                                                                <DropdownMenuItem
                                                                                    onClick={() =>
                                                                                        run(
                                                                                            toggleActive.mutateAsync(
                                                                                                {
                                                                                                    id: product.id,
                                                                                                    isActive: false,
                                                                                                },
                                                                                            ),
                                                                                            text.disabled,
                                                                                        )
                                                                                    }
                                                                                >
                                                                                    <PowerOff />

                                                                                    {
                                                                                        text.disable
                                                                                    }
                                                                                </DropdownMenuItem>
                                                                            )
                                                                            : (
                                                                                <DropdownMenuItem
                                                                                    onClick={() =>
                                                                                        run(
                                                                                            toggleActive.mutateAsync(
                                                                                                {
                                                                                                    id: product.id,
                                                                                                    isActive: true,
                                                                                                },
                                                                                            ),
                                                                                            text.enabled,
                                                                                        )
                                                                                    }
                                                                                >
                                                                                    <Power />

                                                                                    {
                                                                                        text.enable
                                                                                    }
                                                                                </DropdownMenuItem>
                                                                            )
                                                                    )}

                                                                    <DropdownMenuItem
                                                                        onClick={() =>
                                                                            run(
                                                                                toggleFeatured.mutateAsync(
                                                                                    {
                                                                                        id: product.id,
                                                                                        isFeatured:
                                                                                            !product.isFeatured,
                                                                                    },
                                                                                ),
                                                                                product.isFeatured
                                                                                    ? text.unfeaturedSuccess
                                                                                    : text.featuredSuccess,
                                                                            )
                                                                        }
                                                                    >
                                                                        {
                                                                            product.isFeatured
                                                                                ? (
                                                                                    <StarOff />
                                                                                )
                                                                                : (
                                                                                    <Star />
                                                                                )
                                                                        }

                                                                        {
                                                                            product.isFeatured
                                                                                ? text.removeFeatured
                                                                                : text.makeFeatured
                                                                        }
                                                                    </DropdownMenuItem>

                                                                    {canDelete && (
                                                                        <>
                                                                            <DropdownMenuSeparator />

                                                                            <DropdownMenuItem
                                                                                className="text-destructive"
                                                                                onClick={() =>
                                                                                    setConfirm(
                                                                                        {
                                                                                            type:
                                                                                                'delete',
                                                                                            product,
                                                                                        },
                                                                                    )
                                                                                }
                                                                            >
                                                                                <Trash2 />

                                                                                {
                                                                                    text.delete
                                                                                }
                                                                            </DropdownMenuItem>
                                                                        </>
                                                                    )}
                                                                </DropdownMenuContent>
                                                            </DropdownMenu>
                                                        </TableCell>
                                                    </TableRow>
                                                ),
                                            )}
                                        </TableBody>
                                    </Table>
                                </div>

                                <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
                                    <div className="text-sm text-muted-foreground">
                                        {query.data.total}{' '}
                                        {
                                            text.total
                                        }{' '}
                                        ·{' '}
                                        {
                                            text.page
                                        }{' '}
                                        {
                                            query.data.page
                                        }{' '}
                                        /{' '}
                                        {Math.max(
                                            query.data.totalPages,
                                            1,
                                        )}
                                    </div>

                                    <div className="flex gap-2">
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            disabled={
                                                query.data.page <=
                                                1
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
                                            {
                                                text.previous
                                            }
                                        </Button>

                                        <Button
                                            size="sm"
                                            variant="outline"
                                            disabled={
                                                query.data.page >=
                                                query.data.totalPages
                                            }
                                            onClick={() =>
                                                setPage(
                                                    current =>
                                                        current +
                                                        1,
                                                )
                                            }
                                        >
                                            {
                                                text.next
                                            }
                                        </Button>
                                    </div>
                                </div>
                            </>
                        )}
                </CardContent>
            </Card>

            <ConfirmDialog
                open={
                    !!confirm
                }
                onOpenChange={
                    open =>
                        !open &&
                        setConfirm(
                            null,
                        )
                }
                title={
                    confirm
                        ? `${text.deleteTitle}: ${confirm.product.name}`
                        : text.deleteTitle
                }
                description={
                    text.deleteDescription
                }
                confirmLabel={
                    text.deleteConfirm
                }
                destructive
                pending={
                    deleteMutation.isPending
                }
                onConfirm={
                    onConfirm
                }
            />
        </div>
    );
}