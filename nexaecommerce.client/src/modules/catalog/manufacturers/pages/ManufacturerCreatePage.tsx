// src/modules/catalog/manufacturers/pages/ManufacturerCreatePage.tsx
import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

import { PageHeader } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { ManufacturerForm } from '@/modules/catalog/manufacturers/components/ManufacturerForm';
import { manufacturersApi } from '@/modules/catalog/api/manufacturers';
import { useCreateManufacturer } from '@/modules/catalog/manufacturers/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function ManufacturerCreatePage() {
    useDocumentTitle('ایجاد تولیدکننده');
    const navigate = useNavigate();
    const mutation = useCreateManufacturer();

    return (
        <div className="space-y-6">
            <PageHeader
                title="ایجاد تولیدکننده جدید"
                description="اطلاعات تولیدکننده جدید را وارد کنید."
                actions={
                    <Button variant="outline" onClick={() => navigate('/admin/manufacturers')}>
                        <ArrowLeft /> بازگشت به لیست
                    </Button>
                }
            />
            <ManufacturerForm
                mode="create"
                pending={mutation.isPending}
                onCancel={() => navigate('/admin/manufacturers')}
                onSubmit={async (body) => mutation.mutateAsync(body as Parameters<typeof manufacturersApi.create>[0])}
            />
        </div>
    );
}