import {
    useEffect,
    useMemo,
} from 'react';

import {
    resolveMediaUrl,
} from '@/lib/media-url';

import {
    useTranslation,
} from 'react-i18next';

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

import {
    z,
} from 'zod';

import {
    Button,
} from '@/components/ui/button';

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
            z
                .string()
                .optional(),

        sku:
            z
                .string()
                .trim()
                .min(
                    1,
                    'SKU is required.',
                ),

        priceOverride:
            z
                .number()
                .min(
                    0,
                    'Variant price cannot be negative.',
                )
                .optional(),

        /*
         * Existing inventory is read-only in this form.
         *
         * New variants are always created with stock = 0.
         */
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

        /*
         * Generic Catalog Attribute Value IDs
         * selected for this Variant.
         */
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

        isActive:
            z
                .boolean()
                .default(true),

        isPublished:
            z
                .boolean()
                .default(true),

        isFeatured:
            z
                .boolean()
                .default(false),

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

        /*
         * Form-only.
         *
         * Determines which Catalog Attributes are Variant
         * dimensions for this specific product.
         */
        variantAttributeIds:
            z
                .array(
                    z.string(),
                )
                .default([]),

        /*
         * Product-level specifications.
         *
         * These are NOT Variant dimensions.
         */

        specifications:
            z
                .array(
                    z.object({
                        catalogAttributeId:
                            z.string(),

                        catalogAttributeValueId:
                            z.string(),

                        value:
                            z.string().default(''),

                        displayValue:
                            z.string().default(''),

                        colorHex:
                            z.string().default(''),

                        displayOrder:
                            z.number().int().min(0),
                    }),
                )
                .default([]),
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

    isActive: boolean;
    isPublished: boolean;
    isFeatured: boolean;

    discountPercentage: number | null;

    variantAttributeIds: string[];

    specifications: {
        catalogAttributeId: string;
        catalogAttributeValueId: string;
        value: string;
        displayValue: string;
        colorHex: string;
        displayOrder: number;
    }[];

    variants: {
        id?: string;
        sku: string;
        priceOverride?: number;
        stockQuantity: number;
        isActive: boolean;
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

    isActive: true,
    isPublished: true,
    isFeatured: false,

    discountPercentage: null,

    variantAttributeIds: [],

    specifications: [],

    variants: [],

    images: [],
};


function toValues(
    product: Product | undefined,
    catalogAttributes: CatalogAttribute[] | undefined,
): FormValues {
    if (!product) {
        return {
            ...emptyValues,
            variants: [],
            images: [],
            categoryIds: [],
            variantAttributeIds: [],
            specifications: [],
        };
    }

    const source =
        product as Product & {
            attributes?: ProductAttributeRecord[];
        };

    const productAttributes =
        source.attributes ?? [];

    const availableCatalogAttributes =
        catalogAttributes ?? [];

    /* ---------------------------------------------------------------------- */
    /* Helpers                                                                */
    /* ---------------------------------------------------------------------- */

    const normalize =
        (value?: string | null): string =>
            value
                ?.trim()
                .toLowerCase() ?? '';

    const findCatalogAttributeByCode =
        (
            code?: string | null,
        ): CatalogAttribute | undefined => {
            const normalizedCode =
                normalize(code);

            if (!normalizedCode) {
                return undefined;
            }

            return availableCatalogAttributes.find(
                attribute =>
                    normalize(attribute.code) ===
                    normalizedCode,
            );
        };

    /*
     * Product-side AttributeValue IDs and Catalog-side
     * CatalogAttributeValue IDs are different IDs.
     *
     * Therefore we only trust the ID when it really belongs
     * to the current CatalogAttribute. Otherwise we resolve
     * the value by value/displayValue.
     */
    const findCatalogValue =
        (
            catalogAttribute: CatalogAttribute,
            productValue?: {
                id?: string;
                value?: string | null;
                displayValue?: string | null;
            },
        ) => {
            if (!productValue) {
                return undefined;
            }

            const catalogValues =
                catalogAttribute.values ?? [];

            const productValueId =
                productValue.id;

            /* -------------------------------------------------------------- */
            /* 1. Direct ID match                                            */
            /* -------------------------------------------------------------- */

            if (productValueId) {
                const byId =
                    catalogValues.find(
                        value =>
                            value.id ===
                            productValueId,
                    );

                if (byId) {
                    return byId;
                }
            }

            /* -------------------------------------------------------------- */
            /* 2. Text fallback                                               */
            /* -------------------------------------------------------------- */

            const productValueText =
                normalize(
                    productValue.value,
                );

            const productDisplayText =
                normalize(
                    productValue.displayValue,
                );

            if (
                !productValueText &&
                !productDisplayText
            ) {
                return undefined;
            }

            return catalogValues.find(
                value => {
                    const catalogValueText =
                        normalize(
                            value.value,
                        );

                    const catalogDisplayText =
                        normalize(
                            value.displayValue,
                        );

                    return (
                        (
                            Boolean(
                                productValueText,
                            ) &&
                            (
                                catalogValueText ===
                                    productValueText ||
                                catalogDisplayText ===
                                    productValueText
                            )
                        ) ||
                        (
                            Boolean(
                                productDisplayText,
                            ) &&
                            (
                                catalogValueText ===
                                    productDisplayText ||
                                catalogDisplayText ===
                                    productDisplayText
                            )
                        )
                    );
                },
            );
        };

    /* ---------------------------------------------------------------------- */
    /* Detect Variant Dimensions                                             */
    /* ---------------------------------------------------------------------- */

    const variantCodes =
        new Set(
            (
                product.variants ??
                []
            ).flatMap(
                variant =>
                    (
                        variant.attributes ??
                        []
                    )
                        .map(
                            attribute =>
                                normalize(
                                    attribute.attributeCode,
                                ),
                        )
                        .filter(Boolean),
            ),
        );

    /* ---------------------------------------------------------------------- */
    /* Product Specifications                                                */
    /* ---------------------------------------------------------------------- */

    /*
     * Product-level specifications must not contain
     * attributes that are being used as Variant dimensions.
     */
    const specifications =
        productAttributes.flatMap(
            attribute => {
                const productCode =
                    normalize(
                        attribute.code,
                    );

                if (!productCode) {
                    return [];
                }

                const isVariantAttribute =
                    variantCodes.has(
                        productCode,
                    );

                if (isVariantAttribute) {
                    return [];
                }

                const catalogAttribute =
                    findCatalogAttributeByCode(
                        productCode,
                    );

                if (!catalogAttribute) {
                    return [];
                }

                return (
                    attribute.values ??
                    []
                )
                    .map(
                        (
                            productValue,
                            valueIndex,
                        ) => {
                            const catalogValue =
                                findCatalogValue(
                                    catalogAttribute,
                                    {
                                        id:
                                            productValue.id,
                                        value:
                                            productValue.value,
                                        displayValue:
                                            productValue.displayValue,
                                    },
                                );

                            const rawValue =
                                (
                                    catalogValue?.value ??
                                    productValue.value ??
                                    ''
                                ).trim();

                            if (!rawValue) {
                                return null;
                            }

                            const displayValue =
                                (
                                    catalogValue?.displayValue ??
                                    productValue.displayValue ??
                                    rawValue
                                ).trim();

                            const colorHex =
                                (
                                    catalogValue?.colorHex ??
                                    productValue.colorHex ??
                                    ''
                                ).trim();

                            return {
                                catalogAttributeId:
                                    catalogAttribute.id,

                                catalogAttributeValueId:
                                    catalogValue?.id ??
                                    '',

                                value:
                                    rawValue,

                                displayValue:
                                    displayValue,

                                colorHex:
                                    colorHex,

                                displayOrder:
                                    valueIndex,
                            };
                        },
                    )
                    .filter(
                        (
                            item,
                        ): item is FormValues['specifications'][number] =>
                            item !== null,
                    );
            },
        );
/* ---------------------------------------------------------------------- */
/* Selected Variant Dimensions                                            */
/* ---------------------------------------------------------------------- */

const variantAttributeIds =
    Array.from(
        new Set(
            (
                product.variants ??
                []
            ).flatMap(
                variant => {
                    const ids: string[] =
                        [];

                    /*
                     * Generic attributes
                     */
                    for (
                        const attribute of
                        variant.attributes ?? []
                    ) {
                        const code =
                            normalize(
                                attribute.attributeCode,
                            );

                        if (!code) {
                            continue;
                        }

                        const catalogAttribute =
                            findCatalogAttributeByCode(
                                code,
                            );

                        if (
                            catalogAttribute &&
                            catalogAttribute.isActive &&
                            catalogAttribute.isVariantAttribute
                        ) {
                            ids.push(
                                catalogAttribute.id,
                            );
                        }
                    }

                    /*
                     * Legacy Color
                     */
                    if (
                        variant.color
                    ) {
                        const colorAttribute =
                            findCatalogAttributeByCode(
                                'color',
                            );

                        if (
                            colorAttribute &&
                            colorAttribute.isActive &&
                            colorAttribute.isVariantAttribute
                        ) {
                            ids.push(
                                colorAttribute.id,
                            );
                        }
                    }

                    /*
                     * Legacy Size
                     */
                    if (
                        variant.size
                    ) {
                        const sizeAttribute =
                            findCatalogAttributeByCode(
                                'size',
                            );

                        if (
                            sizeAttribute &&
                            sizeAttribute.isActive &&
                            sizeAttribute.isVariantAttribute
                        ) {
                            ids.push(
                                sizeAttribute.id,
                            );
                        }
                    }

                    return ids;
                },
            ),
        ),
    );
    /* ---------------------------------------------------------------------- */
    /* Variants                                                               */
    /* ---------------------------------------------------------------------- */
const variants =
    (
        product.variants ??
        []
    ).map(
        variant => {
            const resolvedAttributeValueIds: string[] =
                [];

            /*
             * ------------------------------------------------------------
             * Generic backend Variant Attributes
             * ------------------------------------------------------------
             */
            for (
                const attribute of
                variant.attributes ?? []
            ) {
                const attributeCode =
                    normalize(
                        attribute.attributeCode,
                    );

                if (!attributeCode) {
                    continue;
                }

                const catalogAttribute =
                    findCatalogAttributeByCode(
                        attributeCode,
                    );

                if (!catalogAttribute) {
                    continue;
                }

                const catalogValue =
                    findCatalogValue(
                        catalogAttribute,
                        {
                            /*
                             * ProductVariantAttribute.attributeValueId
                             * is a Product-side AttributeValue ID.
                             *
                             * Do NOT use it as Catalog Value ID.
                             */
                            value:
                                attribute.value,

                            displayValue:
                                attribute.displayValue,
                        },
                    );

                if (
                    catalogValue?.id
                ) {
                    resolvedAttributeValueIds.push(
                        catalogValue.id,
                    );
                }
            }

            /*
             * ------------------------------------------------------------
             * Legacy Color fallback
             * ------------------------------------------------------------
             *
             * This keeps older variants editable even when generic
             * attributes were not returned for some legacy data.
             */
            if (
                !resolvedAttributeValueIds.some(
                    valueId =>
                        (
                            findCatalogAttributeByCode(
                                'color',
                            )?.values ?? []
                        ).some(
                            value =>
                                value.id ===
                                valueId,
                        ),
                ) &&
                variant.color
            ) {
                const colorAttribute =
                    findCatalogAttributeByCode(
                        'color',
                    );

                const colorValue =
                    findCatalogValue(
                        colorAttribute as CatalogAttribute,
                        {
                            value:
                                variant.color,
                            displayValue:
                                variant.color,
                        },
                    );

                if (
                    colorValue?.id
                ) {
                    resolvedAttributeValueIds.push(
                        colorValue.id,
                    );
                }
            }

            /*
             * ------------------------------------------------------------
             * Legacy Size fallback
             * ------------------------------------------------------------
             */
            if (
                !resolvedAttributeValueIds.some(
                    valueId =>
                        (
                            findCatalogAttributeByCode(
                                'size',
                            )?.values ?? []
                        ).some(
                            value =>
                                value.id ===
                                valueId,
                        ),
                ) &&
                variant.size
            ) {
                const sizeAttribute =
                    findCatalogAttributeByCode(
                        'size',
                    );

                const sizeValue =
                    findCatalogValue(
                        sizeAttribute as CatalogAttribute,
                        {
                            value:
                                variant.size,
                            displayValue:
                                variant.size,
                        },
                    );

                if (
                    sizeValue?.id
                ) {
                    resolvedAttributeValueIds.push(
                        sizeValue.id,
                    );
                }
            }

            /*
             * ------------------------------------------------------------
             * Remove duplicates while preserving stable order.
             * ------------------------------------------------------------
             */
            const attributeValueIds =
                Array.from(
                    new Set(
                        resolvedAttributeValueIds,
                    ),
                );

            return {
                /*
                 * REAL persisted ProductVariant ID.
                 *
                 * Never replace this with react-hook-form's
                 * fieldId.
                 */
                id:
                    variant.id,

                sku:
                    variant.sku ??
                    '',

                priceOverride:
                    variant.priceOverride ??
                    undefined,

                /*
                 * Existing stock remains display-only.
                 */
                stockQuantity:
                    variant.stockQuantity ??
                    0,

                isActive:
                    variant.isActive ??
                    true,

                /*
                 * IMPORTANT:
                 * The form stores CatalogAttributeValue IDs.
                 */
                attributeValueIds,
            };
        },
    );


    /* ---------------------------------------------------------------------- */
    /* Images                                                                 */
    /* ---------------------------------------------------------------------- */

    const images =
        (
            product.images ??
            []
        ).map(
            image => ({
                imageUrl:
                    image.imageUrl ??
                    '',

                altText:
                    image.altText ??
                    '',

                isMain:
                    image.isMain ??
                    false,
            }),
        );

    /* ---------------------------------------------------------------------- */
    /* Final Form Values                                                      */
    /* ---------------------------------------------------------------------- */

    return {
        name:
            product.name ??
            '',

        slug:
            product.slug ??
            '',

        description:
            product.description ??
            '',

        shortDescription:
            product.shortDescription ??
            '',

        price:
            product.price ??
            0,

        currency:
            product.currency ||
            'IRR',

        comparePrice:
            product.comparePrice ??
            null,

        sku:
            product.sku ??
            '',

        brandId:
            product.brandId ??
            null,

        categoryIds:
            product.categoryIds ??
            [],

        isActive:
            product.isActive,

        isPublished:
            product.isPublished,

        isFeatured:
            product.isFeatured,

        discountPercentage:
            product.discountPercentage ??
            null,

        variantAttributeIds,

        specifications,

        variants,

        images,
    };
}




/* -------------------------------------------------------------------------- */
/*                    Local Product Attribute Structures                      */
/* -------------------------------------------------------------------------- */

/*
 * ProductAttribute on the local frontend side appears to use:
 *
 * {
 *   id,
 *   name,
 *   code,
 *   values[]
 * }
 */
type ProductAttributeRecord = {
    id?: string;
    name?: string | null;
    code?: string | null;

    values?: Array<{
        id?: string;
        value?: string | null;
        displayValue?: string | null;
        colorHex?: string | null;
    }>;
};

/* -------------------------------------------------------------------------- */
/*                  Product Specifications → API DTO                          */
/* -------------------------------------------------------------------------- */

/*
 * IMPORTANT
 *
 * Your local products.ts currently defines:
 *
 *   attributes: ProductAttributeInput[];
 *
 * and the compiler error shows ProductAttributeInput contains
 * a required `values` collection.
 *
 * Therefore ProductForm must send grouped Product Attributes:
 *
 * [
 *   {
 *     name: "Material",
 *     code: "material",
 *     values: [
 *       {
 *         value: "Cotton",
 *         displayValue: "Cotton"
 *       }
 *     ]
 *   }
 * ]
 */
function buildProductAttributePayload(
    specifications:
        FormValues['specifications'],

    catalogAttributes:
        | CatalogAttribute[]
        | undefined,
): CreateProductDto['attributes'] {
    if (
        !specifications.length ||
        !catalogAttributes?.length
    ) {
        return [];
    }

    const groups =
        new Map<
            string,
            {
                catalogAttributeId: string;
                name: string;
                code: string;
                values: Array<{
                    value: string;
                    displayValue?: string;
                    colorHex?: string;
                }>;
            }
        >();

    for (
        const specification
        of specifications
    ) {
        const catalogAttribute =
            catalogAttributes.find(
                attribute =>
                    attribute.id ===
                    specification.catalogAttributeId,
            );

        if (
            !catalogAttribute
        ) {
            continue;
        }

        const selectedValue =
            specification.catalogAttributeValueId
                ? catalogAttribute.values.find(
                    value =>
                        value.id ===
                        specification.catalogAttributeValueId,
                )
                : undefined;

        const rawValue =
            (
                selectedValue?.value ??
                specification.value ??
                ''
            ).trim();

        if (!rawValue) {
            continue;
        }

        const displayValue =
            (
                selectedValue?.displayValue ??
                specification.displayValue ??
                rawValue
            ).trim();

        const code =
            catalogAttribute.code
                .trim();

        if (!code) {
            continue;
        }

        let group =
            groups.get(
                code.toLowerCase(),
            );

        if (!group) {
            group = {
                catalogAttributeId:
                    catalogAttribute.id,

                name:
                    catalogAttribute.name,

                code,

                values: [],
            };

            groups.set(
                code.toLowerCase(),
                group,
            );
        }

        const alreadyExists =
            group.values.some(
                item =>
                    item.value
                        .trim()
                        .toLowerCase() ===
                    rawValue
                        .trim()
                        .toLowerCase(),
            );

        if (
            !alreadyExists
        ) {
            group.values.push({
                value:
                    rawValue,

                displayValue:
                    displayValue ||
                    undefined,

                colorHex:
                    selectedValue?.colorHex ??
                    specification.colorHex ??
                    undefined,
            });
        }
    }

    /*
     * The local DTO type is already defined in products.ts.
     *
     * The structural payload above matches the ProductAttributeInput
     * shape indicated by the compiler (`name`, `code`, `values`).
     *
     * `unknown` is used only to bridge any extra compile-time
     * fields added in the local working tree.
     */
    return Array.from(
        groups.values(),
    ) as unknown as CreateProductDto['attributes'];
}


/* -------------------------------------------------------------------------- */
/*                              Props Types                                  */
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
        t,
    } = useTranslation();

    const {
        product,
        mode,
        pending,
        onSubmit,
        onCancel,
    } = props;


    /* ---------------------------------------------------------------------- */
    /*                                  Data                                  */
    /* ---------------------------------------------------------------------- */

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


    /* ---------------------------------------------------------------------- */
    /*                         Variant Attributes                              */
    /* ---------------------------------------------------------------------- */

    const variantAttributes =
        useMemo(
            () =>
                (
                    attributes ??
                    []
                )
                    .filter(
                        attribute =>
                            attribute.isActive &&
                            attribute.isVariantAttribute,
                    )
                    .map(
                        attribute => ({
                            ...attribute,

                            values:
                                (
                                    attribute.values ??
                                    []
                                )
                                    .filter(
                                        value =>
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
                    .filter(
                        attribute =>
                            attribute.values.length >
                            0,
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




    /* ---------------------------------------------------------------------- */
    /*                                  Form                                  */
    /* ---------------------------------------------------------------------- */

    const form =
        useForm<FormValues>({
            resolver:
                zodResolver(
                    schema,
                ) as never,

            defaultValues:
                toValues(
                    product, attributes,
                ),

            mode:
                'onBlur',
        });


    const selectedVariantAttributeIds =
        form.watch(
            'variantAttributeIds',
        ) ?? [];


    const specificationAttributes =
        useMemo(
            () =>
                (
                    attributes ?? []
                )
                    .filter(
                        attribute =>
                            attribute.isActive &&
                            !selectedVariantAttributeIds.includes(
                                attribute.id,
                            ),
                    )
                    .map(
                        attribute => ({
                            ...attribute,

                            values:
                                (
                                    attribute.values ??
                                    []
                                )
                                    .filter(
                                        value =>
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
            [
                attributes,
                selectedVariantAttributeIds,
            ],
        );


    const activeVariantAttributes =
        variantAttributes.filter(
            attribute =>
                selectedVariantAttributeIds.includes(
                    attribute.id,
                ),
        );


    /* ---------------------------------------------------------------------- */
    /*                              Field Arrays                               */
    /* ---------------------------------------------------------------------- */

    const {
        fields:
        specificationFields,

        append:
        addSpecification,

        remove:
        removeSpecification,
    } =
        useFieldArray({
            control:
                form.control,

            name:
                'specifications',
        });


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

            keyName:
                'fieldId',
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


    /* ---------------------------------------------------------------------- */
    /*                              Helper Maps                                */
    /* ---------------------------------------------------------------------- */

    const variantValueIdsByAttributeId =
        useMemo(
            () => {
                const map =
                    new Map<
                        string,
                        Set<string>
                    >();

                for (
                    const attribute of
                    variantAttributes
                ) {
                    map.set(
                        attribute.id,
                        new Set(
                            attribute.values.map(
                                value =>
                                    value.id,
                            ),
                        ),
                    );
                }

                return map;
            },
            [
                variantAttributes,
            ],
        );


    const selectedVariantValueIds =
        useMemo(
            () =>
                new Set(
                    activeVariantAttributes.flatMap(
                        attribute =>
                            attribute.values.map(
                                value =>
                                    value.id,
                            ),
                    ),
                ),
            [
                activeVariantAttributes,
            ],
        );


    /* ---------------------------------------------------------------------- */
    /*                                 Reset                                  */
    /* ---------------------------------------------------------------------- */

  
useEffect(() => {
    /*
     * Create mode can be initialized immediately.
     */
    if (mode === 'create') {
        form.reset(
            toValues(
                undefined,
                attributes,
            ),
        );

        return;
    }

    /*
     * Edit mode:
     *
     * Do NOT reset the form before the Catalog Attributes
     * are available.
     *
     * The Variant form stores CatalogAttributeValue IDs.
     * Without the catalog data we cannot correctly reconstruct
     * those IDs from the Product response.
     */
    if (
        mode === 'edit' &&
        product &&
        attributes &&
        attributes.length > 0
    ) {
        form.reset(
            toValues(
                product,
                attributes,
            ),
        );
    }
}, [
    mode,
    product?.id,
    product?.updatedAt,
    attributes,
    form,
]);


       


    /* ---------------------------------------------------------------------- */
    /*                     Variant Dimension Selection                        */
    /* ---------------------------------------------------------------------- */

    const handleVariantAttributeSelection =
        (
            attributeId:
                string,

            checked:
                boolean,
        ) => {
            const currentIds =
                form.getValues(
                    'variantAttributeIds',
                ) ?? [];

            const nextIds =
                checked
                    ? Array.from(
                        new Set([
                            ...currentIds,
                            attributeId,
                        ]),
                    )
                    : currentIds.filter(
                        id =>
                            id !==
                            attributeId,
                    );

            /*
             * If a dimension is removed, remove all its values
             * from every Variant.
             */
            if (!checked) {
                const removedValueIds =
                    variantValueIdsByAttributeId.get(
                        attributeId,
                    );

                if (
                    removedValueIds &&
                    removedValueIds.size >
                    0
                ) {
                    const variants =
                        form.getValues(
                            'variants',
                        ) ?? [];

                    variants.forEach(
                        (
                            _variant,
                            index,
                        ) => {
                            const currentValueIds =
                                form.getValues(
                                    `variants.${index}.attributeValueIds`,
                                ) ?? [];

                            form.setValue(
                                `variants.${index}.attributeValueIds`,
                                currentValueIds.filter(
                                    valueId =>
                                        !removedValueIds.has(
                                            valueId,
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
                        },
                    );
                }
            }

            form.setValue(
                'variantAttributeIds',
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
        };


    /* ---------------------------------------------------------------------- */
    /*                          Variant Helpers                                */
    /* ---------------------------------------------------------------------- */

    const getSelectedValueIdForAttribute =
        (
            attribute:
                CatalogAttribute,

            selectedIds:
                string[],
        ): string =>
            selectedIds.find(
                valueId =>
                    attribute.values.some(
                        value =>
                            value.id ===
                            valueId,
                    ),
            ) ?? '';

    const setVariantAttributeValue =
        (
            variantIndex: number,
            attribute: CatalogAttribute,
            nextValueId: string,
        ) => {
            const fieldName =
                `variants.${variantIndex}.attributeValueIds` as const;

            const currentIds =
                form.getValues(
                    fieldName,
                ) ?? [];

            const currentAttributeValueIds =
                new Set(
                    (
                        attribute.values ??
                        []
                    ).map(
                        value =>
                            value.id,
                    ),
                );

            const idsWithoutCurrentAttribute =
                currentIds.filter(
                    valueId =>
                        !currentAttributeValueIds.has(
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

            const normalizedIds =
                Array.from(
                    new Set(
                        nextIds,
                    ),
                );

            /*
             * Do not allow two active variants to use the
             * exact same attribute combination.
             */
            if (
                hasDuplicateVariantCombination(
                    variantIndex,
                    normalizedIds,
                )
            ) {
                /*
                 * Keep the previous selection.
                 * The server performs the same validation on Save.
                 */
                return;
            }

            form.setValue(
                fieldName,
                normalizedIds,
                {
                    shouldDirty: true,
                    shouldTouch: true,
                    shouldValidate: true,
                },
            );
        };
    
const getVariantCombinationSignature =
    (
        attributeValueIds: string[],
    ): string => {
        return Array.from(
            new Set(
                attributeValueIds,
            ),
        )
            .sort()
            .join('|');
    };


const hasDuplicateVariantCombination =
    (
        variantIndex: number,
        nextIds: string[],
    ): boolean => {
        const currentVariants =
            form.getValues(
                'variants',
            ) ?? [];

        const nextSignature =
            getVariantCombinationSignature(
                nextIds,
            );

        return currentVariants.some(
            (
                variant,
                index,
            ) => {
                if (
                    index ===
                    variantIndex
                ) {
                    return false;
                }

                if (
                    !variant.isActive
                ) {
                    return false;
                }

                const otherSignature =
                    getVariantCombinationSignature(
                        variant.attributeValueIds ??
                        [],
                    );

                return (
                    nextSignature ===
                    otherSignature
                );
            },
        );
    };




    const sanitizeVariantAttributeValueIds =
        (
            valueIds:
                string[],
        ): string[] => {
            if (
                selectedVariantValueIds.size ===
                0
            ) {
                return [];
            }

            return Array.from(
                new Set(
                    valueIds.filter(
                        valueId =>
                            selectedVariantValueIds.has(
                                valueId,
                            ),
                    ),
                ),
            );
        };


    /* ---------------------------------------------------------------------- */
    /*                         Specification Helpers                          */
    /* ---------------------------------------------------------------------- */

    const getSpecificationAttribute =
        (
            attributeId:
                string,
        ) =>
            specificationAttributes.find(
                attribute =>
                    attribute.id ===
                    attributeId,
            );


    const getSpecificationValue =
        (
            attributeId:
                string,

            valueId:
                string |
                undefined,
        ) => {
            if (!valueId) {
                return undefined;
            }

            const attribute =
                getSpecificationAttribute(
                    attributeId,
                );

            return attribute?.values.find(
                value =>
                    value.id ===
                    valueId,
            );
        };


    const handleSpecificationAttributeChange =
        (
            index:
                number,

            attributeId:
                string,
        ) => {
            const attribute =
                getSpecificationAttribute(
                    attributeId,
                );

            form.setValue(
                `specifications.${index}.catalogAttributeId`,
                attributeId,
                {
                    shouldDirty:
                        true,

                    shouldTouch:
                        true,

                    shouldValidate:
                        true,
                },
            );

            form.setValue(
                `specifications.${index}.catalogAttributeValueId`,
                '',
                {
                    shouldDirty:
                        true,

                    shouldValidate:
                        true,
                },
            );

            form.setValue(
                `specifications.${index}.value`,
                '',
                {
                    shouldDirty:
                        true,

                    shouldValidate:
                        true,
                },
            );

            form.setValue(
                `specifications.${index}.displayValue`,
                '',
                {
                    shouldDirty:
                        true,
                },
            );

            form.setValue(
                `specifications.${index}.colorHex`,
                '',
                {
                    shouldDirty:
                        true,
                },
            );

            /*
             * If there is exactly one possible Catalog Value,
             * select it automatically.
             */
            if (
                attribute &&
                attribute.values.length ===
                1
            ) {
                const onlyValue =
                    attribute.values[0];

                form.setValue(
                    `specifications.${index}.catalogAttributeValueId`,
                    onlyValue.id,
                    {
                        shouldDirty:
                            true,

                        shouldValidate:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.value`,
                    onlyValue.value,
                    {
                        shouldDirty:
                            true,

                        shouldValidate:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.displayValue`,
                    onlyValue.displayValue ??
                    onlyValue.value,
                    {
                        shouldDirty:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.colorHex`,
                    onlyValue.colorHex ??
                    undefined,
                    {
                        shouldDirty:
                            true,
                    },
                );
            }
        };


    const handleSpecificationValueChange =
        (
            index:
                number,

            attributeId:
                string,

            valueId:
                string,
        ) => {
            const selectedValue =
                getSpecificationValue(
                    attributeId,
                    valueId,
                );

            form.setValue(
                `specifications.${index}.catalogAttributeValueId`,
                valueId ||
                undefined,
                {
                    shouldDirty:
                        true,

                    shouldTouch:
                        true,

                    shouldValidate:
                        true,
                },
            );

            if (
                selectedValue
            ) {
                form.setValue(
                    `specifications.${index}.value`,
                    selectedValue.value,
                    {
                        shouldDirty:
                            true,

                        shouldValidate:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.displayValue`,
                    selectedValue.displayValue ??
                    selectedValue.value,
                    {
                        shouldDirty:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.colorHex`,
                    selectedValue.colorHex ??
                    undefined,
                    {
                        shouldDirty:
                            true,
                    },
                );
            } else {
                form.setValue(
                    `specifications.${index}.value`,
                    '',
                    {
                        shouldDirty:
                            true,

                        shouldValidate:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.displayValue`,
                    '',
                    {
                        shouldDirty:
                            true,
                    },
                );

                form.setValue(
                    `specifications.${index}.colorHex`,
                    '',
                    {
                        shouldDirty:
                            true,
                    },
                );
            }
        };


    const handleAddSpecification = () => {
        const availableAttribute =
            specificationAttributes.find(
                attribute =>
                    !form
                        .getValues('specifications')
                        .some(
                            specification =>
                                specification.catalogAttributeId ===
                                attribute.id,
                        ),
            );

        if (!availableAttribute) {
            return;
        }

        addSpecification({
            catalogAttributeId:
                availableAttribute.id,

            catalogAttributeValueId:
                '',

            value:
                '',

            displayValue:
                '',

            colorHex:
                '',

            displayOrder:
                specificationFields.length,
        });
    };



    /* ---------------------------------------------------------------------- */
    /*                          Specifications Validation                      */
    /* ---------------------------------------------------------------------- */

    const validateSpecifications =
        (
            specifications:
                FormValues['specifications'],
        ) => {
            const usedAttributeIds =
                new Set<string>();

            for (
                const specification
                of specifications
            ) {
                const attributeId =
                    specification.catalogAttributeId
                        ?.trim();

                if (!attributeId) {
                    throw new Error(
                        t(
                            'productEdit.specifications.errors.attributeRequired',
                        ),
                    );
                }

                if (
                    usedAttributeIds.has(
                        attributeId,
                    )
                ) {
                    throw new Error(
                        t(
                            'productEdit.specifications.errors.duplicateAttribute',
                        ),
                    );
                }

                usedAttributeIds.add(
                    attributeId,
                );

                const attribute =
                    getSpecificationAttribute(
                        attributeId,
                    );

                if (!attribute) {
                    throw new Error(
                        t(
                            'productEdit.specifications.errors.invalidAttribute',
                        ),
                    );
                }

                /*
                 * Predefined Catalog Values:
                 * the administrator must select one.
                 *
                 * Free-text Catalog Attributes:
                 * a text value is required.
                 */
                if (
                    attribute.values.length >
                    0
                ) {
                    if (
                        !specification.catalogAttributeValueId
                    ) {
                        throw new Error(
                            t(
                                'productEdit.specifications.errors.valueRequired',
                                {
                                    attribute:
                                        attribute.name,
                                },
                            ),
                        );
                    }
                } else {
                    if (
                        !specification.value?.trim()
                    ) {
                        throw new Error(
                            t(
                                'productEdit.specifications.errors.textRequired',
                                {
                                    attribute:
                                        attribute.name,
                                },
                            ),
                        );
                    }
                }
            }
        };


    /* ---------------------------------------------------------------------- */
    /*                           Variant Validation                            */
    /* ---------------------------------------------------------------------- */

    const validateVariants =
        (
            values:
                FormValues,
        ) => {
            const selectedAttributes =
                activeVariantAttributes;

            const variants =
                values.variants ??
                [];

            /*
             * Once dimensions are selected,
             * there must be at least one Variant row.
             */
            if (
                selectedAttributes.length >
                0 &&
                variants.length ===
                0
            ) {
                throw new Error(
                    t(
                        'productEdit.variants.errors.noVariantForDimensions',
                    ),
                );
            }

            /*
             * There must always be at least one active Variant
             * when Variant rows exist.
             */
            if (
                variants.length >
                0 &&
                !variants.some(
                    variant =>
                        variant.isActive,
                )
            ) {
                throw new Error(
                    t(
                        'productEdit.variants.errors.noActiveVariant',
                    ),
                );
            }

            const signatures =
                new Set<string>();

            for (
                const variant
                of variants
            ) {
                if (
                    !variant.isActive
                ) {
                    continue;
                }

                const selectedIds =
                    variant.attributeValueIds ??
                    [];

                const combination =
                    selectedAttributes.map(
                        attribute => {
                            const valueId =
                                getSelectedValueIdForAttribute(
                                    attribute,
                                    selectedIds,
                                );

                            /*
                             * Required Variant Attributes must
                             * have a value.
                             */
                            if (
                                !valueId &&
                                attribute.isRequired
                            ) {
                                throw new Error(
                                    t(
                                        'productEdit.variants.errors.missingAttribute',
                                        {
                                            sku:
                                                variant.sku?.trim() ||
                                                t(
                                                    'productEdit.variants.unnamedVariant',
                                                ),

                                            attribute:
                                                attribute.name,
                                        },
                                    ),
                                );
                            }

                            return (
                                valueId ||
                                ''
                            );
                        },
                    );

                /*
                 * Empty combination is allowed only for a
                 * product with no Variant dimensions.
                 */
                if (
                    selectedAttributes.length >
                    0
                ) {
                    /*
                     * If there are selected dimensions but at
                     * least one optional dimension is empty,
                     * we still need a deterministic signature.
                     */
                    const signature =
                        combination.join(
                            '|',
                        );

                    if (
                        signatures.has(
                            signature,
                        )
                    ) {
                        throw new Error(
                            t(
                                'productEdit.variants.errors.duplicateCombination',
                                {
                                    sku:
                                        variant.sku?.trim() ||
                                        t(
                                            'productEdit.variants.unnamedVariant',
                                        ),
                                },
                            ),
                        );
                    }

                    signatures.add(
                        signature,
                    );
                }
            }
        };


    /* ---------------------------------------------------------------------- */
    /*                            Image Helpers                                */
    /* ---------------------------------------------------------------------- */

    const getOrderedImageUrls =
        (
            images:
                FormValues['images'],
        ): string[] => {
            const validImages =
                images
                    .filter(
                        image =>
                            Boolean(
                                image.imageUrl?.trim(),
                            ),
                    )
                    .map(
                        image => ({
                            ...image,

                            imageUrl:
                                image.imageUrl.trim(),
                        }),
                    );

            if (
                validImages.length <=
                1
            ) {
                return validImages.map(
                    image =>
                        image.imageUrl,
                );
            }

            const mainIndex =
                validImages.findIndex(
                    image =>
                        image.isMain,
                );

            if (
                mainIndex > 0
            ) {
                const [
                    mainImage,
                ] =
                    validImages.splice(
                        mainIndex,
                        1,
                    );

                validImages.unshift(
                    mainImage,
                );
            }

            return validImages.map(
                image =>
                    image.imageUrl,
            );
        };


    const handleAddImage =
        (
            url:
                string,
        ) => {
            const cleanUrl =
                url.trim();

            if (!cleanUrl) {
                return;
            }

            addImage({
                imageUrl:
                    cleanUrl,

                altText:
                    '',

                isMain:
                    imageFields.length ===
                    0,
            });
        };


    /* ---------------------------------------------------------------------- */
    /*                                  Data                                  */
    /* ---------------------------------------------------------------------- */

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
    /*                               Submit                                   */
    /* ---------------------------------------------------------------------- */

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
                mode ===
                    'create'
                    ? t(
                        'productEdit.messages.created',
                    )
                    : t(
                        'productEdit.messages.updated',
                    ),

            onSuccess:
                onCancel,

            transform:
                (
                    values,
                ) => {
                    validateSpecifications(
                        values.specifications,
                    );

                    validateVariants(
                        values,
                    );

                    const productAttributes =
                        buildProductAttributePayload(
                            values.specifications,
                            specificationAttributes,
                        );

                    /* ---------------------------------------------------------- */
                    /*                           CREATE                           */
                    /* ---------------------------------------------------------- */

                    if (
                        mode ===
                        'create'
                    ) {
                        let variants =
                            values.variants.map(
                                variant => ({
                                    sku:
                                        variant.sku
                                            .trim(),

                                    priceOverride:
                                        variant.priceOverride,

                                    /*
                                     * New Variant stock always starts at 0.
                                     * Inventory becomes the stock source.
                                     */
                                    stockQuantity:
                                        0,

                                    attributeValueIds:
                                        sanitizeVariantAttributeValueIds(
                                            variant.attributeValueIds ??
                                            [],
                                        ),
                                }),
                            );

                        /*
                         * Simple product:
                         * no Variant dimensions and no manual Variant.
                         */
                        if (
                            variants.length ===
                            0 &&
                            selectedVariantAttributeIds.length ===
                            0
                        ) {
                            const baseSku =
                                values.sku
                                    ?.trim() ||
                                'PRODUCT';

                            variants = [
                                {
                                    sku:
                                        `${baseSku}-DEFAULT`,

                                    priceOverride:
                                        undefined,

                                    stockQuantity:
                                        0,

                                    attributeValueIds:
                                        [],
                                },
                            ];
                        }

                        return {
                            attributes:
                                productAttributes,
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
                                        image =>
                                            image.imageUrl
                                                .trim(),
                                    )
                                    .filter(
                                        Boolean,
                                    ),
                        } satisfies CreateProductDto;
                    }


                    /* ---------------------------------------------------------- */
                    /*                           UPDATE                           */
                    /* ---------------------------------------------------------- */

                    const existingVariantIds =
                        new Set(
                            (product?.variants ?? [])
                                .map(
                                    variant =>
                                        variant.id,
                                )
                                .filter(
                                    (
                                        id,
                                    ): id is string =>
                                        Boolean(id),
                                ),
                        );

                    const variants =
                        values.variants.map(
                            variant => {
                                const variantId =
                                    variant.id &&
                                        existingVariantIds.has(
                                            variant.id,
                                        )
                                        ? variant.id
                                        : undefined;

                                return {
                                    id:
                                        variantId,

                                    sku:
                                        variant.sku.trim(),

                                    priceOverride:
                                        variant.priceOverride ??
                                        null,

                                    isActive:
                                        variant.isActive,

                                    attributeValueIds:
                                        sanitizeVariantAttributeValueIds(
                                            variant.attributeValueIds ??
                                            [],
                                        ),
                                };
                            },
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

                        attributes:
                            productAttributes,

                        images:
                            getOrderedImageUrls(
                                values.images,
                            ),

                        isActive:
                            values.isActive,

                        isFeatured:
                            values.isFeatured,

                        isPublished:
                            values.isPublished,

                        variants,
                    } satisfies UpdateProductDto;
                },
        });


    /* ---------------------------------------------------------------------- */
    /*                         Variant Actions                                 */
    /* ---------------------------------------------------------------------- */

    const handleAddVariant = () => {
        addVariant({
            sku: '',
            priceOverride: undefined,
            stockQuantity: 0,
            isActive: true,
            attributeValueIds: [],
        });
    };

    /* ---------------------------------------------------------------------- */
    /*                                    UI                                  */
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


                {/* ============================================================ */}
                {/* Basic Information                                             */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'productEdit.basicInformation.title',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'productEdit.basicInformation.description',
                            )}
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
                                        {t(
                                            'productEdit.fields.productName',
                                        )}
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
                                            {t(
                                                'productEdit.fields.slug',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder={
                                                    mode ===
                                                        'create'
                                                        ? t(
                                                            'productEdit.fields.generatedAutomatically',
                                                        )
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
                                            {t(
                                                'productEdit.fields.sku',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder={t(
                                                    'productEdit.fields.productSku',
                                                )}
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
                                        {t(
                                            'productEdit.fields.description',
                                        )}
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
                                        {t(
                                            'productEdit.fields.shortDescription',
                                        )}
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


                {/* ============================================================ */}
                {/* Pricing                                                       */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'productEdit.pricing.title',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'productEdit.pricing.description',
                            )}
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
                                            {t(
                                                'productEdit.fields.price',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                type="number"
                                                min={0}
                                                step={1000}
                                                value={
                                                    field.value
                                                }
                                                onChange={event =>
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
                                            {t(
                                                'productEdit.fields.comparePrice',
                                            )}
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
                                                onChange={event =>
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

                        <FormGrid columns={2}>
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
                                            {t(
                                                'productEdit.fields.discountPercentage',
                                            )}
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
                                                onChange={event =>
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
                                            {t(
                                                'productEdit.fields.currency',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder="IRR"
                                                maxLength={10}
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>
                    </CardContent>
                </Card>


                {/* ============================================================ */}
                {/* Brand & Categories                                           */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'productEdit.brandCategories.title',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'productEdit.brandCategories.description',
                            )}
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
                                        {t(
                                            'productEdit.fields.brand',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <select
                                            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                                            value={
                                                field.value ??
                                                ''
                                            }
                                            onChange={event =>
                                                field.onChange(
                                                    event
                                                        .target
                                                        .value ||
                                                    null,
                                                )
                                            }
                                        >
                                            <option value="">
                                                {t(
                                                    'productEdit.fields.selectBrand',
                                                )}
                                            </option>

                                            {brands.map(
                                                brand => (
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
                                        {t(
                                            'productEdit.fields.categories',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <select
                                            multiple
                                            className="flex h-32 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                                            value={
                                                field.value
                                            }
                                            onChange={event =>
                                                field.onChange(
                                                    Array.from(
                                                        event
                                                            .target
                                                            .selectedOptions,
                                                        option =>
                                                            option.value,
                                                    ),
                                                )
                                            }
                                        >
                                            {categories.map(
                                                category => (
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


                {/* ============================================================ */}
                {/* Product Specifications                                       */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                            <div>
                                <CardTitle>
                                    {t(
                                        'productEdit.specifications.title',
                                    )}
                                </CardTitle>

                                <CardDescription>
                                    {t(
                                        'productEdit.specifications.description',
                                    )}
                                </CardDescription>
                            </div>

                            <Button
                                type="button"
                                variant="outline"
                                onClick={
                                    handleAddSpecification
                                }
                                disabled={
                                    specificationAttributes.length ===
                                    0 ||
                                    specificationFields.length >=
                                    specificationAttributes.length
                                }
                            >
                                <Plus className="size-4" />

                                {t(
                                    'productEdit.specifications.add',
                                )}
                            </Button>
                        </div>
                    </CardHeader>

                    <CardContent>
                        {specificationFields.length === 0 ? (
                            <div className="rounded-lg border border-dashed p-8 text-center">
                                <p className="text-muted-foreground text-sm">
                                    {t(
                                        'productEdit.specifications.empty',
                                    )}
                                </p>

                                {specificationAttributes.length ===
                                    0 && (
                                        <p className="text-muted-foreground mt-2 text-xs">
                                            {t(
                                                'productEdit.specifications.noAvailableAttributes',
                                            )}
                                        </p>
                                    )}
                            </div>
                        ) : (
                            <div className="space-y-4">
                                {specificationFields.map(
                                    (
                                        field,
                                        index,
                                    ) => {
                                        const attributeId =
                                            form.watch(
                                                `specifications.${index}.catalogAttributeId`,
                                            );

                                        const attribute =
                                            specificationAttributes.find(
                                                item =>
                                                    item.id ===
                                                    attributeId,
                                            );

                                        const valueId =
                                            form.watch(
                                                `specifications.${index}.catalogAttributeValueId`,
                                            );

                                        return (
                                            <div
                                                key={field.id}
                                                className="rounded-lg border p-4"
                                            >
                                                <div className="grid gap-4 md:grid-cols-[1fr_1fr_auto] md:items-end">
                                                    {/* Attribute */}
                                                    <FormField
                                                        control={
                                                            form.control
                                                        }
                                                        name={`specifications.${index}.catalogAttributeId`}
                                                        render={() => (
                                                            <FormItem>
                                                                <FormLabel>
                                                                    {t(
                                                                        'productEdit.specifications.attribute',
                                                                    )}
                                                                </FormLabel>

                                                                <FormControl>
                                                                    <select
                                                                        value={
                                                                            attributeId
                                                                        }
                                                                        onChange={(
                                                                            event,
                                                                        ) =>
                                                                            handleSpecificationAttributeChange(
                                                                                index,
                                                                                event
                                                                                    .target
                                                                                    .value,
                                                                            )
                                                                        }
                                                                        className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                                                    >
                                                                        <option value="">
                                                                            {t(
                                                                                'productEdit.specifications.selectAttribute',
                                                                            )}
                                                                        </option>

                                                                        {specificationAttributes
                                                                            .filter(
                                                                                item =>
                                                                                    item.id ===
                                                                                    attributeId ||
                                                                                    !form
                                                                                        .getValues(
                                                                                            'specifications',
                                                                                        )
                                                                                        .some(
                                                                                            (
                                                                                                specification,
                                                                                                specificationIndex,
                                                                                            ) =>
                                                                                                specificationIndex !==
                                                                                                index &&
                                                                                                specification.catalogAttributeId ===
                                                                                                item.id,
                                                                                        ),
                                                                            )
                                                                            .map(
                                                                                item => (
                                                                                    <option
                                                                                        key={
                                                                                            item.id
                                                                                        }
                                                                                        value={
                                                                                            item.id
                                                                                        }
                                                                                    >
                                                                                        {
                                                                                            item.name
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

                                                    {/* Value */}
                                                    <FormField
                                                        control={
                                                            form.control
                                                        }
                                                        name={`specifications.${index}.catalogAttributeValueId`}
                                                        render={() => (
                                                            <FormItem>
                                                                <FormLabel>
                                                                    {t(
                                                                        'productEdit.specifications.value',
                                                                    )}
                                                                </FormLabel>

                                                                {attribute &&
                                                                    attribute.values.length >
                                                                    0 ? (
                                                                    <FormControl>
                                                                        <select
                                                                            value={
                                                                                valueId
                                                                            }
                                                                            onChange={(
                                                                                event,
                                                                            ) =>
                                                                                handleSpecificationValueChange(
                                                                                    index,
                                                                                    attribute.id,
                                                                                    event
                                                                                        .target
                                                                                        .value,
                                                                                )
                                                                            }
                                                                            className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                                                        >
                                                                            <option value="">
                                                                                {t(
                                                                                    'productEdit.specifications.selectValue',
                                                                                )}
                                                                            </option>

                                                                            {attribute.values.map(
                                                                                value => (
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
                                                                    </FormControl>
                                                                ) : (
                                                                    <FormControl>
                                                                        <Input
                                                                            value={form.watch(
                                                                                `specifications.${index}.value`,
                                                                            )}
                                                                            onChange={event =>
                                                                                form.setValue(
                                                                                    `specifications.${index}.value`,
                                                                                    event
                                                                                        .target
                                                                                        .value,
                                                                                    {
                                                                                        shouldDirty:
                                                                                            true,
                                                                                        shouldValidate:
                                                                                            true,
                                                                                    },
                                                                                )
                                                                            }
                                                                            placeholder={t(
                                                                                'productEdit.specifications.valuePlaceholder',
                                                                            )}
                                                                        />
                                                                    </FormControl>
                                                                )}

                                                                <FormMessage />
                                                            </FormItem>
                                                        )}
                                                    />

                                                    {/* Remove */}
                                                    <Button
                                                        type="button"
                                                        variant="ghost"
                                                        size="icon"
                                                        className="text-destructive"
                                                        onClick={() =>
                                                            removeSpecification(
                                                                index,
                                                            )
                                                        }
                                                    >
                                                        <X className="size-4" />
                                                    </Button>
                                                </div>
                                            </div>
                                        );
                                    },
                                )}
                            </div>
                        )}

                        {specificationFields.length > 0 &&
                            specificationFields.length <
                            specificationAttributes.length && (
                                <div className="mt-4">
                                    <Button
                                        type="button"
                                        variant="outline"
                                        size="sm"
                                        onClick={
                                            handleAddSpecification
                                        }
                                    >
                                        <Plus className="size-4" />

                                        {t(
                                            'productEdit.specifications.add',
                                        )}
                                    </Button>
                                </div>
                            )}
                    </CardContent>
                </Card>


                {/* ============================================================ */}
                {/* Variants                                                       */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                            <div className="space-y-1">
                                <CardTitle>
                                    {t(
                                        'productEdit.variants.title',
                                    )}
                                </CardTitle>

                                <CardDescription>
                                    {t(
                                        'productEdit.variants.description',
                                    )}
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

                                {t(
                                    'productEdit.variants.add',
                                )}
                            </Button>
                        </div>
                    </CardHeader>

                    <CardContent className="space-y-6">

                        {/* ----------------------------------------------------- */}
                        {/* Variant Dimensions                                     */}
                        {/* ----------------------------------------------------- */}

                        {variantAttributes.length >
                            0 && (
                                <div className="rounded-lg border bg-muted/20 p-4">
                                    <div className="space-y-1">
                                        <div className="text-sm font-medium">
                                            {t(
                                                'productEdit.variants.dimensionsTitle',
                                            )}
                                        </div>

                                        <p className="text-muted-foreground text-xs">
                                            {t(
                                                'productEdit.variants.dimensionsDescription',
                                            )}
                                        </p>
                                    </div>

                                    <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                                        {variantAttributes.map(
                                            attribute => {
                                                const checked =
                                                    selectedVariantAttributeIds.includes(
                                                        attribute.id,
                                                    );

                                                return (
                                                    <label
                                                        key={
                                                            attribute.id
                                                        }
                                                        className={[
                                                            'flex cursor-pointer items-center gap-3 rounded-md border p-3 transition',
                                                            checked
                                                                ? 'border-primary bg-primary/5'
                                                                : 'border-border',
                                                        ].join(
                                                            ' ',
                                                        )}
                                                    >
                                                        <input
                                                            type="checkbox"
                                                            checked={
                                                                checked
                                                            }
                                                            onChange={event =>
                                                                handleVariantAttributeSelection(
                                                                    attribute.id,
                                                                    event
                                                                        .target
                                                                        .checked,
                                                                )
                                                            }
                                                            className="size-4 accent-primary"
                                                        />

                                                        <div className="min-w-0">
                                                            <div className="text-sm font-medium">
                                                                {
                                                                    attribute.name
                                                                }
                                                            </div>

                                                            {attribute.description && (
                                                                <div className="text-muted-foreground mt-0.5 line-clamp-2 text-xs">
                                                                    {
                                                                        attribute.description
                                                                    }
                                                                </div>
                                                            )}
                                                        </div>
                                                    </label>
                                                );
                                            },
                                        )}
                                    </div>
                                </div>
                            )}

                        
                      
                        {/* ----------------------------------------------------- */}
                        {/* Variant Rows                                            */}
                        {/* ----------------------------------------------------- */}

                        <div className="space-y-4">
                            {variantFields.map((variantField, index) => {
                                const selectedIds =
                                    form.watch(
                                        `variants.${index}.attributeValueIds`,
                                    ) ?? [];

                                return (
                                    <div
                                        key={variantField.fieldId}
                                        className="relative rounded-lg border p-4"
                                    >
                                        <Button
                                            type="button"
                                            variant="ghost"
                                            size="sm"
                                            className="absolute end-2 top-2"
                                            onClick={() =>
                                                removeVariant(index)
                                            }
                                            aria-label={t(
                                                'productEdit.variants.remove',
                                            )}
                                        >
                                            <X className="size-4" />
                                        </Button>

                                        <div className="pe-10">
                                            <FormGrid columns={2}>
                                                {/* Variant SKU */}

                                                <FormField
                                                    control={form.control}
                                                    name={`variants.${index}.sku`}
                                                    render={({ field }) => (
                                                        <FormItem>
                                                            <FormLabel>
                                                                {t(
                                                                    'productEdit.variants.sku',
                                                                )}
                                                            </FormLabel>

                                                            <FormControl>
                                                                <Input
                                                                    {...field}
                                                                    placeholder={t(
                                                                        'productEdit.variants.skuPlaceholder',
                                                                    )}
                                                                />
                                                            </FormControl>

                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                                />

                                                {/* Variant Price */}

                                                <FormField
                                                    control={form.control}
                                                    name={`variants.${index}.priceOverride`}
                                                    render={({ field }) => (
                                                        <FormItem>
                                                            <FormLabel>
                                                                {t(
                                                                    'productEdit.variants.price',
                                                                )}
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
                                                                    onChange={event =>
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
                                                                    placeholder={t(
                                                                        'productEdit.optional',
                                                                    )}
                                                                />
                                                            </FormControl>

                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                                />
                                            </FormGrid>

                                            {/* Stock */}

                                            <div className="mt-4 rounded-md border bg-muted/20 p-3">
                                                <div className="flex items-center justify-between gap-3">
                                                    <div>
                                                        <div className="text-sm font-medium">
                                                            {t(
                                                                'productEdit.variants.stock',
                                                            )}
                                                        </div>

                                                        <p className="mt-0.5 text-xs text-muted-foreground">
                                                            {t(
                                                                'productEdit.variants.existingStock',
                                                            )}
                                                        </p>
                                                    </div>

                                                    <span className="text-sm font-semibold tabular-nums">
                                                        {form.watch(
                                                            `variants.${index}.stockQuantity`,
                                                        ) ?? 0}
                                                    </span>
                                                </div>
                                            </div>

                                            {/* Variant Active */}

                                            <FormField
                                                control={form.control}
                                                name={`variants.${index}.isActive`}
                                                render={({ field }) => (
                                                    <FormItem className="mt-4 flex flex-row items-center justify-between rounded-lg border p-3">
                                                        <div className="space-y-0.5">
                                                            <FormLabel>
                                                                {t(
                                                                    'productEdit.publication.active',
                                                                )}
                                                            </FormLabel>

                                                            <p className="text-xs text-muted-foreground">
                                                                {t(
                                                                    'productEdit.publication.activeDescription',
                                                                )}
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

                                            {/* Dynamic Variant Attributes */}

                                            {activeVariantAttributes.length >
                                                0 ? (
                                                <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                                                    {activeVariantAttributes.map(
                                                        attribute => {
                                                            const selectedValueId =
                                                                getSelectedValueIdForAttribute(
                                                                    attribute,
                                                                    selectedIds,
                                                                );

                                                            const selectedValue =
                                                                attribute.values.find(
                                                                    value =>
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
                                                                        onChange={event =>
                                                                            setVariantAttributeValue(
                                                                                index,
                                                                                attribute,
                                                                                event
                                                                                    .target
                                                                                    .value,
                                                                            )
                                                                        }
                                                                    >
                                                                        <option value="">
                                                                            {t(
                                                                                'productEdit.variants.select',
                                                                            )}
                                                                        </option>

                                                                        {attribute.values.map(
                                                                            value => (
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
                                                                            <div className="flex items-center gap-2 text-xs text-muted-foreground">
                                                                                <span
                                                                                    className="size-4 shrink-0 rounded-full border"
                                                                                    style={{
                                                                                        backgroundColor:
                                                                                            selectedValue?.colorHex ||
                                                                                            'transparent',
                                                                                    }}
                                                                                />

                                                                                <span>
                                                                                    {selectedValue?.displayValue ||
                                                                                        selectedValue?.value ||
                                                                                        t(
                                                                                            'productEdit.variants.selected',
                                                                                        )}
                                                                                </span>
                                                                            </div>
                                                                        )}
                                                                </div>
                                                            );
                                                        },
                                                    )}
                                                </div>
                                            ) : (
                                                <div className="mt-4 rounded-md border border-dashed p-4 text-center">
                                                    <p className="text-sm text-muted-foreground">
                                                        {t(
                                                            'productEdit.variants.dimensionsDescription',
                                                        )}
                                                    </p>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                );
                            })}

                            {variantFields.length === 0 && (
                                <p className="py-4 text-center text-sm text-muted-foreground">
                                    {t('productEdit.variants.empty')}{' '}
                                    {t('productEdit.variants.addHint')}
                                </p>
                            )}
                        </div>
                    </CardContent>
                </Card>

                {/* ============================================================ */}
                {/* Images                                                        */}
                {/* ============================================================ */}
                


                {/* ============================================================ */}
                {/* Images                                                        */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                            <div>
                                <CardTitle>
                                    {t(
                                        'productEdit.images.title',
                                    )}
                                </CardTitle>

                                <CardDescription>
                                    {t(
                                        'productEdit.images.description',
                                    )}
                                </CardDescription>
                            </div>

                            <FileUpload
                                onChange={
                                    handleAddImage
                                }
                                accept="image/*"
                                maxSize={5}
                                placeholder={t(
                                    'productEdit.images.dropzone',
                                )}
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
                                            src={resolveMediaUrl(
                                                field.imageUrl,
                                            )}
                                            alt={
                                                field.altText ||
                                                t(
                                                    'productEdit.images.alt',
                                                )
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
                                            name={`images.${index}.isMain`}
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
                                                            onChange={event => {
                                                                const checked =
                                                                    event
                                                                        .target
                                                                        .checked;

                                                                imageFields.forEach(
                                                                    (
                                                                        _,
                                                                        imageIndex,
                                                                    ) => {
                                                                        form.setValue(
                                                                            `images.${imageIndex}.isMain`,
                                                                            checked &&
                                                                            imageIndex ===
                                                                            index,
                                                                            {
                                                                                shouldDirty:
                                                                                    true,
                                                                            },
                                                                        );
                                                                    },
                                                                );
                                                            }}
                                                            className="size-3"
                                                        />
                                                    </FormControl>

                                                    <FormLabel className="text-xs">
                                                        {t(
                                                            'productEdit.images.main',
                                                        )}
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
                                        {t(
                                            'productEdit.images.empty',
                                        )}{' '}

                                        {t(
                                            'productEdit.images.uploadHint',
                                        )}
                                    </p>
                                )}
                        </div>
                    </CardContent>
                </Card>


                {/* ============================================================ */}
                {/* Publication                                                    */}
                {/* ============================================================ */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'productEdit.publication.title',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'productEdit.publication.description',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent>
                        <div className="grid gap-6 md:grid-cols-2">
                            <FormField
                                control={
                                    form.control
                                }
                                name="isActive"
                                render={({
                                    field,
                                }) => (
                                    <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                                        <div className="space-y-0.5">
                                            <FormLabel>
                                                {t(
                                                    'productEdit.publication.active',
                                                )}
                                            </FormLabel>

                                            <p className="text-muted-foreground text-sm">
                                                {t(
                                                    'productEdit.publication.activeDescription',
                                                )}
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

                            <FormField
                                control={
                                    form.control
                                }
                                name="isFeatured"
                                render={({
                                    field,
                                }) => (
                                    <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                                        <div className="space-y-0.5">
                                            <FormLabel>
                                                {t(
                                                    'productEdit.publication.featured',
                                                )}
                                            </FormLabel>

                                            <p className="text-muted-foreground text-sm">
                                                {t(
                                                    'productEdit.publication.featuredDescription',
                                                )}
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

                            <FormField
                                control={
                                    form.control
                                }
                                name="isPublished"
                                render={({
                                    field,
                                }) => (
                                    <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4 md:col-span-2">
                                        <div className="space-y-0.5">
                                            <FormLabel>
                                                {t(
                                                    'productEdit.publication.published',
                                                )}
                                            </FormLabel>

                                            <p className="text-muted-foreground text-sm">
                                                {t(
                                                    'productEdit.publication.publishedHint',
                                                )}
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
                        </div>
                    </CardContent>
                </Card>


                {/* ============================================================ */}
                {/* Actions                                                       */}
                {/* ============================================================ */}

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
                        {t(
                            'productEdit.actions.cancel',
                        )}
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
                            ? t(
                                'productEdit.actions.create',
                            )
                            : t(
                                'productEdit.actions.save',
                            )}
                    </Button>
                </div>
            </form>
        </Form>
    );
}
