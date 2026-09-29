import {
    useNavigate,
    useParams,
} from 'react-router-dom';

import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import {
    ArrowLeft,
} from 'lucide-react';

import {
    PageHeader,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';

import {
    Button,
} from '@/components/ui/button';

import {
    ProductForm,
} from '../components/ProductForm';

import {
    useProduct,
    useUpdateProduct,
} from '../hooks';

import type {
    UpdateProductDto,
} from '../../api/products';

import {
    useDocumentTitle,
} from '@/hooks/use-document-title';

import {
    appearanceApi,
} from '@/lib/api/appearance';

export default function ProductEditPage() {
    const { t } = useTranslation();

    const {
        id,
    } = useParams<{
        id: string;
    }>();

    const navigate = useNavigate();

    const query = useProduct(id);

    const mutation = useUpdateProduct();

    const appearanceQuery = useQuery({
        queryKey: ['appearance'],
        queryFn: appearanceApi.get,
        staleTime: 5 * 60_000,
        retry: 1,
    });

    const storeName =
        appearanceQuery.data?.storeName ||
        'NexaECommerce';

    useDocumentTitle(
        t('productEdit.pageTitle', {
            productName:
                query.data?.name ||
                t('productEdit.product'),
            storeName,
        }),
    );

    if (query.isLoading) {
        return (
            <LoadingSkeleton
                variant="cards"
                rows={4}
            />
        );
    }

    if (
        query.isError ||
        !query.data
    ) {
        return (
            <ErrorState
                error={query.error}
                onRetry={() =>
                    query.refetch()
                }
                message={t(
                    'productEdit.loadError',
                )}
            />
        );
    }

    const handleSubmit = async (
        body: UpdateProductDto,
    ) => {
        if (!id) {
            throw new Error(
                'Product id is required.',
            );
        }

        await mutation.mutateAsync({
            id,
            data: body,
        });

        navigate(
            '/admin/products',
        );
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title={t(
                    'productEdit.editTitle',
                    {
                        productName:
                            query.data.name,
                    },
                )}
                description={t(
                    'productEdit.description',
                )}
                actions={
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() =>
                            navigate(
                                `/admin/products/${id}`,
                            )
                        }
                    >
                        <ArrowLeft />
                        {t(
                            'productEdit.actions.back',
                        )}
                    </Button>
                }
            />

            <ProductForm
                mode="edit"
                product={query.data}
                pending={
                    mutation.isPending
                }
                onCancel={() =>
                    navigate(
                        `/admin/products/${id}`,
                    )
                }
                onSubmit={
                    handleSubmit
                }
            />
        </div>
    );
}