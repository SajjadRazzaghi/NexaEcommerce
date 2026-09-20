import { useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { INVENTORY_PERM } from '@/lib/api/inventory';
import { usePermission } from '@/hooks/use-permission';
import { useUpdateWarehouse, useWarehouses } from '@/modules/inventory/hooks/useWarehouses';
import WarehouseForm, { type WarehouseFormSubmitBody } from '@/modules/inventory/components/WarehouseForm';
import WarehouseLocationsManager from '@/modules/inventory/components/WarehouseLocationsManager';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function EditWarehousePage() {
    const { t } = useTranslation();
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    useDocumentTitle(t('warehouses.editTitle'));

    const canManage = usePermission(INVENTORY_PERM.manage);
    const query = useWarehouses(true);
    const mutation = useUpdateWarehouse();
    const warehouse = query.data?.find((item) => item.id === id);

    if (!canManage) {
        return <ErrorState error={new Error(t('warehouses.permissionError'))} onRetry={() => undefined} message={t('warehouses.permissionError')} />;
    }
    if (query.isLoading) return <LoadingSkeleton variant="cards" rows={3} />;
    if (query.isError || !warehouse) {
        return <ErrorState error={query.error} onRetry={() => query.refetch()} message={t('warehouses.loadError')} />;
    }

    const submit = async (body: WarehouseFormSubmitBody) => {
        await mutation.mutateAsync({ id: warehouse.id, body });
        toast.success(t('warehouses.updated'));
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title={t('warehouses.editTitle')}
                description={warehouse.name}
                actions={<Button variant="outline" onClick={() => navigate('/admin/warehouses')}><ArrowLeft />{t('warehouses.back')}</Button>}
            />
            <WarehouseForm mode="edit" warehouse={warehouse} pending={mutation.isPending} onCancel={() => navigate('/admin/warehouses')} onSubmit={submit} />
            <WarehouseLocationsManager warehouseId={warehouse.id} />
        </div>
    );
}
