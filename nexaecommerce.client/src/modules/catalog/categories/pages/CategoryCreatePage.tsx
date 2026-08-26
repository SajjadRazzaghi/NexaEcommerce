// src/modules/catalog/categories/pages/CategoryCreatePage.tsx
import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

import { PageHeader } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { CategoryForm } from '../components/CategoryForm';
import { useCreateCategory, useCategories } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function CategoryCreatePage() {
    useDocumentTitle('ایجاد دسته‌بندی');
    const navigate = useNavigate();
    const mutation = useCreateCategory();
    const { data: categories } = useCategories({ pageSize: 999 });

    return (
        <div className="space-y-6">
            <PageHeader
                title="ایجاد دسته‌بندی جدید"
                description="اطلاعات دسته‌بندی جدید را وارد کنید."
                actions={
                    <Button variant="outline" onClick={() => navigate('/admin/categories')}>
                        <ArrowLeft /> بازگشت به لیست
                    </Button>
                }
            />
            <CategoryForm
                mode="create"
                pending={mutation.isPending}
                parentCategories={categories || []}
                onCancel={() => navigate('/admin/categories')}
                onSubmit={async (body) => mutation.mutateAsync(body)}
            />
        </div>
    );
}