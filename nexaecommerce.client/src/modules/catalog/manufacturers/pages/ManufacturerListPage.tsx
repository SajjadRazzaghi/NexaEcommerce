// src/modules/catalog/manufacturers/pages/ManufacturerListPage.tsx
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Plus, Search, MoreHorizontal, Eye, Pencil, Trash2, Power, PowerOff, Star, StarOff, Globe, Globe2 } from 'lucide-react';
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
import type { ManufacturerListItem } from '@/modules/catalog/api/manufacturers';
import { useManufacturerAction, useManufacturers } from '@/modules/catalog/manufacturers/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function ManufacturerListPage() {
    useDocumentTitle('Manufacturers');
    const navigate = useNavigate();
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [filter, setFilter] = useState<'all' | 'active' | 'inactive' | 'published' | 'featured'>('all');
    const [confirm, setConfirm] = useState<{ type: 'delete'; manufacturer: ManufacturerListItem } | null>(null);

    // ✅ استفاده از Permissions صحیح
    const canCreate = usePermission(PERM.manufacturersCreate);
    const canUpdate = usePermission(PERM.manufacturersUpdate);
    const canDelete = usePermission(PERM.manufacturersDelete);
    const canStatus = usePermission(PERM.manufacturersStatus);
    const canPublish = usePermission(PERM.manufacturersPublish);
    const canFeature = usePermission(PERM.manufacturersFeature);

    
    const queryFilter = useMemo(() => ({
        page, pageSize: 20, search,
        isActive: filter === 'active' ? true : filter === 'inactive' ? false : undefined,
        isPublished: filter === 'published' ? true : undefined,
        isFeatured: filter === 'featured' ? true : undefined,
        sortBy: 'name', desc: false,
    }), [page, search, filter]);

    const query = useManufacturers(queryFilter);
    const action = useManufacturerAction('remove');
    const activateAction = useManufacturerAction('activate');
    const deactivateAction = useManufacturerAction('deactivate');
    const publishAction = useManufacturerAction('publish');
    const unpublishAction = useManufacturerAction('unpublish');
    const featureAction = useManufacturerAction('feature');
    const unfeatureAction = useManufacturerAction('unfeature');

    const run = async (promise: Promise<unknown>, success: string) => {
        try { await promise; toast.success(success); } catch (e) { toast.error(e instanceof Error ? e.message : 'Action failed.'); }
    };

    const onConfirm = async () => {
        if (!confirm) return;
        if (confirm.type === 'delete') await run(action.mutateAsync(confirm.manufacturer.id), 'Manufacturer deleted.');
        setConfirm(null);
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title="تولیدکنندگان"
                description="مدیریت تولیدکنندگان، وضعیت انتشار، نمایش و SEO"
                actions={canCreate ? <Button asChild><Link to="/admin/manufacturers/new"><Plus /> تولیدکننده جدید</Link></Button> : null}
            />

            <Card>
                <CardContent className="space-y-4 pt-6">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                        <div className="relative w-full md:max-w-md">
                            <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />
                            <Input className="ps-9" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="جستجوی تولیدکنندگان…" />
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
                    {query.isError && <ErrorState error={query.error} onRetry={() => query.refetch()} message="بارگذاری تولیدکنندگان با خطا مواجه شد." />}
                    {query.isSuccess && query.data.items.length === 0 && <EmptyState icon={Globe2} title="تولیدکننده‌ای یافت نشد" description={search ? 'جستجوی دیگری انجام دهید.' : 'اولین تولیدکننده خود را ایجاد کنید.'} action={canCreate ? <Button asChild><Link to="/admin/manufacturers/new"><Plus /> تولیدکننده جدید</Link></Button> : undefined} />}

                    {query.isSuccess && query.data.items.length > 0 && (
                        <>
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>تولیدکننده</TableHead>
                                        <TableHead>Slug</TableHead>
                                        <TableHead>وضعیت</TableHead>
                                        <TableHead>انتشار</TableHead>
                                        <TableHead>ویژه</TableHead>
                                        <TableHead className="text-end">عملیات</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {query.data.items.map((manufacturer) => (
                                        <TableRow key={manufacturer.id}>
                                            <TableCell>
                                                <div className="flex items-center gap-3">
                                                    <div className="bg-muted grid size-10 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                                        {manufacturer.logoUrl ? <img src={manufacturer.logoUrl} alt="" className="size-full object-contain" /> : <Globe className="text-muted-foreground size-5" />}
                                                    </div>
                                                    <div className="min-w-0">
                                                        <div className="font-medium truncate max-w-56">{manufacturer.name}</div>
                                                        <div className="text-muted-foreground text-xs">ترتیب {manufacturer.displayOrder}</div>
                                                    </div>
                                                </div>
                                            </TableCell>
                                            <TableCell className="text-muted-foreground">{manufacturer.slug || '—'}</TableCell>
                                            <TableCell>
                                                <Badge variant={manufacturer.isActive ? 'default' : 'secondary'}>
                                                    {manufacturer.isActive ? 'فعال' : 'غیرفعال'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={manufacturer.isPublished ? 'default' : 'outline'}>
                                                    {manufacturer.isPublished ? 'منتشر شده' : 'پیش‌نویس'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={manufacturer.isFeatured ? 'default' : 'outline'}>
                                                    {manufacturer.isFeatured ? 'ویژه' : 'عادی'}
                                                </Badge>
                                            </TableCell>
                                            <TableCell className="text-end">
                                                <DropdownMenu>
                                                    <DropdownMenuTrigger asChild>
                                                        <Button variant="ghost" size="icon"><MoreHorizontal /></Button>
                                                    </DropdownMenuTrigger>
                                                    <DropdownMenuContent align="end">
                                                        <DropdownMenuItem onClick={() => navigate(`/admin/manufacturers/${manufacturer.id}`)}>
                                                            <Eye /> مشاهده
                                                        </DropdownMenuItem>
                                                        {canUpdate && (
                                                            <DropdownMenuItem onClick={() => navigate(`/admin/manufacturers/${manufacturer.id}/edit`)}>
                                                                <Pencil /> ویرایش
                                                            </DropdownMenuItem>
                                                        )}
                                                        <DropdownMenuSeparator />
                                                        {canStatus && (
                                                            manufacturer.isActive ? (
                                                                <DropdownMenuItem onClick={() => run(deactivateAction.mutateAsync(manufacturer.id), 'تولیدکننده غیرفعال شد.')}>
                                                                    <PowerOff /> غیرفعال‌سازی
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem onClick={() => run(activateAction.mutateAsync(manufacturer.id), 'تولیدکننده فعال شد.')}>
                                                                    <Power /> فعال‌سازی
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        {canPublish && (
                                                            manufacturer.isPublished ? (
                                                                <DropdownMenuItem onClick={() => run(unpublishAction.mutateAsync(manufacturer.id), 'تولیدکننده از انتشار خارج شد.')}>
                                                                    <Globe2 /> لغو انتشار
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem disabled={!manufacturer.isActive} onClick={() => run(publishAction.mutateAsync(manufacturer.id), 'تولیدکننده منتشر شد.')}>
                                                                    <Globe /> انتشار
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        {canFeature && (
                                                            manufacturer.isFeatured ? (
                                                                <DropdownMenuItem onClick={() => run(unfeatureAction.mutateAsync(manufacturer.id), 'تولیدکننده از ویژه خارج شد.')}>
                                                                    <StarOff /> لغو ویژه
                                                                </DropdownMenuItem>
                                                            ) : (
                                                                <DropdownMenuItem disabled={!manufacturer.isActive} onClick={() => run(featureAction.mutateAsync(manufacturer.id), 'تولیدکننده ویژه شد.')}>
                                                                    <Star /> ویژه
                                                                </DropdownMenuItem>
                                                            )
                                                        )}
                                                        {canDelete && (
                                                            <>
                                                                <DropdownMenuSeparator />
                                                                <DropdownMenuItem className="text-destructive" onClick={() => setConfirm({ type: 'delete', manufacturer })}>
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
                                    {query.data.totalItems} کل · صفحه {query.data.page} از {Math.max(query.data.totalPages, 1)}
                                </div>
                                <div className="flex gap-2">
                                    <Button size="sm" variant="outline" disabled={!query.data.hasPrev} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                                        قبلی
                                    </Button>
                                    <Button size="sm" variant="outline" disabled={!query.data.hasNext} onClick={() => setPage((p) => p + 1)}>
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
                title={`حذف ${confirm?.manufacturer.name}؟`}
                description="تولیدکننده به صورت نرم حذف می‌شود. قابل بازیابی توسط مدیران سیستم."
                confirmLabel="حذف تولیدکننده"
                destructive
                pending={action.isPending}
                onConfirm={onConfirm}
            />
        </div>
    );
}