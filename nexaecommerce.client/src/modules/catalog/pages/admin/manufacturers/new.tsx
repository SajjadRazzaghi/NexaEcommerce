import { useNavigate } from 'react-router-dom';
import { ManufacturerForm } from '@/modules/catalog/manufacturers/components/ManufacturerForm';
import { useCreateManufacturer } from '@/modules/catalog/manufacturers/hooks';
import type { CreateManufacturerDto } from '@/modules/catalog/api/manufacturers';

export default function NewManufacturerPage() {
  const navigate = useNavigate();
  const mutation = useCreateManufacturer();

  return (
    <div className="p-6">
      <ManufacturerForm
        mode="create"
        pending={mutation.isPending}
        onSubmit={(body) => mutation.mutateAsync(body as CreateManufacturerDto)}
        onCancel={() => navigate('/admin/manufacturers')}
      />
    </div>
  );
}
