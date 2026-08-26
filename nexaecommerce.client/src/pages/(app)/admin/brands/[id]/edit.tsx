import { useNavigate, useParams } from 'react-router';
import { ArrowLeft } from 'lucide-react';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { BrandForm } from '@/modules/catalog/brands/components/BrandForm';
import { brandsApi } from '@/modules/catalog/api/brands';
import { useBrand, useUpdateBrand } from '@/modules/catalog/brands/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function EditBrandPage() {
  useDocumentTitle('Edit Brand');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const query = useBrand(id);
  const mutation = useUpdateBrand();

  if (query.isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
  if (query.isError || !query.data) return <ErrorState error={query.error} onRetry={() => query.refetch()} message="We couldn't load this brand." />;

  return (
    <div className="space-y-6">
      <PageHeader title={`Edit ${query.data.name}`} description="Update brand information, SEO and publishing state." actions={<Button variant="outline" onClick={() => navigate(`/admin/brands/${id}`)}><ArrowLeft /> Back</Button>} />
      <BrandForm
        mode="edit"
        brand={query.data}
        pending={mutation.isPending}
        onCancel={() => navigate(`/admin/brands/${id}`)}
        onSubmit={async (body) => mutation.mutateAsync({ id: id!, body: body as Parameters<typeof brandsApi.update>[1] })}
      />
    </div>
  );
}
