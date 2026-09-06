import {
    useNavigate,
    useParams,
} from 'react-router-dom';

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

export default function ProductEditPage() {
    useDocumentTitle(
        'Edit Product',
    );

    const {
        id,
    } =
        useParams<{
            id: string;
        }>();

    const navigate =
        useNavigate();

    const query =
        useProduct(id);

    const mutation =
        useUpdateProduct();

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
                message="Unable to load product."
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
                title={`Edit ${query.data.name}`}
                description="Update product information, variants and catalog configuration."
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
                        Back
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