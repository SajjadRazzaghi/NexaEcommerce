import { useNavigate } from 'react-router';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { PageHeader } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { BrandForm } from '@/modules/catalog/brands/components/BrandForm';
import { brandsApi } from '@/modules/catalog/api/brands';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function NewBrandPage() {
    const { t } = useTranslation();
    const navigate = useNavigate();

    useDocumentTitle(
        t('catalogBrands.newTitle'),
    );

    return (
        <div className="space-y-6">
            <PageHeader
                title={t(
                    'catalogBrands.newTitle',
                )}
                description={t(
                    'catalogBrands.newDescription',
                )}
                actions={
                    <Button
                        variant="outline"
                        onClick={() =>
                            navigate(
                                '/admin/brands',
                            )
                        }
                    >
                        <ArrowLeft />
                        {t(
                            'catalogBrands.backToBrands',
                        )}
                    </Button>
                }
            />

            <BrandForm
                mode="create"
                onCancel={() =>
                    navigate(
                        '/admin/brands',
                    )
                }
                onSubmit={async body => {
                    const result =
                        await brandsApi.create(
                            body,
                        );

                    navigate(
                        `/admin/brands/${result.id}`,
                    );

                    return result;
                }}
            />
        </div>
    );
}