import { useNavigate } from 'react-router-dom';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { PageHeader } from '@/components/data-states';
import { ProductForm } from '@/modules/catalog/products/components/ProductForm';
import { useCreateProduct } from '@/modules/catalog/products/hooks';
import type { CreateProductDto } from '@/modules/catalog/api/products';

export default function NewProductPage() {
    const navigate = useNavigate();
    const createProduct = useCreateProduct();
    const handleSubmit = async (body: CreateProductDto) => {
        const product = await createProduct.mutateAsync({
            ...body,
            variants: body.variants.map((variant) => ({ ...variant, stockQuantity: 0 })),
        });
        navigate(`/admin/products/${product.id}`);
    };

    return (
        <div className="space-y-6">
            <PageHeader title="Create Product" description="Create the product and its variants once in the catalog. Warehouse quantities are managed separately for the existing variants." />
            <Alert><AlertDescription>ثبت محصول فقط یک‌بار در کاتالوگ انجام می‌شود. پس از ایجاد محصول، از بخش «موجودی» همان Variant را به انبار و موقعیت اختصاص دهید؛ هیچ محصول دومی برای انبار ساخته نمی‌شود.</AlertDescription></Alert>
            <ProductForm mode="create" pending={createProduct.isPending} onSubmit={handleSubmit} onCancel={() => navigate('/admin/products')} />
        </div>
    );
}
