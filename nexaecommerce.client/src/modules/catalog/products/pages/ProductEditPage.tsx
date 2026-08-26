// src/modules/catalog/products/pages/ProductEditPage.tsx
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { ProductForm } from '../components/ProductForm';
import { useProduct, useUpdateProduct } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function ProductEditPage() {
    useDocumentTitle('ویرایش محصول');
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const query = useProduct(id);
    const mutation = useUpdateProduct();

    if (query.isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
    if (query.isError || !query.data) {
        return (
            <ErrorState
                error={query.error}
                onRetry={() => query.refetch()}
                message="بارگذاری محصول با خطا مواجه شد."
            />
        );
    }

    const handleSubmit = async (body: any) => {
        await mutation.mutateAsync({ id: id!, data: body });
        navigate('/admin/products');
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title={`ویرایش ${query.data.name}`}
                description="به‌روزرسانی اطلاعات محصول."
                actions={
                    <Button variant="outline" onClick={() => navigate(`/admin/products/${id}`)}>
                        <ArrowLeft /> بازگشت
                    </Button>
                }
            />
            <ProductForm
                mode="edit"
                product={query.data}
                pending={mutation.isPending}
                onCancel={() => navigate(`/admin/products/${id}`)}
                onSubmit={handleSubmit}
            />
        </div>
    );
}