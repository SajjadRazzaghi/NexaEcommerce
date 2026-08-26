import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Loader2, Pencil, Plus, Search, Trash2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { ManufacturerStatusBadge } from '@/modules/catalog/manufacturers/components/ManufacturerStatusBadge';
import { useManufacturerAction, useManufacturers } from '@/modules/catalog/manufacturers/hooks';

export default function ManufacturersPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const filter = useMemo(() => ({ page, pageSize, search, sortBy: 'displayOrder', desc: false }), [page, search]);
  const query = useManufacturers(filter);
  const remove = useManufacturerAction('remove');

  const data = query.data as any;
  const items = data?.items ?? data?.data ?? [];
  const total = data?.totalCount ?? data?.total ?? items.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div className="space-y-6 p-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold">Manufacturers</h1>
          <p className="text-muted-foreground">Manage product manufacturers and their storefront visibility.</p>
        </div>
        <Button asChild>
          <Link to="/admin/manufacturers/new"><Plus /> Add manufacturer</Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Manufacturer list</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="relative max-w-md">
            <Search className="text-muted-foreground absolute left-3 top-2.5 h-4 w-4" />
            <Input value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="Search manufacturers..." className="pl-9" />
          </div>

          {query.isLoading ? (
            <div className="flex min-h-48 items-center justify-center"><Loader2 className="animate-spin" /></div>
          ) : query.isError ? (
            <div className="rounded-lg border border-destructive/30 p-6 text-destructive">Failed to load manufacturers.</div>
          ) : items.length === 0 ? (
            <div className="rounded-lg border p-10 text-center text-muted-foreground">No manufacturers found.</div>
          ) : (
            <div className="space-y-3">
              {items.map((manufacturer: any) => (
                <div key={manufacturer.id} className="grid gap-4 rounded-xl border p-4 md:grid-cols-[56px_1fr_auto] md:items-center">
                  <div className="flex h-14 w-14 items-center justify-center overflow-hidden rounded-lg bg-muted">
                    {manufacturer.logoUrl ? <img src={manufacturer.logoUrl} alt={manufacturer.name} className="h-full w-full object-contain" /> : <span className="text-xs">No logo</span>}
                  </div>
                  <div className="min-w-0 space-y-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold">{manufacturer.name}</span>
                      <ManufacturerStatusBadge active={manufacturer.isActive} label={manufacturer.isActive ? 'Active' : 'Inactive'} />
                      {manufacturer.isPublished && <ManufacturerStatusBadge active label="Published" />}
                      {manufacturer.isFeatured && <ManufacturerStatusBadge active label="Featured" />}
                    </div>
                    <div className="text-muted-foreground text-sm">/{manufacturer.slug} · {manufacturer.productCount ?? 0} products · order {manufacturer.displayOrder ?? 0}</div>
                  </div>
                  <div className="flex gap-2 md:justify-end">
                    <Button variant="outline" size="sm" asChild>
                      <Link to={`/admin/manufacturers/${manufacturer.id}/edit`}><Pencil /> Edit</Link>
                    </Button>
                    <Button variant="destructive" size="sm" disabled={remove.isPending} onClick={() => {
                      if (window.confirm(`Delete ${manufacturer.name}?`)) remove.mutate(manufacturer.id);
                    }}><Trash2 /> Delete</Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <Button variant="outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <span className="text-sm text-muted-foreground">Page {page} of {totalPages}</span>
              <Button variant="outline" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
