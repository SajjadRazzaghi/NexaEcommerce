
import {
    useNavigate,
    useSearchParams,
} from 'react-router';

import {
    useTranslation,
} from 'react-i18next';

import {
    Alert,
    AlertDescription,
} from '@/components/ui/alert';

import {
    PageHeader,
    ErrorState,
    LoadingSkeleton,
} from '@/components/data-states';

import {
    ProductForm,
} from '@/modules/catalog/products/components/ProductForm';

import {
    useCreateProduct,
    useProduct,
} from '@/modules/catalog/products/hooks';

import type {
    CreateProductDto,
} from '@/modules/catalog/api/products';

export default function NewProductPage() {
    const navigate = useNavigate();

    const [searchParams] =
        useSearchParams();

    const { i18n } = useTranslation();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa') ?? false;

    // Copy parameters supplied by ProductListPage.
    const copyFromId =
        searchParams.get('copyFrom') || undefined;

    const initialSku =
        searchParams.get('sku')?.trim() ?? '';

    const isCopyMode =
        Boolean(copyFromId);

    // This hook is disabled automatically when copyFromId is absent.
    const sourceProductQuery =
        useProduct(copyFromId);

    const createProduct =
        useCreateProduct();

    const text = isFa
        ? {
            title: isCopyMode
                ? 'کپی محصول'
                : 'ایجاد محصول جدید',

            description: isCopyMode
                ? 'مشخصات محصول مبدأ را بررسی و ویرایش کنید و نسخه جدید را بسازید.'
                : 'اطلاعات محصول جدید را وارد کنید.',

            inventoryNote:
                'موجودی محصول جدید از محصول مبدأ کپی نمی‌شود. پس از ایجاد محصول، موجودی هر تنوع را از بخش مدیریت موجودی و در انبار و موقعیت مربوط وارد کنید.',

            missingSku:
                'SKU اصلی محصول جدید وارد نشده است. به فهرست محصولات برگردید و دوباره گزینه کپی محصول را انتخاب کنید.',

            cancel: 'انصراف',
        }
        : {
            title: isCopyMode
                ? 'Copy product'
                : 'Create new product',

            description: isCopyMode
                ? 'Review and edit the source product details, then create a new independent product.'
                : 'Enter the new product information.',

            inventoryNote:
                'Inventory is not copied from the source product. After creation, enter stock for each variant through Inventory Management.',

            missingSku:
                'The new product main SKU is missing. Return to the product list and start the copy operation again.',

            cancel: 'Cancel',
        };

    const handleSubmit = async (
        body: CreateProductDto,
    ) => {
        // Inventory belongs to the Inventory module.
        // Never copy or create an opening balance from the source.
        return await createProduct.mutateAsync({
            ...body,

            variants:
                body.variants.map(variant => ({
                    ...variant,
                    stockQuantity: 0,
                })),
        });
    };

    // The SKU must be supplied by the copy dialog.
    if (
        isCopyMode &&
        !initialSku
    ) {
        return (
            <div className="space-y-6">
                <PageHeader
                    title={text.title}
                    description={text.description}
                />

                <Alert variant="destructive">
                    <AlertDescription>
                        {text.missingSku}
                    </AlertDescription>
                </Alert>
            </div>
        );
    }

    // Load the source product before showing the form.
    if (
        isCopyMode &&
        sourceProductQuery.isLoading
    ) {
        return (
            <div className="space-y-6">
                <PageHeader
                    title={text.title}
                    description={text.description}
                />

                <LoadingSkeleton
                    variant="cards"
                    rows={4}
                />
            </div>
        );
    }

    if (
        isCopyMode &&
        (
            sourceProductQuery.isError ||
            !sourceProductQuery.data
        )
    ) {
        return (
            <ErrorState
                error={sourceProductQuery.error}
                onRetry={() =>
                    sourceProductQuery.refetch()
                }
                message={
                    isFa
                        ? 'دریافت محصول مبدأ برای کپی ناموفق بود.'
                        : 'Could not load the source product.'
                }
            />
        );
    }

    return (
        <div
            className="space-y-6"
            dir={isFa ? 'rtl' : 'ltr'}
        >
            <PageHeader
                title={text.title}
                description={text.description}
            />

            <Alert>
                <AlertDescription>
                    {text.inventoryNote}
                </AlertDescription>
            </Alert>

            <ProductForm
                mode="create"

                sourceProduct={
                    isCopyMode
                        ? sourceProductQuery.data
                        : undefined
                }

                initialSku={
                    isCopyMode
                        ? initialSku
                        : undefined
                }

                pending={
                    createProduct.isPending
                }

                onSubmit={handleSubmit}

                onSuccess={result => {
                    if (
                        typeof result === 'object' &&
                        result !== null &&
                        'id' in result &&
                        typeof result.id === 'string'
                    ) {
                        navigate(
                            `/admin/products/${result.id}`,
                        );
                    }
                }}

                onCancel={() =>
                    navigate('/admin/products')
                }
            />
        </div>
    );
}
