import {
    useTranslation,
} from 'react-i18next';

import {
    useNavigate,
} from 'react-router-dom';

import {
    Alert,
    AlertDescription,
} from '@/components/ui/alert';

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

    const {
        i18n,
    } = useTranslation();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa') ?? false;

    const text = isFa
        ? {
            title:
                'ایجاد محصول',

            description:
                'محصول و Variantهای آن را در کاتالوگ ایجاد کنید. موجودی انبار برای Variantهای ایجادشده به‌صورت جداگانه مدیریت می‌شود.',

            inventoryNote:
                'محصول فقط یک‌بار در کاتالوگ ایجاد می‌شود. پس از ایجاد محصول، موجودی هر Variant را از بخش مدیریت موجودی به انبار و موقعیت مربوط اختصاص دهید.',

            cancel:
                'انصراف',
        }
        : {
            title:
                'Create Product',

            description:
                'Create the product and its variants once in the catalog. Warehouse quantities are managed separately for the existing variants.',

            inventoryNote:
                'Create the product only once in the catalog. After creation, assign each variant to a warehouse and location from Inventory Management.',

            cancel:
                'Cancel',
        };

    const createProduct =
        useCreateProduct();

    const handleSubmit =
        async (
            body: CreateProductDto,
        ) => {
            const product =
                await createProduct.mutateAsync(
                    {
                        ...body,

                        variants:
                            body.variants.map(
                                variant => ({
                                    ...variant,
                                    stockQuantity:
                                        0,
                                }),
                            ),
                    },
                );

            navigate(
                `/admin/products/${product.id}`,
            );
        };

    return (
        <div
            className="space-y-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <PageHeader
                title={
                    text.title
                }
                description={
                    text.description
                }
            />

            <Alert>
                <AlertDescription>
                    {
                        text.inventoryNote
                    }
                </AlertDescription>
            </Alert>

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