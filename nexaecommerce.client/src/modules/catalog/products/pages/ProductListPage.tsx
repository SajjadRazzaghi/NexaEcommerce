// src/modules/catalog/products/pages/ProductListPage.tsx
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Plus, Search, MoreHorizontal, Eye, Pencil, Trash2, Power, PowerOff, Star, StarOff, Package } from 'lucide-react'; import { toast } from 'sonner';

import { PageHeader, EmptyState, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';
import { useAdminProducts, useDeleteProduct, useToggleActive, useToggleFeatured } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { formatPrice } from '@/lib/utils';

export default function ProductListPage() {
    useDocumentTitle('Product Management');
    const navigate = useNavigate();
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [filter, setFilter] = useState<'all' | 'active' | 'inactive' | 'featured' | 'inStock' | 'outOfStock'>('all');
    const [confirm, setConfirm] = useState<{ type: 'delete'; product: any } | null>(null);

    const canCreate = usePermission(PERM.productsCreate);
    const canUpdate = usePermission(PERM.productsUpdate);
    const canDelete = usePermission(PERM.productsDelete);
    const canStatus = usePermission(PERM.productsStatus);

    const queryFilter = useMemo(() => ({
        page,
        pageSize: 20,
        search,
        isActive: filter === 'active' ? true : filter === 'inactive' ? false : undefined,
        isFeatured: filter === 'featured' ? true : undefined,
        isInStock: filter === 'inStock' ? true : filter === 'outOfStock' ? false : undefined,
        sortBy: 'newest' as const,
    }), [page, search, filter]);

    const query = useAdminProducts(queryFilter);
    const deleteMutation = useDeleteProduct();
    const toggleActive = useToggleActive();
    const toggleFeatured = useToggleFeatured();

    const run = async (promise: Promise<unknown>, success: string) => {
        try {
            await promise;
            toast.success(success);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'عملیات ناموفق بود.');
        }
    };

    const onConfirm = async () => {
        if (!confirm) return;
        if (confirm.type === 'delete') {
            await run(deleteMutation.mutateAsync(confirm.product.id), 'محصول حذف شد.');
        }
        setConfirm(null);
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title="مدیریت محصولات"
                description="مدیریت محصولات فروشگاه"
                actions={canCreate ? (
                    <Button asChild>
                        <Link to="/admin/products/new"><Plus /> محصول جدید</Link>
                    </Button>
                ) : null}
            />

            <Card>
                <CardContent className="space-y-4 pt-6">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                        <div className="relative w-full md:max-w-md">
                            <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />
                            <Input
                                className="ps-9"
                                value={search}
                                onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                                placeholder="جستجوی محصولات…"
                            />
                        </div>
                        <div className="flex flex-wrap gap-2">
                            {(['all', 'active', 'inactive', 'featured', 'inStock', 'outOfStock'] as const).map((item) => (
                                <Button
                                    key={item}
                                    size="sm"
                                    variant={filter === item ? 'default' : 'outline'}
                                    onClick={() => { setFilter(item); setPage(1); }}
                                >
                                    {item === 'all' && 'همه'}
                                    {item === 'active' && 'فعال'}
                                    {item === 'inactive' && 'غیرفعال'}
                                    {item === 'featured' && 'ویژه'}
                                    {item === 'inStock' && 'موجود'}
                                    {item === 'outOfStock' && 'ناموجود'}
                                </Button>
                            ))}
                        </div>
                    </div>

                    {query.isLoading && <LoadingSkeleton variant="table" rows={8} cols={7} />}
                    {query.isError && (
                        <ErrorState
                            error={query.error}
                            onRetry={() => query.refetch()}
                            message="بارگذاری محصولات با خطا مواجه شد."
                        />
                    )}
                    {query.isSuccess && query.data?.items?.length === 0 && (
                        <EmptyState
                            icon={Package}
                            title="محصولی یافت نشد"
                            description={search ? 'جستجوی دیگری انجام دهید.' : 'اولین محصول را ایجاد کنید.'}
                            action={canCreate ? (
                                <Button asChild>
                                    <Link to="/admin/products/new"><Plus /> محصول جدید</Link>
                                </Button>
                            ) : undefined}
                        />
                    )}

                    {query.isSuccess && query.data?.items?.length > 0 && (
                        <>
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>محصول</TableHead>
                                        <TableHead>قیمت</TableHead>
                                        <TableHead>دسته‌بندی</TableHead>
                                        <TableHead>وضعیت</TableHead>
                                        <TableHead>ویژه</TableHead>
                                        <TableHead>موجودی</TableHead>
                                        <TableHead className="text-end">عملیات</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {query.data.items.map((product) => (
                                        <TableRow key={product.id}>
                                            <TableCell>
                                                <div className="flex items-center gap-3">
                                                    <div className="bg-muted grid size-10 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                                        {product.mainImage ? (
                                                            <img src={product.mainImage} alt="" className="size-full object-contain" />
                                                        ) : (
                                                            <Package className="text-muted-foreground size-5" />
                                                        )}
                                                    </div>
                                                    <div className="min-w-0">
                                                        <div className="font-medium truncate max-w-48">{product.name}</div>
                                                        <div className="text-muted-foreground text-xs">SKU: {product.sku || '—'}</div>
                                                    </div>
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex flex-col">
                                                    <span className="font-medium">{formatPrice(product.finalPrice)}</span>
                                                    {product.price !== product.finalPrice && (
                                                        <span className="text-xs text-muted-foreground line-through">
                                                            {formatPrice(product.price)}
                                                        </span>
                                                    )}
                                                </div>
                                            </TableCell>
                                            <TableCell className="text-muted-foreground">
                                                {product.categoryNames?.slice(0, 2).join(', ')}
                                                {product.categoryNames?.length > 2 && ' ...'}
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={product.isActive ? 'default' : 'secondary'}>
                                                    {product.isActive ? 'فعال' : 'غیرفعال'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={product.isFeatured ? 'default' : 'outline'}>
                                                    {product.isFeatured ? 'ویژه' : 'عادی'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={product.isInStock ? 'default' : 'destructive'}>
                                                    {product.isInStock ? `موجود (${product.stockQuantity})` : 'ناموجود'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell className="text-end">
                                                <DropdownMenu>
                                                    <DropdownMenuTrigger asChild>
                                                        <Button variant="ghost" size="icon"><MoreHorizontal /></Button>
                                                    </DropdownMenuTrigger>
                                                    <DropdownMenuContent align="end">
                                                        <DropdownMenuItem onClick={() => navigate(`/admin/products/${product.id}`)}>
                                                            <Eye /> مشاهده
                                                        </DropdownMenuItem>
                                                        {canUpdate && (
                                                            <DropdownMenuItem onClick={() => navigate(`/admin/products/${product.id}/edit`)}>
                                                                <Pencil /> ویرایش
                                                            </DropdownMenuItem>
                                                        )}
                                                        <DropdownMenuSeparator />
                                                        {canStatus && (
                                                            product.isActive ? (
                                                                <DropdownMenuItem onClick={() => run(toggleActive.mutateAsync({ id: product.id, isActive: false }), 'محصول غیرفعال شد.')}>
                                                                    <PowerOff /> غیرفعال‌سازی
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem onClick={() => run(toggleActive.mutateAsync({ id: product.id, isActive: true }), 'محصول فعال شد.')}>
                                                                    <Power /> فعال‌سازی
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        <DropdownMenuItem onClick={() => run(toggleFeatured.mutateAsync({ id: product.id, isFeatured: !product.isFeatured }), product.isFeatured ? 'محصول از ویژه خارج شد.' : 'محصول ویژه شد.')}>
                                                            {product.isFeatured ? <StarOff /> : <Star />}
                                                            {product.isFeatured ? 'لغو ویژه' : 'ویژه'}
                                                        </DropdownMenuItem>
                                                        {canDelete && (
                                                            <>
                                                                <DropdownMenuSeparator />
                                                                <DropdownMenuItem className="text-destructive" onClick={() => setConfirm({ type: 'delete', product })}>
                                                                    <Trash2 /> حذف
                                                                </DropdownMenuItem>
                                                            </>
                                                        )}
                                                    </DropdownMenuContent>
                                                </DropdownMenu>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>

                            <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
                                <div className="text-muted-foreground text-sm">
                                    {query.data.total} کل · صفحه {query.data.page} از {Math.max(query.data.totalPages, 1)}
                                </div>
                                <div className="flex gap-2">
                                    <Button
                                        size="sm"
                                        variant="outline"
                                        disabled={query.data.page <= 1}
                                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                                    >
                                        قبلی
                                    </Button>
                                    <Button
                                        size="sm"
                                        variant="outline"
                                        disabled={query.data.page >= query.data.totalPages}
                                        onClick={() => setPage((p) => p + 1)}
                                    >
                                        بعدی
                                    </Button>
                                </div>
                            </div>
                        </>
                    )}
                </CardContent>
            </Card>

            <ConfirmDialog
                open={!!confirm}
                onOpenChange={(open) => !open && setConfirm(null)}
                title={`حذف ${confirm?.product?.name}؟`}
                description="محصول به صورت نرم حذف می‌شود. قابل بازیابی توسط مدیران سیستم."
                confirmLabel="حذف محصول"
                destructive
                pending={deleteMutation.isPending}
                onConfirm={onConfirm}
            />
        </div>
    );
}