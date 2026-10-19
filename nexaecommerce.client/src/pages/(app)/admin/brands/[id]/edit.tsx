
import {
    useNavigate,
    useParams,
} from 'react-router';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import {
    PageHeader,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { BrandForm } from '@/modules/catalog/brands/components/BrandForm';
import {
    useBrand,
    useUpdateBrand,
} from '@/modules/catalog/brands/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function EditBrandPage() {
    const { t } = useTranslation();
    const navigate = useNavigate();

    const { id } = useParams<{
        id: string;
    }>();

    const query = useBrand(id);
    const mutation = useUpdateBrand();

    useDocumentTitle(
        query.data?.name
            ? `${t('catalogBrands.newTitle')} - ${query.data.name}`
            : t('catalogBrands.newTitle'),
    );

    if (query.isLoading) {
        return (
            <LoadingSkeleton
                variant="cards"
                rows={4}
            />
        );
    }

    if (query.isError || !query.data) {
        return (
            <ErrorState
                error={query.error}
                onRetry={() => query.refetch()}
                message={t('catalogBrands.loadError')}
            />
        );
    }

    const brand = query.data;

    return (
        <div className="space-y-6">
            <PageHeader
                title={`${t('catalogBrands.newTitle')} - ${brand.name}`}
                description={brand.slug}
                actions={
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() =>
                            navigate(`/admin/brands/${id}`)
                        }
                    >
                        <ArrowLeft />
                        {t('catalogBrands.backToBrands')}
                    </Button>
                }
            />

            <BrandForm
                mode="edit"
                brand={brand}
                pending={mutation.isPending}
                onCancel={() =>
                    navigate(`/admin/brands/${id}`)
                }
                onSubmit={async body => {
                    if (!id) {
                        throw new Error(
                            'Brand id is required.',
                        );
                    }

                    if (!('isActive' in body)) {
                        throw new Error(
                            'Invalid brand update payload.',
                        );
                    }

                    return mutation.mutateAsync({
                        id,
                        body,
                    });
                }}
            />
        </div>
    );
}
