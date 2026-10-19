
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import {
    PageHeader,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { CategoryForm } from '../components/CategoryForm';
import {
    useCategory,
    useUpdateCategory,
    useCategories,
} from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function CategoryEditPage() {
    const { t } = useTranslation();
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const query = useCategory(id);
    const mutation = useUpdateCategory();
    const { data: categories } = useCategories({ pageSize: 999 });

    // تمام Hookها باید قبل از returnهای شرطی اجرا شوند.
    useDocumentTitle(
        query.data?.name
            ? t('categoryAttributesPanel.editPageTitle', {
                name: query.data.name,
            })
            : t('catalogCategories.detailsTitle'),
    );

    if (query.isLoading) {
        return <LoadingSkeleton variant="cards" rows={4} />;
    }

    if (query.isError || !query.data) {
        return (
            <ErrorState
                error={query.error}
                onRetry={() => query.refetch()}
                message="بارگذاری دسته‌بندی با خطا مواجه شد."
            />
        );
    }

    const parentCategories = (categories || []).filter(
        category => category.id !== id,
    );

    return (
        <div className="space-y-6">
            <PageHeader
                title={t('categoryAttributesPanel.editPageTitle', {
                    name: query.data.name,
                })}
                description={t(
                    'categoryAttributesPanel.editPageDescription',
                )}
                actions={
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() => navigate(`/admin/categories/${id}`)}
                    >
                        <ArrowLeft />
                        {t('categoryAttributesPanel.backToDetails')}
                    </Button>
                }
            />

            <CategoryForm
                mode="edit"
                category={query.data}
                pending={mutation.isPending}
                parentCategories={parentCategories}
                onCancel={() => navigate(`/admin/categories/${id}`)}
                onSubmit={async body =>
                    mutation.mutateAsync({
                        id: id!,
                        data: body,
                    })
                }
            />
        </div>
    );
}
