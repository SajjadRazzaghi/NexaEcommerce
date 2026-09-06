import {
    useNavigate,
} from 'react-router-dom';

import {
    PageHeader,
} from '@/components/data-states';

import {
    ProductForm,
} from '@/modules/catalog/products/components/ProductForm';

import {
    useCreateProduct,
} from '@/modules/catalog/products/hooks';

import type {
    CreateProductDto,
} from '@/modules/catalog/api/products';

export default function NewProductPage() {
    const navigate =
        useNavigate();

    const createProduct =
        useCreateProduct();

    const handleSubmit = async (
        body: CreateProductDto,
    ) => {
        await createProduct.mutateAsync(
            body,
        );

        navigate(
            '/admin/products',
        );
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title="Create Product"
                description="Create a product with pricing, categories, variants and stock."
            />

            <ProductForm
                mode="create"
                pending={
                    createProduct.isPending
                }
                onSubmit={
                    handleSubmit
                }
                onCancel={() =>
                    navigate(
                        '/admin/products',
                    )
                }
            />
        </div>
    );
}