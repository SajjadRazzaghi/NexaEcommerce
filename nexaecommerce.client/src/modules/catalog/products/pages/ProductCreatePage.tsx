// src/modules/catalog/products/pages/ProductCreatePage.tsx
import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

import { PageHeader } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { ProductForm } from '../components/ProductForm';
import { useCreateProduct } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function ProductCreatePage() {
    useDocumentTitle('ایجاد محصول جدید');
    const navigate = useNavigate();
    const mutation = useCreateProduct();

    const handleSubmit = async (body: any) => {
        await mutation.mutateAsync(body);
        navigate('/admin/products');
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title="ایجاد محصول جدید"
                description="اطلاعات محصول جدید را وارد کنید."
                actions={
                    <Button variant="outline" onClick={() => navigate('/admin/products')}>
                        <ArrowLeft /> بازگشت به لیست
                    </Button>
                }
            />
            <ProductForm
                mode="create"
                pending={mutation.isPending}
                onCancel={() => navigate('/admin/products')}
                onSubmit={handleSubmit}
            />
        </div>
    );
}