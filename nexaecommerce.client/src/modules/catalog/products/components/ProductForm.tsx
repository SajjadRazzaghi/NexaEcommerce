import {
    useEffect,
    useMemo,
} from 'react';

import {
    zodResolver,
} from '@hookform/resolvers/zod';

import {
    useFieldArray,
    useForm,
} from 'react-hook-form';

import {
    Loader2,
    Plus,
    Save,
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

import {
    Input,
} from '@/components/ui/input';

import {
    Textarea,
} from '@/components/ui/textarea';

import {
    Switch,
} from '@/components/ui/switch';

import {
    FileUpload,
} from '@/components/ui/file-upload';

import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/components/ui/form';

import {
    FormGrid,
} from '@/components/forms/form-grid';

import {
    FormBanner,
} from '@/components/auth/form-banner';

import {
    useSubmitForm,
} from '@/components/forms/use-submit-form';

import {
    useBrands,
} from '@/modules/catalog/brands/hooks';

import {
    useCategories,
} from '@/modules/catalog/categories/hooks';

import {
    useCatalogAttributes,
} from '@/modules/catalog/catalogAttributes/hooks';

import type {
    CatalogAttribute,
} from '@/modules/catalog/api/catalogAttributes';

import type {
    Product,
    CreateProductDto,
    UpdateProductDto,
} from '../../api/products';


/* -------------------------------------------------------------------------- */
/*                                   Schemas                                  */
/* -------------------------------------------------------------------------- */

const variantSchema =
    z.object({
        id:
            z.string().optional(),

        sku:
            z
                .string()
                .trim()
                .min(
                    1,
                    'SKU is required.',
                ),

        color:
            z
                .string()
                .default(''),

        size:
            z
                .string()
                .default(''),

        priceOverride:
            z
                .number()
                .min(
                    0,
                    'Variant price cannot be negative.',
                )
                .optional(),

        stockQuantity:
            z
                .number()
                .int()
                .min(
                    0,
                    'Stock cannot be negative.',
                )
                .default(0),

        attributeValueIds:
            z
                .array(
                    z.string(),
                )
                .default([]),
    });


const imageSchema =
    z.object({
        imageUrl:
            z
                .string()
                .min(
                    1,
                    'Image URL is required.',
                ),

        altText:
            z
                .string()
                .default(''),

        isMain:
            z
                .boolean()
                .default(false),
    });


const schema =
    z.object({
        name:
            z
                .string()
                .trim()
                .min(
                    2,
                    'Product name must be at least 2 characters.',
                )
                .max(
                    200,
                    'Product name cannot exceed 200 characters.',
                ),

        slug:
            z
                .string()
                .trim()
                .max(
                    200,
                    'Slug cannot exceed 200 characters.',
                )
                .default(''),

        description:
            z
                .string()
                .max(
                    5000,
                    'Description cannot exceed 5000 characters.',
                )
                .default(''),

        shortDescription:
            z
                .string()
                .max(
                    500,
                    'Short description cannot exceed 500 characters.',
                )
                .default(''),

        price:
            z
                .number()
                .min(
                    0,
                    'Price cannot be negative.',
                ),

        currency:
            z
                .string()
                .trim()
                .default('IRR'),

        comparePrice:
            z
                .number()
                .min(
                    0,
                    'Compare price cannot be negative.',
                )
                .nullable()
                .default(null),

        sku:
            z
                .string()
                .max(
                    50,
                    'SKU cannot exceed 50 characters.',
                )
                .default(''),

        brandId:
            z
                .string()
                .nullable()
                .default(null),

        categoryIds:
            z
                .array(
                    z.string(),
                )
                .default([]),

        stockQuantity:
            z
                .number()
                .int()
                .min(
                    0,
                    'Stock cannot be negative.',
                )
                .default(0),

        isActive:
            z
                .boolean()
                .default(true),

        isFeatured:
            z
                .boolean()
                .default(false),

        isInStock:
            z
                .boolean()
                .default(true),

        discountPercentage:
            z
                .number()
                .min(
                    0,
                    'Discount cannot be negative.',
                )
                .max(
                    100,
                    'Discount cannot exceed 100%.',
                )
                .nullable()
                .default(null),

        variants:
            z
                .array(
                    variantSchema,
                )
                .default([]),

        images:
            z
                .array(
                    imageSchema,
                )
                .default([]),
    });


/* -------------------------------------------------------------------------- */
/*                                Form Types                                  */
/* -------------------------------------------------------------------------- */

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
        id?: string;

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


/* -------------------------------------------------------------------------- */
/*                         Catalog → Product Mapping                          */
/* -------------------------------------------------------------------------- */

/**
 * Converts ProductVariant attribute references into
 * CatalogAttributeValue IDs.
 *
 * ProductVariant.attributes may contain Product-level
 * attribute/value identifiers, while the update API expects
 * CatalogAttributeValue IDs.
 */
function mapProductVariantAttributeIds(
    variant:
        Product['variants'][number],

    catalogAttributes:
        | CatalogAttribute[]
        | undefined,
): string[] {
    if (
        !catalogAttributes ||
        catalogAttributes.length === 0
    ) {
        return [];
    }

    const result: string[] = [];

    for (
        const productAttribute
        of variant.attributes ?? []
    ) {
        const productCode =
            productAttribute.attributeCode
                ?.trim()
                .toLowerCase();

        const productValue =
            productAttribute.value
                ?.trim()
                .toLowerCase();

        if (
            !productCode ||
            !productValue
        ) {
            continue;
        }

        const catalogAttribute =
            catalogAttributes.find(
                (
                    attribute,
                ) =>
                    attribute.code
                        ?.trim()
                        .toLowerCase() ===
                    productCode,
            );

        if (!catalogAttribute) {
            continue;
        }

        const catalogValue =
            (
                catalogAttribute.values ??
                []
            ).find(
                (
                    value,
                ) => {
                    const canonicalValue =
                        value.value
                            ?.trim()
                            .toLowerCase();

                    const displayValue =
                        (
                            value.displayValue ??
                            ''
                        )
                            .trim()
                            .toLowerCase();

                    return (
                        canonicalValue ===
                            productValue ||
                        displayValue ===
                            productValue
                    );
                },
            );

        if (
            catalogValue
        ) {
            result.push(
                catalogValue.id,
            );
        }
    }

    return Array.from(
        new Set(
            result,
        ),
    );
}


/* -------------------------------------------------------------------------- */
/*                         Product → Form Mapping                             */
/* -------------------------------------------------------------------------- */

function toValues(
    product:
        | Product
        | undefined,

    catalogAttributes:
        | CatalogAttribute[]
        | undefined,
): FormValues {
    if (!product) {
        return {
            ...emptyValues,

            variants: [],

            images: [],

            categoryIds: [],
        };
    }

    return {
        name:
            product.name ?? '',

        slug:
            product.slug ?? '',

        description:
            product.description ?? '',

        shortDescription:
            product.shortDescription ?? '',

        price:
            product.price ?? 0,

        currency:
            product.currency || 'IRR',

        comparePrice:
            product.comparePrice ??
            null,

        sku:
            product.sku ?? '',

        brandId:
            product.brandId ??
            null,

        categoryIds:
            product.categoryIds ??
            [],

        stockQuantity:
            product.stockQuantity ??
            0,

        isActive:
            product.isActive,

        isFeatured:
            product.isFeatured,

        isInStock:
            product.isInStock,

        discountPercentage:
            product.discountPercentage ??
            null,

        variants:
            (
                product.variants ??
                []
            ).map(
                (
                    variant,
                ) => ({
                    id:
                        variant.id,

                    sku:
                        variant.sku ?? '',

                    color:
                        variant.color ??
                        '',

                    size:
                        variant.size ??
                        '',

                    priceOverride:
                        variant.priceOverride ??
                        undefined,

                    stockQuantity:
                        variant.stockQuantity ??
                        0,

                    attributeValueIds:
                        mapProductVariantAttributeIds(
                            variant,
                            catalogAttributes,
                        ),
                }),
            ),

        images:
            (
                product.images ??
                []
            ).map(
                (
                    image,
                ) => ({
                    imageUrl:
                        image.imageUrl,

                    altText:
                        image.altText ??
                        '',

                    isMain:
                        image.isMain,
                }),
            ),
    };
}


/* -------------------------------------------------------------------------- */
/*                                Props Types                                 */
/* -------------------------------------------------------------------------- */

type ProductFormCreateProps = {
    mode: 'create';

    product?: undefined;

    pending?: boolean;

    onSubmit: (
        body: CreateProductDto,
    ) => Promise<unknown>;

    onCancel: () => void;
};


type ProductFormEditProps = {
    mode: 'edit';

    product: Product;

    pending?: boolean;

    onSubmit: (
        body: UpdateProductDto,
    ) => Promise<unknown>;

    onCancel: () => void;
};


type ProductFormProps =
    | ProductFormCreateProps
    | ProductFormEditProps;


/* -------------------------------------------------------------------------- */
/*                              Main Component                                */
/* -------------------------------------------------------------------------- */

export function ProductForm(
    props: ProductFormProps,
) {
    const {
        product,
        mode,
        pending,
        onSubmit,
        onCancel,
    } = props;


    /* ------------------------------- Data -------------------------------- */

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


    /* ------------------------- Variant Attributes ------------------------ */

    const variantAttributes =
        useMemo(
            () =>
                (
                    attributes ??
                    []
                )
                    .filter(
                        (
                            attribute,
                        ) =>
                            attribute.isActive &&
                            attribute.isVariantAttribute,
                    )
                    .map(
                        (
                            attribute,
                        ) => ({
                            ...attribute,

                            values:
                                (
                                    attribute.values ??
                                    []
                                )
                                    .filter(
                                        (
                                            value,
                                        ) =>
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
                        (
                            a,
                            b,
                        ) =>
                            a.displayOrder -
                            b.displayOrder,
                    ),
            [attributes],
        );


    /* ------------------------------ Form -------------------------------- */

    const form =
        useForm<FormValues>({
            resolver:
                zodResolver(
                    schema,
                ) as never,

            defaultValues:
                toValues(
                    product,
                    attributes,
                ),

            mode:
                'onBlur',
        });


    /* ---------------------------- Arrays -------------------------------- */

    const {
        fields:
            variantFields,

        append:
            addVariant,

        remove:
            removeVariant,
    } =
        useFieldArray({
            control:
                form.control,

            name:
                'variants',
        });


    const {
        fields:
            imageFields,

        append:
            addImage,

        remove:
            removeImage,
    } =
        useFieldArray({
            control:
                form.control,

            name:
                'images',
        });


    /* ---------------------------- Reset --------------------------------- */

    useEffect(
        () => {
            form.reset(
                toValues(
                    product,
                    attributes,
                ),
            );
        },
        [
            product,
            attributes,
            form,
        ],
    );


    /* --------------------------- Submit --------------------------------- */

    const submitFlow =
        useSubmitForm<
            FormValues,
            CreateProductDto |
                UpdateProductDto,
            unknown
        >({
            form,

            mutationFn:
                onSubmit as (
                    body:
                        | CreateProductDto
                        | UpdateProductDto,
                ) =>
                    Promise<unknown>,

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
                (
                    values,
                ) => {
                    /* --------------------------- CREATE --------------------------- */

                    if (
                        mode ===
                        'create'
                    ) {
                        let variants =
                            values.variants.map(
                                (
                                    variant,
                                ) => ({
                                    sku:
                                        variant.sku
                                            .trim(),

                                    color:
                                        variant.color
                                            ?.trim() ||
                                        undefined,

                                    size:
                                        variant.size
                                            ?.trim() ||
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
                                                ).filter(
                                                    Boolean,
                                                ),
                                            ),
                                        ),
                                }),
                            );

                        /*
                         * Simple products without explicitly entered
                         * variants receive one default variant.
                         */
                        if (
                            variants.length ===
                            0
                        ) {
                            const baseSku =
                                values.sku
                                    ?.trim() ||
                                'PRODUCT';

                            variants = [
                                {
                                    sku:
                                        `${ baseSku } -DEFAULT`,

                                    color:
                                        '',

                                    size:
                                        '',

                                    stockQuantity:
                                        values.stockQuantity ??
                                        0,

                                    priceOverride:
                                        undefined,

                                    attributeValueIds:
                                        [],
                                },
                            ];
                        }

                        return {
                            name:
                                values.name
                                    .trim(),

                            price:
                                values.price,

                            currency:
                                values.currency
                                    .trim() ||
                                'IRR',

                            sku:
                                values.sku
                                    ?.trim() ||
                                undefined,

                            description:
                                values.description
                                    ?.trim() ||
                                undefined,

                            shortDescription:
                                values.shortDescription
                                    ?.trim() ||
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
                                            image.imageUrl
                                                .trim(),
                                    )
                                    .filter(
                                        Boolean,
                                    ),
                        } satisfies CreateProductDto;
                    }


                    /* ---------------------------- UPDATE ---------------------------- */

                    const variants =
                        values.variants.map(
                            (
                                variant,
                            ) => ({
                                /*
                                 * Existing variant:
                                 * keep its actual server-side ID.
                                 *
                                 * New variant:
                                 * omit the ID.
                                 */
                                id:
                                    variant.id ||
                                    undefined,

                                sku:
                                    variant.sku
                                        .trim(),

                                color:
                                    variant.color
                                        ?.trim() ||
                                    undefined,

                                size:
                                    variant.size
                                        ?.trim() ||
                                    undefined,

                                priceOverride:
                                    variant.priceOverride ??
                                    null,

                                /*
                                 * Existing stock is owned by Inventory.
                                 * Only a NEW variant may send initial stock.
                                 */
                                stockQuantity:
                                    variant.id
                                        ? undefined
                                        : variant.stockQuantity,

                                isActive:
                                    true,

                                /*
                                 * IMPORTANT:
                                 * These must be CatalogAttributeValue IDs.
                                 */
                                attributeValueIds:
                                    Array.from(
                                        new Set(
                                            (
                                                variant.attributeValueIds ??
                                                []
                                            ).filter(
                                                Boolean,
                                            ),
                                        ),
                                    ),
                            }),
                        );


                    return {
                        name:
                            values.name
                                .trim(),

                        price:
                            values.price,

                        currency:
                            values.currency
                                .trim() ||
                            'IRR',

                        description:
                            values.description
                                ?.trim() ||
                            undefined,

                        shortDescription:
                            values.shortDescription
                                ?.trim() ||
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

                        variants,
                    } satisfies UpdateProductDto;
                },
        });


    /* -------------------------- Variant Actions ------------------------- */

    const handleAddVariant =
        () => {
            addVariant({
                id:
                    undefined,

                sku:
                    '',

                color:
                    '',

                size:
                    '',

                priceOverride:
                    undefined,

                stockQuantity:
                    0,

                attributeValueIds:
                    [],
            });
        };


    /* --------------------------- Image Actions -------------------------- */

    const handleAddImage =
        (
            url: string,
        ) => {
            const cleanUrl =
                url.trim();

            if (
                !cleanUrl
            ) {
                return;
            }

            const isMain =
                imageFields.length ===
                0;

            addImage({
                imageUrl:
                    cleanUrl,

                altText:
                    '',

                isMain,
            });
        };


    /* ----------------------------- Data --------------------------------- */

    const brands =
        brandsData?.items ??
        [];


    const categories =
        Array.isArray(
            categoriesData,
        )
            ? categoriesData
            : (
                  categoriesData as
                      | {
                            items?: Array<{
                                id: string;
                                name: string;
                            }>;
                        }
                      | undefined
              )?.items ??
              [];


    /* ---------------------------------------------------------------------- */
    /*                                  UI                                    */
    /* ---------------------------------------------------------------------- */

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


                {/* ---------------------------------------------------------------- */}
                {/* Basic Information                                                 */}
                {/* ---------------------------------------------------------------- */}

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


                {/* ---------------------------------------------------------------- */}
                {/* Pricing                                                            */}
                {/* ---------------------------------------------------------------- */}

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
                                                type="number"
                                                min={0}
                                                step={1000}
                                                value={
                                                    field.value
                                                }
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
                                name="currency"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            Currency
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder="IRR"
                                                maxLength={
                                                    10
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
                                name="stockQuantity"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            Product Stock
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                type="number"
                                                min={0}
                                                value={
                                                    field.value
                                                }
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
                        </FormGrid>
                    </CardContent>
                </Card>


                {/* ---------------------------------------------------------------- */}
                {/* Brand & Categories                                                 */}
                {/* ---------------------------------------------------------------- */}

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
                                                field.value ??
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


                {/* ---------------------------------------------------------------- */}
                {/* Variants                                                           */}
                {/* ---------------------------------------------------------------- */}

                <Card>
                    <CardHeader>
                        <div className="flex items-center justify-between gap-4">
                            <div>
                                <CardTitle>
                                    Variants
                                </CardTitle>

                                <CardDescription>
                                    SKU, catalog attributes, variant pricing and availability
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
                                ) => {
                                    const currentVariant =
                                        form.watch(
                                            `variants.${index}`,
                                        );

                                    const isExistingVariant =
                                        Boolean(
                                            currentVariant?.id,
                                        );

                                    return (
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
                                                    control={
                                                        form.control
                                                    }
                                                    name={`variants.${ index }.sku`}
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
                                                                    placeholder="Variant SKU"
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
                                                    name={`variants.${ index }.stockQuantity`}
                                                    render={({
                                                        field,
                                                    }) => (
                                                        <FormItem>
                                                            <FormLabel>
                                                                Stock
                                                            </FormLabel>

                                                            <FormControl>
                                                                <Input
                                                                    type="number"
                                                                    min={0}
                                                                    value={
                                                                        field.value
                                                                    }
                                                                    disabled={
                                                                        mode ===
                                                                            'edit' &&
                                                                        isExistingVariant
                                                                    }
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

                                                            {mode ===
                                                                'edit' &&
                                                                isExistingVariant && (
                                                                    <p className="text-muted-foreground text-xs">
                                                                        Existing stock is managed by Inventory.
                                                                    </p>
                                                                )}

                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                                />

                                                <FormField
                                                    control={
                                                        form.control
                                                    }
                                                    name={`variants.${ index }.priceOverride`}
                                                    render={({
                                                        field,
                                                    }) => (
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
                                                                            event
                                                                                .target
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


                                            {variantAttributes.length >
                                                0 && (
                                                <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                                                    {variantAttributes.map(
                                                        (
                                                            attribute,
                                                        ) => {
                                                            const selectedIds =
                                                                form.watch(
                                                                    `variants.${ index }.attributeValueIds`,
                                                                ) ||
                                                                [];

                                                            const selectedValueId =
                                                                selectedIds.find(
                                                                    (
                                                                        valueId,
                                                                    ) =>
                                                                        attribute.values.some(
                                                                            (
                                                                                value,
                                                                            ) =>
                                                                                value.id ===
                                                                                valueId,
                                                                        ),
                                                                ) ||
                                                                '';

                                                            const selectedValue =
                                                                attribute.values.find(
                                                                    (
                                                                        value,
                                                                    ) =>
                                                                        value.id ===
                                                                        selectedValueId,
                                                                );

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
                                                                                    `variants.${ index }.attributeValueIds`,
                                                                                ) ||
                                                                                [];

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
                                                                                `variants.${ index }.attributeValueIds`,
                                                                                Array.from(
                                                                                    new Set(
                                                                                        nextIds,
                                                                                    ),
                                                                                ),
                                                                                {
                                                                                    shouldDirty:
                                                                                        true,
                                                                                    shouldTouch:
                                                                                        true,
                                                                                    shouldValidate:
                                                                                        true,
                                                                                },
                                                                            );

                                                                            /*
                                                                             * Keep legacy color/size convenience
                                                                             * fields synchronized with the generic
                                                                             * catalog attribute model.
                                                                             */
                                                                            const code =
                                                                                attribute.code
                                                                                    ?.trim()
                                                                                    .toLowerCase();

                                                                            const display =
                                                                                attribute.values.find(
                                                                                    (
                                                                                        value,
                                                                                    ) =>
                                                                                        value.id ===
                                                                                        nextValueId,
                                                                                );

                                                                            if (
                                                                                code ===
                                                                                'color'
                                                                            ) {
                                                                                form.setValue(
                                                                                    `variants.${ index }.color`,
                                                                                    display?.displayValue ||
                                                                                        display?.value ||
                                                                                        '',
                                                                                    {
                                                                                        shouldDirty:
                                                                                            true,
                                                                                    },
                                                                                );
                                                                            }

                                                                            if (
                                                                                code ===
                                                                                'size'
                                                                            ) {
                                                                                form.setValue(
                                                                                    `variants.${ index }.size`,
                                                                                    display?.displayValue ||
                                                                                        display?.value ||
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
                                                                            (
                                                                                value,
                                                                            ) => (
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

                                                                    {attribute.displayType?.toLowerCase() ===
                                                                        'color' &&
                                                                        selectedValueId && (
                                                                            <div className="text-muted-foreground flex items-center gap-2 text-xs">
                                                                                <span
                                                                                    className="size-4 rounded-full border"
                                                                                    style={{
                                                                                        backgroundColor:
                                                                                            selectedValue?.colorHex ||
                                                                                            'transparent',
                                                                                    }}
                                                                                />

                                                                                <span>
                                                                                    {selectedValue?.displayValue ||
                                                                                        selectedValue?.value ||
                                                                                        'Selected'}
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
                                    );
                                },
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


                {/* ---------------------------------------------------------------- */}
                {/* Images                                                             */}
                {/* ---------------------------------------------------------------- */}

                <Card>
                    <CardHeader>
                        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
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
                                                field:
                                                    imageField,
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


                {/* ---------------------------------------------------------------- */}
                {/* Publication Status                                                 */}
                {/* ---------------------------------------------------------------- */}

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
                                    key={
                                        name
                                    }
                                    control={
                                        form.control
                                    }
                                    name={
                                        name
                                    }
                                    render={({
                                        field,
                                    }) => (
                                        <FormItem className="flex items-center justify-between rounded-lg border p-4">
                                            <div className="space-y-1">
                                                <FormLabel>
                                                    {
                                                        label
                                                    }
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


                {/* ---------------------------------------------------------------- */}
                {/* Actions                                                            */}
                {/* ---------------------------------------------------------------- */}

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

                        {mode ===
                        'create'
                            ? 'Create Product'
                            : 'Save Changes'}
                    </Button>
                </div>
            </form>
        </Form>
    );
}

