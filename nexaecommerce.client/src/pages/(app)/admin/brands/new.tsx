import { useNavigate } from 'react-router';
import { ArrowLeft } from 'lucide-react';

import { PageHeader } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { BrandForm } from '@/modules/catalog/brands/components/BrandForm';
import { brandsApi } from '@/modules/catalog/api/brands';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function NewBrandPage() {
  useDocumentTitle('New Brand');
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <PageHeader title="New brand" description="Create a new catalog brand." actions={<Button variant="outline" onClick={() => navigate('/admin/brands')}><ArrowLeft /> Back to brands</Button>} />
      <BrandForm mode="create" onCancel={() => navigate('/admin/brands')} onSubmit={async (body) => { const result = await brandsApi.create(body); navigate(`/admin/brands/${result.id}`); return result; }} />
    </div>
  );
}
