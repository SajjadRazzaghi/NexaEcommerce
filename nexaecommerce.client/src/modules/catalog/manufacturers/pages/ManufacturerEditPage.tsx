// src/modules/catalog/manufacturers/pages/ManufacturerEditPage.tsx
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { ManufacturerForm } from '@/modules/catalog/manufacturers/components/ManufacturerForm';
import { useManufacturer, useUpdateManufacturer } from '@/modules/catalog/manufacturers/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function ManufacturerEditPage() {
    useDocumentTitle('ویرایش تولیدکننده');
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const query = useManufacturer(id);
    const mutation = useUpdateManufacturer();

    if (query.isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
    if (query.isError || !query.data) return <ErrorState error={query.error} onRetry={() => query.refetch()} message="بارگذاری تولیدکننده با خطا مواجه شد." />;

    const handleSubmit = async (body: any) => {
        await mutation.mutateAsync({ id: id!, body }); // ✅ تغییر از data به body
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title={`ویرایش ${query.data.name}`}
                description="به‌روزرسانی اطلاعات تولیدکننده، SEO و وضعیت انتشار."
                actions={
                    <Button variant="outline" onClick={() => navigate(`/admin/manufacturers/${id}`)}>
                        <ArrowLeft /> بازگشت
                    </Button>
                }
            />
            <ManufacturerForm
                mode="edit"
                manufacturer={query.data}
                pending={mutation.isPending}
                onCancel={() => navigate(`/admin/manufacturers/${id}`)}
                onSubmit={handleSubmit} // ✅ استفاده از handleSubmit
            />
        </div>
    );
}