// src/modules/catalog/categories/pages/CategoryListPage.tsx
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Plus, Search, MoreHorizontal, Eye, Pencil, Trash2, Power, PowerOff, Star, StarOff, Globe, Globe2, FolderTree } from 'lucide-react';
import { toast } from 'sonner';

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
import { useCategories, useCategoryAction, useDeleteCategory } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function CategoryListPage() {
    useDocumentTitle('دسته‌بندی‌ها');
    const navigate = useNavigate();
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [filter, setFilter] = useState<'all' | 'active' | 'inactive' | 'published' | 'featured'>('all');
    const [confirm, setConfirm] = useState<{ type: 'delete'; category: any } | null>(null);

    const canCreate = usePermission(PERM.categoriesCreate);
    const canUpdate = usePermission(PERM.categoriesUpdate);
    const canDelete = usePermission(PERM.categoriesDelete);
    const canStatus = usePermission(PERM.categoriesStatus);
    const canPublish = usePermission(PERM.categoriesPublish);
    const canFeature = usePermission(PERM.categoriesFeature);

    const queryFilter = useMemo(() => ({
        page, pageSize: 20, search,
        isActive: filter === 'active' ? true : filter === 'inactive' ? false : undefined,
        isPublished: filter === 'published' ? true : undefined,
        isFeatured: filter === 'featured' ? true : undefined,
        sortBy: 'displayOrder', desc: false,
    }), [page, search, filter]);

    const query = useCategories(queryFilter);
    const deleteMutation = useDeleteCategory();
    const activateAction = useCategoryAction('activate');
    const deactivateAction = useCategoryAction('deactivate');
    const publishAction = useCategoryAction('publish');
    const unpublishAction = useCategoryAction('unpublish');
    const featureAction = useCategoryAction('feature');
    const unfeatureAction = useCategoryAction('unfeature');

    const run = async (promise: Promise<unknown>, success: string) => {
        try { await promise; toast.success(success); } catch (e) { toast.error(e instanceof Error ? e.message : 'عملیات ناموفق بود.'); }
    };

    const onConfirm = async () => {
        if (!confirm) return;
        if (confirm.type === 'delete') await run(deleteMutation.mutateAsync(confirm.category.id), 'دسته‌بندی حذف شد.');
        setConfirm(null);
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title="دسته‌بندی‌ها"
                description="مدیریت دسته‌بندی‌های فروشگاه"
                actions={canCreate ? <Button asChild><Link to="/admin/categories/new"><Plus /> دسته‌بندی جدید</Link></Button> : null}
            />

            <Card>
                <CardContent className="space-y-4 pt-6">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                        <div className="relative w-full md:max-w-md">
                            <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />
                            <Input className="ps-9" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="جستجوی دسته‌بندی‌ها…" />
                        </div>
                        <div className="flex flex-wrap gap-2">
                            {(['all', 'active', 'inactive', 'published', 'featured'] as const).map((item) => (
                                <Button key={item} size="sm" variant={filter === item ? 'default' : 'outline'} onClick={() => { setFilter(item); setPage(1); }}>
                                    {item === 'all' && 'همه'}
                                    {item === 'active' && 'فعال'}
                                    {item === 'inactive' && 'غیرفعال'}
                                    {item === 'published' && 'منتشر شده'}
                                    {item === 'featured' && 'ویژه'}
                                </Button>
                            ))}
                        </div>
                    </div>

                    {query.isLoading && <LoadingSkeleton variant="table" rows={8} cols={6} />}
                    {query.isError && <ErrorState error={query.error} onRetry={() => query.refetch()} message="بارگذاری دسته‌بندی‌ها با خطا مواجه شد." />}
                    {query.isSuccess && query.data?.length === 0 && (
                        <EmptyState
                            icon={FolderTree}
                            title="دسته‌بندی‌ای یافت نشد"
                            description={search ? 'جستجوی دیگری انجام دهید.' : 'اولین دسته‌بندی را ایجاد کنید.'}
                            action={canCreate ? <Button asChild><Link to="/admin/categories/new"><Plus /> دسته‌بندی جدید</Link></Button> : undefined}
                        />
                    )}

                    {query.isSuccess && query.data?.length > 0 && (
                        <>
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>دسته‌بندی</TableHead>
                                        <TableHead>والد</TableHead>
                                        <TableHead>وضعیت</TableHead>
                                        <TableHead>انتشار</TableHead>
                                        <TableHead>ویژه</TableHead>
                                        <TableHead className="text-end">عملیات</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {query.data.map((category) => (
                                        <TableRow key={category.id}>
                                            <TableCell>
                                                <div className="flex items-center gap-3">
                                                    <div className="bg-muted grid size-10 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                                        {category.imageUrl ? <img src={category.imageUrl} alt="" className="size-full object-contain" /> : <FolderTree className="text-muted-foreground size-5" />}
                                                    </div>
                                                    <div className="min-w-0">
                                                        <div className="font-medium truncate max-w-56">{category.name}</div>
                                                        <div className="text-muted-foreground text-xs">ترتیب {category.displayOrder}</div>
                                                    </div>
                                                </div>
                                            </TableCell>
                                            <TableCell className="text-muted-foreground">
                                                {category.parentCategoryName || '—'}
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={category.isActive ? 'default' : 'secondary'}>
                                                    {category.isActive ? 'فعال' : 'غیرفعال'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={category.isPublished ? 'default' : 'outline'}>
                                                    {category.isPublished ? 'منتشر شده' : 'پیش‌نویس'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={category.isFeatured ? 'default' : 'outline'}>
                                                    {category.isFeatured ? 'ویژه' : 'عادی'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell className="text-end">
                                                <DropdownMenu>
                                                    <DropdownMenuTrigger asChild>
                                                        <Button variant="ghost" size="icon"><MoreHorizontal /></Button>
                                                    </DropdownMenuTrigger>
                                                    <DropdownMenuContent align="end">
                                                        <DropdownMenuItem onClick={() => navigate(`/admin/categories/${category.id}`)}>
                                                            <Eye /> مشاهده
                                                        </DropdownMenuItem>
                                                        {canUpdate && (
                                                            <DropdownMenuItem onClick={() => navigate(`/admin/categories/${category.id}/edit`)}>
                                                                <Pencil /> ویرایش
                                                            </DropdownMenuItem>
                                                        )}
                                                        <DropdownMenuSeparator />
                                                        {canStatus && (
                                                            category.isActive ? (
                                                                <DropdownMenuItem onClick={() => run(deactivateAction.mutateAsync(category.id), 'دسته‌بندی غیرفعال شد.')}>
                                                                    <PowerOff /> غیرفعال‌سازی
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem onClick={() => run(activateAction.mutateAsync(category.id), 'دسته‌بندی فعال شد.')}>
                                                                    <Power /> فعال‌سازی
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        {canPublish && (
                                                            category.isPublished ? (
                                                                <DropdownMenuItem onClick={() => run(unpublishAction.mutateAsync(category.id), 'دسته‌بندی از انتشار خارج شد.')}>
                                                                    <Globe2 /> لغو انتشار
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem disabled={!category.isActive} onClick={() => run(publishAction.mutateAsync(category.id), 'دسته‌بندی منتشر شد.')}>
                                                                    <Globe /> انتشار
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        {canFeature && (
                                                            category.isFeatured ? (
                                                                <DropdownMenuItem onClick={() => run(unfeatureAction.mutateAsync(category.id), 'دسته‌بندی از ویژه خارج شد.')}>
                                                                    <StarOff /> لغو ویژه
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem disabled={!category.isActive} onClick={() => run(featureAction.mutateAsync(category.id), 'دسته‌بندی ویژه شد.')}>
                                                                    <Star /> ویژه
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        {canDelete && (
                                                            <>
                                                                <DropdownMenuSeparator />
                                                                <DropdownMenuItem className="text-destructive" onClick={() => setConfirm({ type: 'delete', category })}>
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
                                    {query.data?.length || 0} کل · صفحه {page}
                                </div>
                                <div className="flex gap-2">
                                    <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                                        قبلی
                                    </Button>
                                    <Button size="sm" variant="outline" disabled={query.data?.length < 20} onClick={() => setPage((p) => p + 1)}>
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
                title={`حذف ${confirm?.category.name}؟`}
                description="دسته‌بندی به صورت نرم حذف می‌شود. قابل بازیابی توسط مدیران سیستم."
                confirmLabel="حذف دسته‌بندی"
                destructive
                pending={deleteMutation.isPending}
                onConfirm={onConfirm}
            />
        </div>
    );
}