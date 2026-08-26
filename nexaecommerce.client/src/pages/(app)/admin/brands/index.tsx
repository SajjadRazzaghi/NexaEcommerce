import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router';
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
import type { BrandListItem } from '@/modules/catalog/api/brands';
import { useBrandAction, useBrands } from '@/modules/catalog/brands/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function BrandsPage() {
  useDocumentTitle('Brands');
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<'all' | 'active' | 'inactive' | 'published' | 'featured'>('all');
  const [confirm, setConfirm] = useState<{ type: 'delete'; brand: BrandListItem } | null>(null);

  const canCreate = usePermission(PERM.brandsCreate);
  const canUpdate = usePermission(PERM.brandsUpdate);
  const canDelete = usePermission(PERM.brandsDelete);
  const canStatus = usePermission(PERM.brandsStatus);
  const canPublish = usePermission(PERM.brandsPublish);
  const canFeature = usePermission(PERM.brandsFeature);

  const queryFilter = useMemo(() => ({
    page, pageSize: 20, search,
    isActive: filter === 'active' ? true : filter === 'inactive' ? false : undefined,
    isPublished: filter === 'published' ? true : undefined,
    isFeatured: filter === 'featured' ? true : undefined,
    sortBy: 'name', desc: false,
  }), [page, search, filter]);

  const query = useBrands(queryFilter);
  const action = useBrandAction('remove');
  const activateAction = useBrandAction('activate');
  const deactivateAction = useBrandAction('deactivate');
  const publishAction = useBrandAction('publish');
  const unpublishAction = useBrandAction('unpublish');
  const featureAction = useBrandAction('feature');
  const unfeatureAction = useBrandAction('unfeature');

  const run = async (promise: Promise<unknown>, success: string) => {
    try { await promise; toast.success(success); } catch (e) { toast.error(e instanceof Error ? e.message : 'Action failed.'); }
  };

  const onConfirm = async () => {
    if (!confirm) return;
    if (confirm.type === 'delete') await run(action.mutateAsync(confirm.brand.id), 'Brand deleted.');
    setConfirm(null);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Brands"
        description="Manage catalog brands, publishing state, merchandising and SEO."
        actions={canCreate ? <Button asChild><Link to="/admin/brands/new"><Plus /> New brand</Link></Button> : null}
      />

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="relative w-full md:max-w-md">
              <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />
              <Input className="ps-9" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="Search brands…" />
            </div>
            <div className="flex flex-wrap gap-2">
              {(['all', 'active', 'inactive', 'published', 'featured'] as const).map((item) => (
                <Button key={item} size="sm" variant={filter === item ? 'default' : 'outline'} onClick={() => { setFilter(item); setPage(1); }}>
                  {item[0].toUpperCase() + item.slice(1)}
                </Button>
              ))}
            </div>
          </div>

          {query.isLoading && <LoadingSkeleton variant="table" rows={8} cols={6} />}
          {query.isError && <ErrorState error={query.error} onRetry={() => query.refetch()} message="We couldn't load brands." />}
          {query.isSuccess && query.data.items.length === 0 && <EmptyState icon={Globe2} title="No brands found" description={search ? 'Try a different search.' : 'Create your first brand to start building the catalog.'} action={canCreate ? <Button asChild><Link to="/admin/brands/new"><Plus /> New brand</Link></Button> : undefined} />}

          {query.isSuccess && query.data.items.length > 0 && (
            <>
              <Table>
                <TableHeader><TableRow>
                  <TableHead>Brand</TableHead><TableHead>Slug</TableHead><TableHead>Status</TableHead><TableHead>Publishing</TableHead><TableHead>Featured</TableHead><TableHead className="text-end">Actions</TableHead>
                </TableRow></TableHeader>
                <TableBody>
                  {query.data.items.map((brand) => (
                    <TableRow key={brand.id}>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <div className="bg-muted grid size-10 shrink-0 place-items-center overflow-hidden rounded-lg border">
                            {brand.logoUrl ? <img src={brand.logoUrl} alt="" className="size-full object-contain" /> : <Globe className="text-muted-foreground size-5" />}
                          </div>
                          <div className="min-w-0"><div className="font-medium truncate max-w-56">{brand.name}</div><div className="text-muted-foreground text-xs">Order {brand.displayOrder}</div></div>
                        </div>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{brand.slug}</TableCell>
                      <TableCell><Badge variant={brand.isActive ? 'default' : 'secondary'}>{brand.isActive ? 'Active' : 'Inactive'}</Badge></TableCell>
                      <TableCell><Badge variant={brand.isPublished ? 'default' : 'outline'}>{brand.isPublished ? 'Published' : 'Draft'}</Badge></TableCell>
                      <TableCell><Badge variant={brand.isFeatured ? 'default' : 'outline'}>{brand.isFeatured ? 'Featured' : 'Normal'}</Badge></TableCell>
                      <TableCell className="text-end">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild><Button variant="ghost" size="icon"><MoreHorizontal /></Button></DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => navigate(`/admin/brands/${brand.id}`)}><Eye /> View</DropdownMenuItem>
                            {canUpdate && <DropdownMenuItem onClick={() => navigate(`/admin/brands/${brand.id}/edit`)}><Pencil /> Edit</DropdownMenuItem>}
                            <DropdownMenuSeparator />
                            {canStatus && (brand.isActive ? <DropdownMenuItem onClick={() => run(deactivateAction.mutateAsync(brand.id), 'Brand deactivated.')}><PowerOff /> Deactivate</DropdownMenuItem> : <DropdownMenuItem onClick={() => run(activateAction.mutateAsync(brand.id), 'Brand activated.')}><Power /> Activate</DropdownMenuItem>)}
                            {canPublish && (brand.isPublished ? <DropdownMenuItem onClick={() => run(unpublishAction.mutateAsync(brand.id), 'Brand unpublished.')}><Globe2 /> Unpublish</DropdownMenuItem> : <DropdownMenuItem disabled={!brand.isActive} onClick={() => run(publishAction.mutateAsync(brand.id), 'Brand published.')}><Globe /> Publish</DropdownMenuItem>)}
                            {canFeature && (brand.isFeatured ? <DropdownMenuItem onClick={() => run(unfeatureAction.mutateAsync(brand.id), 'Brand unfeatured.')}><StarOff /> Unfeature</DropdownMenuItem> : <DropdownMenuItem disabled={!brand.isActive} onClick={() => run(featureAction.mutateAsync(brand.id), 'Brand featured.')}><Star /> Feature</DropdownMenuItem>)}
                            {canDelete && <DropdownMenuItem className="text-destructive" onClick={() => setConfirm({ type: 'delete', brand })}><Trash2 /> Delete</DropdownMenuItem>}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
                <div className="text-muted-foreground text-sm">{query.data.totalItems} total · Page {query.data.page} of {Math.max(query.data.totalPages, 1)}</div>
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" disabled={!query.data.hasPrev} onClick={() => setPage((p) => Math.max(1, p - 1))}>Previous</Button>
                  <Button size="sm" variant="outline" disabled={!query.data.hasNext} onClick={() => setPage((p) => p + 1)}>Next</Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      <ConfirmDialog
        open={!!confirm}
        onOpenChange={(open) => !open && setConfirm(null)}
        title={`Delete ${confirm?.brand.name}?`}
        description="The brand will be soft-deleted. It can be restored later by an authorized administrator."
        confirmLabel="Delete brand"
        destructive
        pending={action.isPending}
        onConfirm={onConfirm}
      />
    </div>
  );
}
