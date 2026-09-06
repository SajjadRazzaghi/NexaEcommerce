import { useEffect, useMemo } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import {
    useForm,
    useFieldArray,
} from 'react-hook-form';

import {
    Loader2,
    Save,
    Plus,
    X,
} from 'lucide-react';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { FileUpload } from '@/components/ui/file-upload';
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/components/ui/form';
import { FormGrid } from '@/components/forms/form-grid';
import { FormBanner } from '@/components/auth/form-banner';
import { useSubmitForm } from '@/components/forms/use-submit-form';

import { useBrands } from '@/modules/catalog/brands/hooks';
import { useCategories } from '@/modules/catalog/categories/hooks';
import {
    useCatalogAttributes,
} from '@/modules/catalog/catalogAttributes/hooks';

import type {
    Product,
    CreateProductDto,
    UpdateProductDto,
} from '../../api/products';

const variantSchema = z.object({
    sku: z
        .string()
        .trim()
        .min(1, 'SKU is required'),

    color: z
        .string()
        .default(''),

    size: z
        .string()
        .default(''),

    priceOverride: z
        .number()
        .min(0)
        .optional(),

    stockQuantity: z
        .number()
        .int()
        .min(0)
        .default(0),

    attributeValueIds: z
        .array(z.string())
        .default([]),
});
const imageSchema = z.object({
    imageUrl: z
        .string()
        .min(1, 'Image URL is required'),

    altText: z
        .string()
        .default(''),

    isMain: z
        .boolean()
        .default(false),
});

const schema = z.object({
    name: z
        .string()
        .trim()
        .min(
            2,
            'Product name must be at least 2 characters.',
        )
        .max(200),

    slug: z
        .string()
        .trim()
        .max(200)
        .default(''),

    description: z
        .string()
        .max(5000)
        .default(''),

    shortDescription: z
        .string()
        .max(500)
        .default(''),

    price: z
        .number()
        .min(
            0,
            'Price cannot be negative',
        ),

    currency: z
        .string()
        .default('IRR'),

    comparePrice: z
        .number()
        .min(0)
        .nullable()
        .default(null),

    sku: z
        .string()
        .max(50)
        .default(''),

    brandId: z
        .string()
        .nullable()
        .default(null),

    categoryIds: z
        .array(z.string())
        .default([]),

    stockQuantity: z
        .number()
        .int()
        .min(0)
        .default(0),

    isActive: z
        .boolean()
        .default(true),

    isFeatured: z
        .boolean()
        .default(false),

    isInStock: z
        .boolean()
        .default(true),

    discountPercentage: z
        .number()
        .min(0)
        .max(100)
        .nullable()
        .default(null),

    variants: z
        .array(variantSchema)
        .default([]),

    images: z
        .array(imageSchema)
        .default([]),
});

type FormValues = {
    name: string;
    slug: string;
    description: string;
    shortDescription: string;
    price: number;
    currency: string;
    comparePrice: number | null;
    sku: string;
    brandId: string | null;
    categoryIds: string[];
    stockQuantity: number;
    isActive: boolean;
    isFeatured: boolean;
    isInStock: boolean;
    discountPercentage: number | null;

    variants: {
        sku: string;
        color: string;
        size: string;
        priceOverride?: number;
        stockQuantity: number;
        attributeValueIds: string[];
    }[];

    images: {
        imageUrl: string;
        altText: string;
        isMain: boolean;
    }[];
};

const emptyValues: FormValues = {
    name: '',
    slug: '',
    description: '',
    shortDescription: '',
    price: 0,
    currency: 'IRR',
    comparePrice: null,
    sku: '',
    brandId: null,
    categoryIds: [],
    stockQuantity: 0,
    isActive: true,
    isFeatured: false,
    isInStock: true,
    discountPercentage: null,
    variants: [],
    images: [],
};

function toValues(
    product?: Product,
): FormValues {
    if (!product) {
        return emptyValues;
    }

    return {
        name: product.name,
        slug: product.slug || '',
        description: product.description || '',
        shortDescription:
            product.shortDescription || '',
        price: product.price,
        currency: product.currency || 'IRR',
        comparePrice:
            product.comparePrice ?? null,
        sku: product.sku || '',
        brandId: product.brandId || null,
        categoryIds:
            product.categoryIds || [],
        stockQuantity:
            product.stockQuantity || 0,
        isActive: product.isActive,
        isFeatured: product.isFeatured,
        isInStock: product.isInStock,
        discountPercentage:
            product.discountPercentage || null,

        variants: product.variants.map(
            (variant) => ({
                sku: variant.sku,

                color:
                    variant.color || '',

                size:
                    variant.size || '',

                priceOverride:
                    variant.priceOverride ??
                    undefined,

                stockQuantity:
                    variant.stockQuantity,

                attributeValueIds:
                    (
                        variant.attributes ??
                        []
                    )
                        .map(
                            (
                                attribute,
                            ) =>
                                attribute.attributeValueId,
                        )
                        .filter(Boolean),
            }),
        ),

        images: product.images.map(
            (image) => ({
                imageUrl:
                    image.imageUrl,

                altText:
                    image.altText || '',

                isMain:
                    image.isMain,
            }),
        ),
    };
}
export function ProductForm({
    product,
    mode,
    pending,
    onSubmit,
    onCancel,
}: {
    product?: Product;
    mode: 'create' | 'edit';
    pending?: boolean;
    onSubmit: (
        body:
            | CreateProductDto
            | UpdateProductDto,
    ) => Promise<unknown>;
    onCancel: () => void;
}) {
    const {
        data: brandsData,
    } = useBrands({
        pageSize: 999,
    });

    const {
        data: categoriesData,
    } = useCategories({
        pageSize: 999,
    });

    const {
        data: attributes,
    } = useCatalogAttributes();
    const variantAttributes =
        useMemo(
            () =>
                (attributes ?? [])
                    .filter(
                        (attribute) =>
                            attribute.isActive &&
                            attribute.isVariantAttribute,
                    )
                    .map(
                        (attribute) => ({
                            ...attribute,
                            values:
                                (
                                    attribute.values ??
                                    []
                                )
                                    .filter(
                                        (value) =>
                                            value.isActive,
                                    )
                                    .sort(
                                        (
                                            a,
                                            b,
                                        ) =>
                                            a.displayOrder -
                                            b.displayOrder,
                                    ),
                        }),
                    )
                    .sort(
                        (a, b) =>
                            a.displayOrder -
                            b.displayOrder,
                    ),
            [attributes],
        );

    const form =
        useForm<FormValues>({
            resolver:
                zodResolver(
                    schema,
                ) as any,

            defaultValues:
                toValues(product),

            mode: 'onBlur',
        });

    const {
        fields: variantFields,
        append: addVariant,
        remove: removeVariant,
    } = useFieldArray({
        control: form.control,
        name: 'variants',
    });

    const {
        fields: imageFields,
        append: addImage,
        remove: removeImage,
    } = useFieldArray({
        control: form.control,
        name: 'images',
    });

    useEffect(() => {
        form.reset(
            toValues(product),
        );
    }, [form, product]);

    const submitFlow =
        useSubmitForm<
            FormValues,
            CreateProductDto | UpdateProductDto,
            unknown
        >({
            form,
            mutationFn: onSubmit,

            fields:
                Object.keys(
                    emptyValues,
                ) as (
                    keyof FormValues
                )[],

            successMessage:
                mode === 'create'
                    ? 'Product created successfully.'
                    : 'Product updated successfully.',

            onSuccess:
                onCancel,

            transform:
                (values) => {
                    if (
                        mode ===
                        'create'
                    ) {
                        const variants =
                            values.variants.map(
                                (
                                    variant,
                                ) => ({
                                    sku:
                                        variant.sku.trim(),

                                    color:
                                        variant.color?.trim() ||
                                        undefined,

                                    size:
                                        variant.size?.trim() ||
                                        undefined,

                                    priceOverride:
                                        variant.priceOverride,

                                    stockQuantity:
                                        variant.stockQuantity,

                                    attributeValueIds:
                                        Array.from(
                                            new Set(
                                                (
                                                    variant.attributeValueIds ??
                                                    []
                                                ).filter(Boolean),
                                            ),
                                        ),
                                }),
                            );
                        if (
                            variants.length ===
                            0
                        ) {
                            variants.push({
                                sku: `${values.sku?.trim() ||
                                    'PRODUCT'
                                    }-DEFAULT`,

                                color: '',
                                size: '',

                                stockQuantity:
                                    values.stockQuantity ?? 0,

                                priceOverride:
                                    undefined,

                                attributeValueIds: [],
                            });
                        }

                        return {
                            name:
                                values.name.trim(),

                            price:
                                values.price,

                            currency:
                                values.currency.trim() ||
                                'IRR',

                            sku:
                                values.sku?.trim() ||
                                undefined,

                            description:
                                values.description?.trim() ||
                                undefined,

                            shortDescription:
                                values.shortDescription?.trim() ||
                                undefined,

                            brandId:
                                values.brandId ||
                                undefined,

                            categoryIds:
                                values.categoryIds,

                            variants,

                            images:
                                values.images
                                    .map(
                                        (
                                            image,
                                        ) =>
                                            image.imageUrl.trim(),
                                    )
                                    .filter(
                                        Boolean,
                                    ),
                        } satisfies CreateProductDto;
                    }

                    return {
                        name:
                            values.name.trim(),

                        price:
                            values.price,

                        currency:
                            values.currency.trim() ||
                            'IRR',

                        description:
                            values.description?.trim() ||
                            undefined,

                        shortDescription:
                            values.shortDescription?.trim() ||
                            undefined,

                        comparePrice:
                            values.comparePrice ??
                            null,

                        discountPercentage:
                            values.discountPercentage ??
                            null,

                        brandId:
                            values.brandId ||
                            null,

                        categoryIds:
                            values.categoryIds,

                        isActive:
                            values.isActive,

                        isFeatured:
                            values.isFeatured,

                        isPublished:
                            values.isActive,
                    } satisfies UpdateProductDto;
                },
        });

    const handleAddVariant =
        () => {
            addVariant({
                sku: '',
                color: '',
                size: '',
                priceOverride:
                    undefined,
                stockQuantity: 0,
                attributeValueIds: [],
            });
        };

    const handleAddImage =
        (url: string) => {
            const isMain =
                imageFields.length ===
                0;

            addImage({
                imageUrl: url,
                altText: '',
                isMain,
            });
        };

    const brands =
        brandsData?.items || [];

    const categories =
        Array.isArray(
            categoriesData,
        )
            ? categoriesData
            : (
                  categoriesData as any
              )?.items || [];

    

    return (
        <Form {...form}>
            <form
                onSubmit={
                    submitFlow.submit
                }
                className="space-y-6"
                noValidate
            >
                {submitFlow.banner && (
                    <FormBanner
                        state={
                            submitFlow.banner
                        }
                    />
                )}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            Basic Information
                        </CardTitle>

                        <CardDescription>
                            Basic product information
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        <FormField
                            control={
                                form.control
                            }
                            name="name"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        Product Name *
                                    </FormLabel>

                                    <FormControl>
                                        <Input
                                            {...field}
                                            autoFocus
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormGrid columns={2}>
                            <FormField
                                control={
                                    form.control
                                }
                                name="slug"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            Slug
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder={
                                                    mode ===
                                                    'create'
                                                        ? 'Generated automatically'
                                                        : undefined
                                                }
                                                disabled={
                                                    mode ===
                                                    'create'
                                                }
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            <FormField
                                control={
                                    form.control
                                }
                                name="sku"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            SKU
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder="Product SKU"
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>

                        <FormField
                            control={
                                form.control
                            }
                            name="description"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        Description
                                    </FormLabel>

                                    <FormControl>
                                        <Textarea
                                            {...field}
                                            rows={6}
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={
                                form.control
                            }
                            name="shortDescription"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        Short Description
                                    </FormLabel>

                                    <FormControl>
                                        <Textarea
                                            {...field}
                                            rows={2}
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>
                            Pricing & Discount
                        </CardTitle>

                        <CardDescription>
                            Product pricing and discount settings
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        <FormGrid columns={2}>
                            <FormField
                                control={
                                    form.control
                                }
                                name="price"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            Price *
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                type="number"
                                                min={0}
                                                step={1000}
                                                onChange={(
                                                    event,
                                                ) =>
                                                    field.onChange(
                                                        Number(
                                                            event.target
                                                                .value,
                                                        ),
                                                    )
                                                }
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            <FormField
                                control={
                                    form.control
                                }
                                name="comparePrice"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            Compare Price
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                type="number"
                                                min={0}
                                                step={1000}
                                                value={
                                                    field.value ??
                                                    ''
                                                }
                                                onChange={(
                                                    event,
                                                ) =>
                                                    field.onChange(
                                                        event
                                                            .target
                                                            .value
                                                            ? Number(
                                                                  event
                                                                      .target
                                                                      .value,
                                                              )
                                                            : null,
                                                    )
                                                }
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>

                        <FormGrid columns={3}>
                            <FormField
                                control={
                                    form.control
                                }
                                name="discountPercentage"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            Discount Percentage
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                type="number"
                                                min={0}
                                                max={100}
                                                value={
                                                    field.value ??
                                                    ''
                                                }
                                                onChange={(
                                                    event,
                                                ) =>
                                                    field.onChange(
                                                        event
                                                            .target
                                                            .value
                                                            ? Number(
                                                                  event
                                                                      .target
                                                                      .value,
                                                              )
                                                            : null,
                                                    )
                                                }
                                                placeholder="e.g. 15"
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>
                            Brand & Categories
                        </CardTitle>

                        <CardDescription>
                            Select the product brand and categories
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        <FormField
                            control={
                                form.control
                            }
                            name="brandId"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        Brand
                                    </FormLabel>

                                    <FormControl>
                                        <select
                                            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                                            value={
                                                field.value ||
                                                ''
                                            }
                                            onChange={(
                                                event,
                                            ) =>
                                                field.onChange(
                                                    event
                                                        .target
                                                        .value ||
                                                        null,
                                                )
                                            }
                                        >
                                            <option value="">
                                                Select Brand
                                            </option>

                                            {brands.map(
                                                (
                                                    brand,
                                                ) => (
                                                    <option
                                                        key={
                                                            brand.id
                                                        }
                                                        value={
                                                            brand.id
                                                        }
                                                    >
                                                        {
                                                            brand.name
                                                        }
                                                    </option>
                                                ),
                                            )}
                                        </select>
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={
                                form.control
                            }
                            name="categoryIds"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        Categories
                                    </FormLabel>

                                    <FormControl>
                                        <select
                                            multiple
                                            className="flex h-32 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                                            value={
                                                field.value
                                            }
                                            onChange={(
                                                event,
                                            ) =>
                                                field.onChange(
                                                    Array.from(
                                                        event
                                                            .target
                                                            .selectedOptions,
                                                        (
                                                            option,
                                                        ) =>
                                                            option.value,
                                                    ),
                                                )
                                            }
                                        >
                                            {categories.map(
                                                (
                                                    category,
                                                ) => (
                                                    <option
                                                        key={
                                                            category.id
                                                        }
                                                        value={
                                                            category.id
                                                        }
                                                    >
                                                        {
                                                            category.name
                                                        }
                                                    </option>
                                                ),
                                            )}
                                        </select>
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <div className="flex items-center justify-between gap-4">
                            <div>
                                <CardTitle>
                                    Variants
                                </CardTitle>

                                <CardDescription>
                                    SKU, color, size and stock
                                </CardDescription>
                            </div>

                            <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                onClick={
                                    handleAddVariant
                                }
                            >
                                <Plus className="me-1 size-4" />
                                Add Variant
                            </Button>
                        </div>
                    </CardHeader>

                    <CardContent>
                        <div className="space-y-4">
                            {variantFields.map(
                                (
                                    variantField,
                                    index,
                                ) => (
                                    <div
                                        key={
                                            variantField.id
                                        }
                                        className="relative rounded-lg border p-4"
                                    >
                                        <Button
                                            type="button"
                                            variant="ghost"
                                            size="sm"
                                            className="absolute end-2 top-2"
                                            onClick={() =>
                                                removeVariant(
                                                    index,
                                                )
                                            }
                                        >
                                            <X className="size-4" />
                                        </Button>

                                        <FormGrid columns={4}>
                                            <FormField
                                                control={form.control}
                                                name={`variants.${index}.sku`}
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>
                                                            SKU
                                                        </FormLabel>

                                                        <FormControl>
                                                            <Input
                                                                {...field}
                                                                placeholder="SKU"
                                                            />
                                                        </FormControl>

                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />

                                            <FormField
                                                control={form.control}
                                                name={`variants.${index}.stockQuantity`}
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>
                                                            Stock
                                                        </FormLabel>

                                                        <FormControl>
                                                            <Input
                                                                {...field}
                                                                type="number"
                                                                min={0}
                                                                onChange={(
                                                                    event,
                                                                ) =>
                                                                    field.onChange(
                                                                        Number(
                                                                            event
                                                                                .target
                                                                                .value,
                                                                        ),
                                                                    )
                                                                }
                                                            />
                                                        </FormControl>

                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />

                                            <FormField
                                                control={form.control}
                                                name={`variants.${index}.priceOverride`}
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>
                                                            Variant Price
                                                        </FormLabel>

                                                        <FormControl>
                                                            <Input
                                                                type="number"
                                                                min={0}
                                                                step={1000}
                                                                value={
                                                                    field.value ??
                                                                    ''
                                                                }
                                                                onChange={(
                                                                    event,
                                                                ) =>
                                                                    field.onChange(
                                                                        event.target
                                                                            .value
                                                                            ? Number(
                                                                                event
                                                                                    .target
                                                                                    .value,
                                                                            )
                                                                            : undefined,
                                                                    )
                                                                }
                                                                placeholder="Optional"
                                                            />
                                                        </FormControl>

                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />

                                            <div className="text-muted-foreground flex items-end pb-2 text-xs">
                                                {
                                                    variantAttributes.length
                                                }{' '}
                                                variant attributes
                                            </div>
                                        </FormGrid>

                                        {variantAttributes.length > 0 && (
                                            <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                                                {variantAttributes.map(
                                                    (attribute) => {
                                                        const selectedIds =
                                                            form.watch(
                                                                `variants.${index}.attributeValueIds`,
                                                            ) || [];

                                                        const selectedValueId =
                                                            selectedIds.find(
                                                                (valueId) =>
                                                                    attribute.values.some(
                                                                        (value) =>
                                                                            value.id ===
                                                                            valueId,
                                                                    ),
                                                            ) || '';

                                                        return (
                                                            <div
                                                                key={
                                                                    attribute.id
                                                                }
                                                                className="space-y-2"
                                                            >
                                                                <FormLabel>
                                                                    {
                                                                        attribute.name
                                                                    }

                                                                    {attribute.isRequired && (
                                                                        <span className="ms-1 text-destructive">
                                                                            *
                                                                        </span>
                                                                    )}
                                                                </FormLabel>

                                                                <select
                                                                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                                                                    value={
                                                                        selectedValueId
                                                                    }
                                                                    onChange={(
                                                                        event,
                                                                    ) => {
                                                                        const nextValueId =
                                                                            event
                                                                                .target
                                                                                .value;

                                                                        const currentIds =
                                                                            form.getValues(
                                                                                `variants.${index}.attributeValueIds`,
                                                                            ) || [];

                                                                        const idsWithoutCurrentAttribute =
                                                                            currentIds.filter(
                                                                                (
                                                                                    valueId,
                                                                                ) =>
                                                                                    !attribute.values.some(
                                                                                        (
                                                                                            value,
                                                                                        ) =>
                                                                                            value.id ===
                                                                                            valueId,
                                                                                    ),
                                                                            );

                                                                        const nextIds =
                                                                            nextValueId
                                                                                ? [
                                                                                    ...idsWithoutCurrentAttribute,
                                                                                    nextValueId,
                                                                                ]
                                                                                : idsWithoutCurrentAttribute;

                                                                        form.setValue(
                                                                            `variants.${index}.attributeValueIds`,
                                                                            nextIds,
                                                                            {
                                                                                shouldDirty:
                                                                                    true,
                                                                                shouldTouch:
                                                                                    true,
                                                                                shouldValidate:
                                                                                    true,
                                                                            },
                                                                        );

                                                                        const selectedValue =
                                                                            attribute.values.find(
                                                                                (
                                                                                    value,
                                                                                ) =>
                                                                                    value.id ===
                                                                                    nextValueId,
                                                                            );

                                                                        if (
                                                                            attribute.code.toLowerCase() ===
                                                                            'color'
                                                                        ) {
                                                                            form.setValue(
                                                                                `variants.${index}.color`,
                                                                                selectedValue?.displayValue ||
                                                                                selectedValue?.value ||
                                                                                '',
                                                                                {
                                                                                    shouldDirty:
                                                                                        true,
                                                                                },
                                                                            );
                                                                        }

                                                                        if (
                                                                            attribute.code.toLowerCase() ===
                                                                            'size'
                                                                        ) {
                                                                            form.setValue(
                                                                                `variants.${index}.size`,
                                                                                selectedValue?.displayValue ||
                                                                                selectedValue?.value ||
                                                                                '',
                                                                                {
                                                                                    shouldDirty:
                                                                                        true,
                                                                                },
                                                                            );
                                                                        }
                                                                    }}
                                                                >
                                                                    <option value="">
                                                                        Select{' '}
                                                                        {
                                                                            attribute.name
                                                                        }
                                                                    </option>

                                                                    {attribute.values.map(
                                                                        (value) => (
                                                                            <option
                                                                                key={
                                                                                    value.id
                                                                                }
                                                                                value={
                                                                                    value.id
                                                                                }
                                                                            >
                                                                                {value.displayValue ||
                                                                                    value.value}
                                                                            </option>
                                                                        ),
                                                                    )}
                                                                </select>

                                                                {attribute.displayType
                                                                    ?.toLowerCase() ===
                                                                    'color' &&
                                                                    selectedValueId && (
                                                                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                                                                            <span
                                                                                className="size-4 rounded-full border"
                                                                                style={{
                                                                                    backgroundColor:
                                                                                        attribute.values.find(
                                                                                            (
                                                                                                value,
                                                                                            ) =>
                                                                                                value.id ===
                                                                                                selectedValueId,
                                                                                        )
                                                                                            ?.colorHex ||
                                                                                        'transparent',
                                                                                }}
                                                                            />

                                                                            <span>
                                                                                Selected
                                                                            </span>
                                                                        </div>
                                                                    )}
                                                            </div>
                                                        );
                                                    },
                                                )}
                                            </div>
                                        )}
                                    </div>
                                ),
                            )}

                            {variantFields.length ===
                                0 && (
                                <p className="text-muted-foreground py-4 text-center text-sm">
                                    No variants added yet.
                                    Click "Add Variant" to add one.
                                </p>
                            )}
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <div className="flex items-center justify-between gap-4">
                            <div>
                                <CardTitle>
                                    Images
                                </CardTitle>

                                <CardDescription>
                                    Product images
                                </CardDescription>
                            </div>

                            <FileUpload
                                onChange={
                                    handleAddImage
                                }
                                accept="image/*"
                                maxSize={5}
                                placeholder="Drop an image here or click to upload"
                            />
                        </div>
                    </CardHeader>

                    <CardContent>
                        <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                            {imageFields.map(
                                (
                                    field,
                                    index,
                                ) => (
                                    <div
                                        key={
                                            field.id
                                        }
                                        className="relative rounded-lg border p-2"
                                    >
                                        <img
                                            src={
                                                field.imageUrl
                                            }
                                            alt={
                                                field.altText ||
                                                'Product image'
                                            }
                                            className="h-24 w-full rounded object-cover"
                                        />

                                        <Button
                                            type="button"
                                            variant="ghost"
                                            size="sm"
                                            className="absolute end-1 top-1 size-6 p-0"
                                            onClick={() =>
                                                removeImage(
                                                    index,
                                                )
                                            }
                                        >
                                            <X className="size-4" />
                                        </Button>

                                        <FormField
                                            control={
                                                form.control
                                            }
                                            name={`images.${ index }.isMain`}
                                            render={({
                                                field: imageField,
                                            }) => (
                                                <FormItem className="mt-2 flex items-center gap-2">
                                                    <FormControl>
                                                        <input
                                                            type="checkbox"
                                                            checked={
                                                                imageField.value
                                                            }
                                                            onChange={(
                                                                event,
                                                            ) =>
                                                                imageField.onChange(
                                                                    event
                                                                        .target
                                                                        .checked,
                                                                )
                                                            }
                                                            className="size-3"
                                                        />
                                                    </FormControl>

                                                    <FormLabel className="text-xs">
                                                        Main image
                                                    </FormLabel>
                                                </FormItem>
                                            )}
                                        />
                                    </div>
                                ),
                            )}

                            {imageFields.length ===
                                0 && (
                                <p className="text-muted-foreground col-span-full py-8 text-center text-sm">
                                    No images added yet.
                                    Upload an image above.
                                </p>
                            )}
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>
                            Publication Status
                        </CardTitle>

                        <CardDescription>
                            Control product visibility status
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="grid gap-4 sm:grid-cols-3">
                        {(
                            [
                                [
                                    'isActive',
                                    'Active',
                                    'Inactive products are not shown in the storefront.',
                                ],
                                [
                                    'isFeatured',
                                    'Featured',
                                    'Featured products are highlighted in featured sections.',
                                ],
                                [
                                    'isInStock',
                                    'In Stock',
                                    'Product stock status in the storefront.',
                                ],
                            ] as const
                        ).map(
                            ([
                                name,
                                label,
                                description,
                            ]) => (
                                <FormField
                                    key={name}
                                    control={
                                        form.control
                                    }
                                    name={name}
                                    render={({
                                        field,
                                    }) => (
                                        <FormItem className="flex items-center justify-between rounded-lg border p-4">
                                            <div className="space-y-1">
                                                <FormLabel>
                                                    {label}
                                                </FormLabel>

                                                <p className="text-muted-foreground text-xs">
                                                    {
                                                        description
                                                    }
                                                </p>
                                            </div>

                                            <FormControl>
                                                <Switch
                                                    checked={
                                                        field.value
                                                    }
                                                    onCheckedChange={
                                                        field.onChange
                                                    }
                                                />
                                            </FormControl>
                                        </FormItem>
                                    )}
                                />
                            ),
                        )}
                    </CardContent>
                </Card>

                <div className="flex flex-wrap justify-end gap-2">
                    <Button
                        type="button"
                        variant="outline"
                        onClick={
                            onCancel
                        }
                        disabled={
                            pending ||
                            submitFlow.isPending
                        }
                    >
                        Cancel
                    </Button>

                    <Button
                        type="submit"
                        disabled={
                            pending ||
                            submitFlow.isPending
                        }
                    >
                        {submitFlow.isPending ? (
                            <Loader2 className="animate-spin" />
                        ) : (
                            <Save />
                        )}

                        {mode === 'create'
                            ? 'Create Product'
                            : 'Save Changes'}
                    </Button>
                </div>
            </form>
        </Form>
    );
}

