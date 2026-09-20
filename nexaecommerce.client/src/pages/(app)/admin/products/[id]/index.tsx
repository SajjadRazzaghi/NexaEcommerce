import { useParams } from 'react-router';
import ProductDetailPage from '@/modules/catalog/products/pages/ProductDetailPage';
import ProductInventoryManager from '@/modules/inventory/components/ProductInventoryManager';
import { usePermission } from '@/hooks/use-permission';
import { INVENTORY_PERM } from '@/lib/api/inventory';

export default function ProductDetailRoute() {
    const { id } = useParams<{ id: string }>();
    const canManageInventory = usePermission(INVENTORY_PERM.manage);
    return (
        <div className="space-y-6">
            <ProductDetailPage />
            {id && canManageInventory && <ProductInventoryManager productId={id} />}
        </div>
    );
}
