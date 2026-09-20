import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { PageHeader } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import WarehouseForm, {
  type WarehouseFormSubmitBody,
} from '@/modules/inventory/components/WarehouseForm';
import { useCreateWarehouse } from '@/modules/inventory/hooks/useWarehouses';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function NewWarehousePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  useDocumentTitle(t('warehouses.createTitle'));

  const mutation = useCreateWarehouse();

  const submit = async (body: WarehouseFormSubmitBody) => {
    await mutation.mutateAsync(body);
    toast.success(t('warehouses.created'));
    navigate('/admin/warehouses');
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('warehouses.createTitle')}
        description={t('warehouses.createDescription')}
        actions={
          <Button
            variant="outline"
            onClick={() => navigate('/admin/warehouses')}
          >
            <ArrowLeft />
            {t('warehouses.back')}
          </Button>
        }
      />

      <WarehouseForm
        mode="create"
        pending={mutation.isPending}
        onCancel={() => navigate('/admin/warehouses')}
        onSubmit={submit}
      />
    </div>
  );
}
