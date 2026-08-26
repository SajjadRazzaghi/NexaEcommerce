import { useNavigate, useParams } from 'react-router-dom';
import { Loader2 } from 'lucide-react';

import { ManufacturerForm } from '@/modules/catalog/manufacturers/components/ManufacturerForm';
import { useManufacturer, useUpdateManufacturer } from '@/modules/catalog/manufacturers/hooks';
import type { UpdateManufacturerDto } from '@/modules/catalog/api/manufacturers';

export default function EditManufacturerPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const query = useManufacturer(id);
  const mutation = useUpdateManufacturer();

  if (!id || query.isLoading) {
    return <div className="flex min-h-64 items-center justify-center"><Loader2 className="animate-spin" /></div>;
  }

  if (query.isError || !query.data) {
    return <div className="p-6 text-destructive">Manufacturer not found.</div>;
  }

  return (
    <div className="p-6">
      <ManufacturerForm
        manufacturer={query.data}
        mode="edit"
        pending={mutation.isPending}
        onSubmit={(body) => mutation.mutateAsync({ id, body: body as UpdateManufacturerDto })}
        onCancel={() => navigate('/admin/manufacturers')}
      />
    </div>
  );
}
