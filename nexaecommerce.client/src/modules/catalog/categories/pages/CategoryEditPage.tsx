// src/modules/catalog/categories/pages/CategoryEditPage.tsx
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { CategoryForm } from '../components/CategoryForm';
import { useCategory, useUpdateCategory, useCategories } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function CategoryEditPage() {
    useDocumentTitle('ویرایش دسته‌بندی');
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const query = useCategory(id);
    const mutation = useUpdateCategory();
    const { data: categories } = useCategories({ pageSize: 999 });

    if (query.isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
    if (query.isError || !query.data) return <ErrorState error={query.error} onRetry={() => query.refetch()} message="بارگذاری دسته‌بندی با خطا مواجه شد." />;

    // حذف خود دسته‌بندی از لیست والدها (برای جلوگیری از انتخاب خودش)
    const parentCategories = (categories || []).filter(c => c.id !== id);

    return (
        <div className="space-y-6">
            <PageHeader
                title={`ویرایش ${query.data.name}`}
                description="به‌روزرسانی اطلاعات دسته‌بندی."
                actions={
                    <Button variant="outline" onClick={() => navigate(`/admin/categories/${id}`)}>
                        <ArrowLeft /> بازگشت
                    </Button>
                }
            />
            <CategoryForm
                mode="edit"
                category={query.data}
                pending={mutation.isPending}
                parentCategories={parentCategories}
                onCancel={() => navigate(`/admin/categories/${id}`)}
                onSubmit={async (body) => mutation.mutateAsync({ id: id!, data: body })}
            />
        </div>
    );
}