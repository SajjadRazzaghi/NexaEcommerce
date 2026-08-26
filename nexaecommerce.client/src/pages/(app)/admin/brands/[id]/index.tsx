import { useNavigate, useParams } from 'react-router';
import { ArrowLeft, Pencil, Globe, Star, Power, PowerOff, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useBrand, useBrandAction } from '@/modules/catalog/brands/hooks';
import { usePermission } from '@/hooks/use-permission';
import { PERM } from '@/lib/api/admin';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function BrandDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const query = useBrand(id);
  const activate = useBrandAction('activate');
  const deactivate = useBrandAction('deactivate');
  const publish = useBrandAction('publish');
  const unpublish = useBrandAction('unpublish');
  const feature = useBrandAction('feature');
  const unfeature = useBrandAction('unfeature');
  const remove = useBrandAction('remove');

  const canUpdate = usePermission(PERM.brandsUpdate);
  const canDelete = usePermission(PERM.brandsDelete);
  const canStatus = usePermission(PERM.brandsStatus);
  const canPublish = usePermission(PERM.brandsPublish);
  const canFeature = usePermission(PERM.brandsFeature);

  useDocumentTitle(query.data?.name ?? 'Brand');

  if (query.isLoading) return <LoadingSkeleton variant="cards" rows={3} />;
  if (query.isError || !query.data) return <ErrorState error={query.error} onRetry={() => query.refetch()} message="We couldn't load this brand." />;

  const brand = query.data;
  const run = async (promise: Promise<unknown>, message: string) => {
    try { await promise; toast.success(message); } catch (e) { toast.error(e instanceof Error ? e.message : 'Action failed.'); }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={brand.name}
        description={brand.slug}
        actions={<div className="flex gap-2"><Button variant="outline" onClick={() => navigate('/admin/brands')}><ArrowLeft /> Brands</Button>{canUpdate && <Button onClick={() => navigate(`/admin/brands/${brand.id}/edit`)}><Pencil /> Edit</Button>}</div>}
      />

      <div className="grid gap-6 lg:grid-cols-[1.4fr_0.6fr]">
        <Card><CardHeader><CardTitle>Brand information</CardTitle><CardDescription>Current catalog and SEO data.</CardDescription></CardHeader><CardContent className="space-y-6">
          <div className="flex flex-wrap gap-2"><Badge variant={brand.isActive ? 'default' : 'secondary'}>{brand.isActive ? 'Active' : 'Inactive'}</Badge><Badge variant={brand.isPublished ? 'default' : 'outline'}>{brand.isPublished ? 'Published' : 'Draft'}</Badge><Badge variant={brand.isFeatured ? 'default' : 'outline'}>{brand.isFeatured ? 'Featured' : 'Normal'}</Badge></div>
          <dl className="grid gap-4 sm:grid-cols-2">
            <Info label="Name" value={brand.name} /><Info label="Slug" value={brand.slug} /><Info label="Website" value={brand.website} href={brand.website} /><Info label="Display order" value={String(brand.displayOrder)} />
            <Info label="SEO title" value={brand.seoTitle} /><Info label="SEO description" value={brand.seoDescription} /><Info label="SEO keywords" value={brand.seoKeywords} />
            <Info label="Created" value={new Date(brand.createdAt).toLocaleString()} /><Info label="Updated" value={brand.updatedAt ? new Date(brand.updatedAt).toLocaleString() : '—'} />
          </dl>
          <div><h3 className="font-medium">Description</h3><p className="text-muted-foreground mt-2 whitespace-pre-wrap">{brand.description || 'No description.'}</p></div>
        </CardContent></Card>

        <Card><CardHeader><CardTitle>Actions</CardTitle></CardHeader><CardContent className="space-y-2">
          {canStatus && (brand.isActive ? <Button className="w-full justify-start" variant="outline" onClick={() => run(deactivate.mutateAsync(brand.id), 'Brand deactivated.')}><PowerOff /> Deactivate</Button> : <Button className="w-full justify-start" variant="outline" onClick={() => run(activate.mutateAsync(brand.id), 'Brand activated.')}><Power /> Activate</Button>)}
          {canPublish && (brand.isPublished ? <Button className="w-full justify-start" variant="outline" onClick={() => run(unpublish.mutateAsync(brand.id), 'Brand unpublished.')}><Globe /> Unpublish</Button> : <Button className="w-full justify-start" variant="outline" disabled={!brand.isActive} onClick={() => run(publish.mutateAsync(brand.id), 'Brand published.')}><Globe /> Publish</Button>)}
          {canFeature && (brand.isFeatured ? <Button className="w-full justify-start" variant="outline" onClick={() => run(unfeature.mutateAsync(brand.id), 'Brand unfeatured.')}><Star /> Unfeature</Button> : <Button className="w-full justify-start" variant="outline" disabled={!brand.isActive} onClick={() => run(feature.mutateAsync(brand.id), 'Brand featured.')}><Star /> Feature</Button>)}
          {canDelete && <Button className="w-full justify-start" variant="destructive" onClick={() => run(remove.mutateAsync(brand.id), 'Brand deleted.').then(() => navigate('/admin/brands'))}><Trash2 /> Delete</Button>}
        </CardContent></Card>
      </div>
    </div>
  );
}

function Info({ label, value, href }: { label: string; value?: string | null; href?: string | null }) {
  return <div className="rounded-lg border p-3"><dt className="text-muted-foreground text-xs">{label}</dt><dd className="mt-1 break-words text-sm">{href && value ? <a className="text-primary underline" href={href} target="_blank" rel="noreferrer">{value}</a> : value || '—'}</dd></div>;
}
