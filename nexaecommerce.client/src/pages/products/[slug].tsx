import React, {
    useEffect,
    useMemo,
    useState,
} from 'react';

import {
    Alert,
    Box,
    Button,
    Chip,
    CircularProgress,
    Container,
    Divider,
    Rating,
    Stack,
    Typography,
} from '@mui/material';

import {
    ShoppingCartOutlined,
} from '@mui/icons-material';

import {
    Link,
    useParams,
} from 'react-router-dom';

import {
    useProductBySlug,
} from '@/modules/catalog/hooks/useProducts';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';

import type {
    ProductVariant,
} from '@/modules/catalog/api/products';

function money(
    amount: number,
    currency: string,
) {
    return (
        new Intl.NumberFormat(
            'fa-IR',
            {
                maximumFractionDigits: 0,
            },
        ).format(amount) +
        ` ${currency}`
    );
}

type AttributeOption = {
    id: string;
    value: string;
    displayValue?: string | null;
    colorHex?: string | null;
};

type AttributeGroup = {
    code: string;
    name: string;
    options: AttributeOption[];
};

function normalize(
    value: string | null | undefined,
) {
    return (
        value ?? ''
    )
        .trim()
        .toLowerCase();
}

function getVariantAttributeGroups(
    variants: ProductVariant[],
): AttributeGroup[] {
    const map =
        new Map<
            string,
            AttributeGroup
        >();

    for (
        const variant of variants
    ) {
        for (
            const attribute
            of variant.attributes ?? []
        ) {
            const code =
                normalize(
                    attribute.attributeCode,
                );

            if (!code) {
                continue;
            }

            const name =
                attribute.attributeName?.trim() ||
                attribute.attributeCode;

            let group =
                map.get(code);

            if (!group) {
                group = {
                    code,
                    name,
                    options: [],
                };

                map.set(
                    code,
                    group,
                );
            }

            const optionId =
                attribute.attributeValueId;

            if (!optionId) {
                continue;
            }

            const alreadyExists =
                group.options.some(
                    option =>
                        option.id ===
                        optionId,
                );

            if (!alreadyExists) {
                group.options.push({
                    id:
                        optionId,

                    value:
                        attribute.value,

                    displayValue:
                        attribute.displayValue,

                    colorHex:
                        attribute.colorHex,
                });
            }
        }
    }

    return Array.from(
        map.values(),
    );
}

function getVariantAttributeMap(
    variant: ProductVariant,
): Record<string, string> {
    const result:
        Record<string, string> =
        {};

    for (
        const attribute
        of variant.attributes ?? []
    ) {
        const code =
            normalize(
                attribute.attributeCode,
            );

        if (!code) {
            continue;
        }

        result[code] =
            attribute.attributeValueId;
    }

    return result;
}

function matchesSelections(
    variant: ProductVariant,
    selections: Record<
        string,
        string
    >,
) {
    const variantAttributes =
        getVariantAttributeMap(
            variant,
        );

    return Object.entries(
        selections,
    ).every(
        ([
            code,
            valueId,
        ]) =>
            !valueId ||
            variantAttributes[
            code
            ] === valueId,
    );
}

function getVariantDisplayAttributes(
    variant: ProductVariant,
): string[] {
    const genericAttributes =
        (
            variant.attributes ??
            []
        )
            .map(
                attribute =>
                    attribute.displayValue ||
                    attribute.value,
            )
            .filter(Boolean);

    if (
        genericAttributes.length >
        0
    ) {
        return genericAttributes;
    }

    return [
        variant.color,
        variant.size,
    ].filter(
        (
            value,
        ): value is string =>
            Boolean(
                value,
            ),
    );
}

function getVariantLabel(
    variant: ProductVariant,
) {
    const attributes =
        getVariantDisplayAttributes(
            variant,
        );

    if (
        attributes.length >
        0
    ) {
        return attributes.join(
            ' • ',
        );
    }

    return variant.sku;
}

function getErrorMessage(
    error: unknown,
) {
    if (
        error instanceof Error
    ) {
        return error.message;
    }

    return 'Unable to add the selected variant to the cart.';
}

export default function ProductDetailPage() {
    const {
        slug,
    } = useParams<{
        slug: string;
    }>();

    const {
        data: product,
        isLoading,
        error,
    } =
        useProductBySlug(
            slug,
        );

    const {
        add,
    } =
        useCartMutations();

    const [
        selectedImage,
        setSelectedImage,
    ] =
        useState(0);

    const [
        selectedVariantId,
        setSelectedVariantId,
    ] =
        useState<
            string | null
        >(null);

    const [
        selectedAttributes,
        setSelectedAttributes,
    ] =
        useState<
            Record<
                string,
                string
            >
        >({});

    const [
        quantity,
        setQuantity,
    ] =
        useState(1);

    const activeVariants =
        useMemo(
            () =>
                (
                    product?.variants ??
                    []
                ).filter(
                    variant =>
                        variant.isActive,
                ),
            [
                product,
            ],
        );

    const attributeGroups =
        useMemo(
            () =>
                getVariantAttributeGroups(
                    activeVariants,
                ),
            [
                activeVariants,
            ],
        );

    const hasGenericAttributes =
        attributeGroups.length >
        0;

    /*
     * For a simple product without generic
     * attributes, choose the first available
     * active variant automatically.
     *
     * For configurable products, the customer
     * must select every attribute combination.
     */
    useEffect(
        () => {
            if (
                activeVariants.length ===
                0
            ) {
                setSelectedVariantId(
                    null,
                );

                return;
            }

            if (
                hasGenericAttributes
            ) {
                const currentSelection =
                    selectedAttributes;

                const complete =
                    attributeGroups.every(
                        group =>
                            Boolean(
                                currentSelection[
                                group.code
                                ],
                            ),
                    );

                if (
                    complete
                ) {
                    const matched =
                        activeVariants.find(
                            variant =>
                                matchesSelections(
                                    variant,
                                    currentSelection,
                                ),
                        );

                    setSelectedVariantId(
                        matched?.id ??
                        null,
                    );
                } else {
                    setSelectedVariantId(
                        null,
                    );
                }

                return;
            }

            const existing =
                activeVariants.find(
                    variant =>
                        variant.id ===
                        selectedVariantId,
                );

            if (
                !existing
            ) {
                const firstAvailable =
                    activeVariants.find(
                        variant =>
                            variant.stockQuantity >
                            0,
                    ) ??
                    activeVariants[0];

                setSelectedVariantId(
                    firstAvailable?.id ??
                    null,
                );
            }
        },
        [
            activeVariants,
            attributeGroups,
            hasGenericAttributes,
            selectedAttributes,
            selectedVariantId,
        ],
    );

    /*
     * Reset selection whenever product
     * changes.
     */
    useEffect(
        () => {
            setSelectedAttributes(
                {},
            );

            setSelectedVariantId(
                null,
            );

            setQuantity(
                1,
            );

            setSelectedImage(
                0,
            );
        },
        [
            product?.id,
        ],
    );

    const selectedVariant =
        useMemo(
            () =>
                activeVariants.find(
                    variant =>
                        variant.id ===
                        selectedVariantId,
                ) ??
                null,
            [
                activeVariants,
                selectedVariantId,
            ],
        );

    const maxStock =
        selectedVariant
            ?.stockQuantity ??
        (
            hasGenericAttributes
                ? 0
                : product?.stockQuantity ??
                0
        );

    const price =
        selectedVariant?.priceOverride ??
        product?.finalPrice ??
        product?.price ??
        0;

    const comparePrice =
        selectedVariant?.comparePrice ??
        product?.comparePrice ??
        null;

    const hasDiscount =
        Boolean(
            comparePrice &&
            comparePrice >
            price,
        );

    /*
     * Determine whether a value can participate
     * in at least one currently possible variant.
     */
    const isOptionAvailable = (
        groupCode: string,
        optionId: string,
    ) => {
        const hypotheticalSelections =
        {
            ...selectedAttributes,
            [groupCode]:
                optionId,
        };

        return activeVariants.some(
            variant =>
                variant.stockQuantity >
                0 &&
                matchesSelections(
                    variant,
                    hypotheticalSelections,
                ),
        );
    };

    const handleAttributeChange = (
        groupCode: string,
        optionId: string,
    ) => {
        const nextSelections =
        {
            ...selectedAttributes,
            [groupCode]:
                optionId,
        };

        let cleanedSelections =
        {
            ...nextSelections,
        };

        for (
            const group
            of attributeGroups
        ) {
            const selected =
                cleanedSelections[
                group.code
                ];

            if (!selected) {
                continue;
            }

            const valid =
                activeVariants.some(
                    variant =>
                        variant.stockQuantity >
                        0 &&
                        matchesSelections(
                            variant,
                            cleanedSelections,
                        ),
                );

            if (!valid) {
                delete cleanedSelections[
                    group.code
                ];
            }
        }

        setSelectedAttributes(
            cleanedSelections,
        );

        setQuantity(
            1,
        );
    };

    const canAddToCart =
        Boolean(
            product &&
            product.isActive &&
            product.isInStock &&
            selectedVariant &&
            selectedVariant.isActive &&
            selectedVariant.stockQuantity >
            0 &&
            quantity > 0 &&
            quantity <=
            selectedVariant.stockQuantity,
        );

    const handleAddToCart = () => {
        if (!selectedVariant) {
            return;
        }

        if (!selectedVariant.isActive) {
            return;
        }

        if (
            selectedVariant.stockQuantity <=
            0
        ) {
            return;
        }

        if (
            quantity >
            selectedVariant.stockQuantity
        ) {
            setQuantity(
                selectedVariant.stockQuantity,
            );

            return;
        }

        add.mutate({
            productVariantId:
                selectedVariant.id,
            quantity,
        });
    };

    /*
     * Limit the quantity picker to
     * currently selected variant stock.
     */
    useEffect(
        () => {
            if (
                maxStock <=
                0
            ) {
                setQuantity(
                    1,
                );

                return;
            }

            setQuantity(
                current =>
                    Math.min(
                        Math.max(
                            current,
                            1,
                        ),
                        maxStock,
                    ),
            );
        },
        [
            maxStock,
        ],
    );

    if (
        isLoading
    ) {
        return (
            <Box
                sx={{
                    minHeight:
                        '65vh',

                    display:
                        'flex',

                    alignItems:
                        'center',

                    justifyContent:
                        'center',
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    if (
        error
    ) {
        return (
            <Container
                maxWidth="lg"
                sx={{
                    py: 6,
                }}
            >
                <Alert
                    severity="error"
                >
                    {getErrorMessage(
                        error,
                    )}
                </Alert>
            </Container>
        );
    }

    if (
        !product
    ) {
        return (
            <Container
                maxWidth="lg"
                sx={{
                    py: 6,
                }}
            >
                <Alert
                    severity="warning"
                >
                    محصول پیدا نشد.
                </Alert>
            </Container>
        );
    }

    const activeImages =
        product.images.length >
            0
            ? product.images
            : [
                {
                    id:
                        'fallback',

                    imageUrl:
                        '/placeholder.jpg',

                    altText:
                        product.name,

                    displayOrder:
                        0,

                    isMain:
                        true,
                },
            ];

    return (
        <Box
            sx={{
                backgroundColor:
                    '#fafafa',

                minHeight:
                    '100vh',

                py: {
                    xs: 3,
                    md: 6,
                },

                direction:
                    'rtl',
            }}
        >
            <Container
                maxWidth="xl"
            >
                <Box
                    sx={{
                        display:
                            'grid',

                        gridTemplateColumns:
                        {
                            xs:
                                '1fr',

                            md:
                                '1.1fr 1fr',
                        },

                        gap:
                        {
                            xs:
                                3,

                            md:
                                6,
                        },
                    }}
                >
                    {/* ---------------------------------------------------------- */}
                    {/* Images                                                       */}
                    {/* ---------------------------------------------------------- */}

                    <Box>
                        <Box
                            sx={{
                                backgroundColor:
                                    '#fff',

                                border:
                                    '1px solid',

                                borderColor:
                                    'divider',

                                borderRadius:
                                    4,

                                overflow:
                                    'hidden',
                            }}
                        >
                            <Box
                                component="img"
                                src={
                                    activeImages[
                                        selectedImage
                                    ]
                                        ?.imageUrl ??
                                    '/placeholder.jpg'
                                }
                                alt={
                                    activeImages[
                                        selectedImage
                                    ]
                                        ?.altText ??
                                    product.name
                                }
                                sx={{
                                    width:
                                        '100%',

                                    height:
                                    {
                                        xs:
                                            350,

                                        md:
                                            560,
                                    },

                                    objectFit:
                                        'contain',

                                    backgroundColor:
                                        '#f7f7f7',
                                }}
                                onError={(
                                    event:
                                        React.SyntheticEvent<HTMLImageElement>,
                                ) => {
                                    event.currentTarget.src =
                                        '/placeholder.jpg';
                                }}
                            />
                        </Box>

                        <Stack
                            direction="row"
                            spacing={1}
                            sx={{
                                mt:
                                    2,

                                overflowX:
                                    'auto',

                                pb:
                                    1,
                            }}
                        >
                            {activeImages.map(
                                (
                                    image,
                                    index,
                                ) => (
                                    <Box
                                        key={
                                            image.id
                                        }
                                        component="button"
                                        type="button"
                                        onClick={() =>
                                            setSelectedImage(
                                                index,
                                            )
                                        }
                                        sx={{
                                            width:
                                                82,

                                            height:
                                                82,

                                            flexShrink:
                                                0,

                                            border:
                                                '2px solid',

                                            borderColor:
                                                selectedImage ===
                                                    index
                                                    ? 'primary.main'
                                                    : 'divider',

                                            borderRadius:
                                                2,

                                            overflow:
                                                'hidden',

                                            p:
                                                0,

                                            background:
                                                '#fff',

                                            cursor:
                                                'pointer',
                                        }}
                                    >
                                        <Box
                                            component="img"
                                            src={
                                                image.imageUrl
                                            }
                                            alt={
                                                image.altText ??
                                                product.name
                                            }
                                            sx={{
                                                width:
                                                    '100%',

                                                height:
                                                    '100%',

                                                objectFit:
                                                    'cover',
                                            }}
                                        />
                                    </Box>
                                ),
                            )}
                        </Stack>
                    </Box>

                    {/* ---------------------------------------------------------- */}
                    {/* Product information                                         */}
                    {/* ---------------------------------------------------------- */}

                    <Box>
                        <Stack
                            spacing={2.5}
                        >
                            {product.brandName && (
                                <Typography
                                    variant="body2"
                                    color="text.secondary"
                                >
                                    {
                                        product.brandName
                                    }
                                </Typography>
                            )}

                            <Typography
                                variant="h3"
                                component="h1"
                                sx={{
                                    fontWeight:
                                        900,

                                    fontSize:
                                    {
                                        xs:
                                            '2rem',

                                        md:
                                            '3rem',
                                    },
                                }}
                            >
                                {
                                    product.name
                                }
                            </Typography>

                            <Stack
                                direction="row"
                                spacing={1.5}
                                sx={{
                                    alignItems:
                                        'center',

                                    direction:
                                        'ltr',
                                }}
                            >
                                <Rating
                                    value={
                                        product.averageRating
                                    }
                                    precision={
                                        0.5
                                    }
                                    readOnly
                                />

                                <Typography
                                    variant="body2"
                                    color="text.secondary"
                                >
                                    {
                                        product.averageRating
                                    }{' '}
                                    (
                                    {
                                        product.reviewCount
                                    }{' '}
                                    reviews)
                                </Typography>
                            </Stack>

                            <Stack
                                direction="row"
                                spacing={1}
                                sx={{
                                    flexWrap:
                                        'wrap',
                                }}
                            >
                                {product.categories.map(
                                    category => (
                                        <Chip
                                            key={
                                                category
                                            }
                                            label={
                                                category
                                            }
                                            size="small"
                                            variant="outlined"
                                        />
                                    ),
                                )}
                            </Stack>

                            <Divider />

                            {product.shortDescription && (
                                <Typography
                                    color="text.secondary"
                                    sx={{
                                        lineHeight:
                                            2,
                                    }}
                                >
                                    {
                                        product.shortDescription
                                    }
                                </Typography>
                            )}

                            {/* -------------------------------------------------- */}
                            {/* Price                                               */}
                            {/* -------------------------------------------------- */}

                            <Box>
                                {hasDiscount && (
                                    <Typography
                                        variant="body1"
                                        color="text.secondary"
                                        sx={{
                                            textDecoration:
                                                'line-through',
                                        }}
                                    >
                                        {money(
                                            comparePrice!,
                                            product.currency,
                                        )}
                                    </Typography>
                                )}

                                <Typography
                                    variant="h4"
                                    color="primary"
                                    sx={{
                                        fontWeight:
                                            900,
                                    }}
                                >
                                    {money(
                                        price,
                                        product.currency,
                                    )}
                                </Typography>

                                {selectedVariant && (
                                    <Typography
                                        variant="caption"
                                        color="text.secondary"
                                        sx={{
                                            display:
                                                'block',

                                            mt:
                                                0.5,
                                        }}
                                    >
                                        SKU:{' '}
                                        {
                                            selectedVariant.sku
                                        }
                                    </Typography>
                                )}
                            </Box>

                            {/* -------------------------------------------------- */}
                            {/* Generic Attribute Selection                       */}
                            {/* -------------------------------------------------- */}

                            {hasGenericAttributes && (
                                <Box>
                                    <Typography
                                        variant="subtitle1"
                                        sx={{
                                            fontWeight:
                                                800,

                                            mb:
                                                2,
                                        }}
                                    >
                                        انتخاب مشخصات
                                    </Typography>

                                    <Stack
                                        spacing={2.5}
                                    >
                                        {attributeGroups.map(
                                            group => (
                                                <Box
                                                    key={
                                                        group.code
                                                    }
                                                >
                                                    <Typography
                                                        variant="body2"
                                                        sx={{
                                                            fontWeight:
                                                                800,

                                                            mb:
                                                                1,
                                                        }}
                                                    >
                                                        {
                                                            group.name
                                                        }
                                                    </Typography>

                                                    <Stack
                                                        direction="row"
                                                        spacing={1}
                                                        useFlexGap
                                                        sx={{
                                                            flexWrap:
                                                                'wrap',
                                                        }}
                                                    >
                                                        {group.options.map(
                                                            option => {
                                                                const selected =
                                                                    selectedAttributes[
                                                                    group.code
                                                                    ] ===
                                                                    option.id;

                                                                const available =
                                                                    isOptionAvailable(
                                                                        group.code,
                                                                        option.id,
                                                                    );

                                                                return (
                                                                    <Button
                                                                        key={
                                                                            option.id
                                                                        }
                                                                        type="button"
                                                                        variant={
                                                                            selected
                                                                                ? 'contained'
                                                                                : 'outlined'
                                                                        }
                                                                        disabled={
                                                                            !available ||
                                                                            add.isPending
                                                                        }
                                                                        onClick={() =>
                                                                            handleAttributeChange(
                                                                                group.code,
                                                                                option.id,
                                                                            )
                                                                        }
                                                                        sx={{
                                                                            borderRadius:
                                                                                2,

                                                                            minWidth:
                                                                                88,
                                                                        }}
                                                                    >
                                                                        {option.colorHex && (
                                                                            <Box
                                                                                component="span"
                                                                                sx={{
                                                                                    width:
                                                                                        14,

                                                                                    height:
                                                                                        14,

                                                                                    borderRadius:
                                                                                        '50%',

                                                                                    backgroundColor:
                                                                                        option.colorHex,

                                                                                    border:
                                                                                        '1px solid',

                                                                                    borderColor:
                                                                                        'divider',

                                                                                    mr:
                                                                                        1,
                                                                                }}
                                                                            />
                                                                        )}

                                                                        {
                                                                            option.displayValue ||
                                                                            option.value
                                                                        }
                                                                    </Button>
                                                                );
                                                            },
                                                        )}
                                                    </Stack>
                                                </Box>
                                            ),
                                        )}
                                    </Stack>

                                    {Object.keys(
                                        selectedAttributes,
                                    ).length >
                                        0 && (
                                            <Box
                                                sx={{
                                                    mt:
                                                        2,
                                                }}
                                            >
                                                {selectedVariant ? (
                                                    <Alert
                                                        severity={
                                                            selectedVariant.stockQuantity >
                                                                0
                                                                ? 'success'
                                                                : 'warning'
                                                        }
                                                    >
                                                        {selectedVariant.stockQuantity >
                                                            0
                                                            ? `گزینه انتخاب‌شده موجود است — ${selectedVariant.stockQuantity} عدد`
                                                            : 'این ترکیب در حال حاضر موجود نیست.'}
                                                    </Alert>
                                                ) : (
                                                    <Alert
                                                        severity="info"
                                                    >
                                                        لطفاً مشخصات کامل و یک ترکیب موجود را انتخاب کنید.
                                                    </Alert>
                                                )}
                                            </Box>
                                        )}
                                </Box>
                            )}

                            {/* -------------------------------------------------- */}
                            {/* Simple variant fallback                           */}
                            {/* -------------------------------------------------- */}

                            {!hasGenericAttributes &&
                                activeVariants.length >
                                1 && (
                                    <Box>
                                        <Typography
                                            variant="subtitle1"
                                            sx={{
                                                fontWeight:
                                                    800,

                                                mb:
                                                    1.5,
                                            }}
                                        >
                                            انتخاب مدل
                                        </Typography>

                                        <Stack
                                            direction="row"
                                            spacing={1}
                                            useFlexGap
                                            sx={{
                                                flexWrap:
                                                    'wrap',
                                            }}
                                        >
                                            {activeVariants.map(
                                                variant => {
                                                    const selected =
                                                        selectedVariantId ===
                                                        variant.id;

                                                    const disabled =
                                                        variant.stockQuantity <=
                                                        0;

                                                    return (
                                                        <Button
                                                            key={
                                                                variant.id
                                                            }
                                                            type="button"
                                                            variant={
                                                                selected
                                                                    ? 'contained'
                                                                    : 'outlined'
                                                            }
                                                            disabled={
                                                                disabled ||
                                                                add.isPending
                                                            }
                                                            onClick={() => {
                                                                setSelectedVariantId(
                                                                    variant.id,
                                                                );

                                                                setQuantity(
                                                                    1,
                                                                );
                                                            }}
                                                            sx={{
                                                                borderRadius:
                                                                    2,
                                                            }}
                                                        >
                                                            {
                                                                getVariantLabel(
                                                                    variant,
                                                                )
                                                            }
                                                        </Button>
                                                    );
                                                },
                                            )}
                                        </Stack>
                                    </Box>
                                )}

                            {/* -------------------------------------------------- */}
                            {/* Quantity                                           */}
                            {/* -------------------------------------------------- */}

                            <Box
                                sx={{
                                    width: '100%',
                                    mt: 1,
                                }}
                            >
                                <Typography
                                    variant="body2"
                                    sx={{
                                        fontWeight: 800,
                                        mb: 1,
                                    }}
                                >
                                    تعداد
                                </Typography>

                                <Box
                                    sx={{
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'space-between',
                                        gap: 1,
                                        width: '100%',
                                        flexWrap: {
                                            xs: 'wrap',
                                            sm: 'nowrap',
                                        },
                                    }}
                                >
                                    <Box
                                        sx={{
                                            display: 'flex',
                                            alignItems: 'center',
                                            border: '1px solid',
                                            borderColor: 'divider',
                                            borderRadius: 2.5,
                                            overflow: 'hidden',
                                            flexShrink: 0,
                                        }}
                                    >
                                        <Button
                                            type="button"
                                            variant="text"
                                            disabled={
                                                quantity <= 1 ||
                                                add.isPending
                                            }
                                            onClick={() =>
                                                setQuantity(
                                                    value =>
                                                        Math.max(
                                                            1,
                                                            value - 1,
                                                        ),
                                                )
                                            }
                                            sx={{
                                                minWidth: 44,
                                                width: 44,
                                                height: 44,
                                                borderRadius: 0,
                                                fontSize: '1.25rem',
                                                p: 0,
                                            }}
                                        >
                                            −
                                        </Button>

                                        <Typography
                                            sx={{
                                                minWidth: 44,
                                                textAlign: 'center',
                                                fontWeight: 800,
                                                userSelect: 'none',
                                            }}
                                        >
                                            {quantity}
                                        </Typography>

                                        <Button
                                            type="button"
                                            variant="text"
                                            disabled={
                                                !selectedVariant ||
                                                quantity >= maxStock ||
                                                add.isPending
                                            }
                                            onClick={() =>
                                                setQuantity(
                                                    value =>
                                                        Math.min(
                                                            maxStock,
                                                            value + 1,
                                                        ),
                                                )
                                            }
                                            sx={{
                                                minWidth: 44,
                                                width: 44,
                                                height: 44,
                                                borderRadius: 0,
                                                fontSize: '1.25rem',
                                                p: 0,
                                            }}
                                        >
                                            +
                                        </Button>
                                    </Box>

                                    <Typography
                                        variant="body2"
                                        color="text.secondary"
                                        sx={{
                                            textAlign: 'right',
                                            flex: 1,
                                            minWidth: 0,
                                        }}
                                    >
                                        موجودی: {maxStock}
                                    </Typography>
                                </Box>
                            </Box>

                            {/* -------------------------------------------------- */}
                            {/* Add to cart                                        */}
                            {/* -------------------------------------------------- */}

                            <Button
                                fullWidth
                                size="large"
                                variant="contained"
                                startIcon={
                                    add.isPending ? (
                                        <CircularProgress
                                            size={18}
                                            color="inherit"
                                        />
                                    ) : (
                                        <ShoppingCartOutlined />
                                    )
                                }
                                disabled={
                                    !canAddToCart ||
                                    add.isPending
                                }
                                onClick={
                                    handleAddToCart
                                }
                                sx={{
                                    width: '100%',
                                    borderRadius: 3,
                                    minHeight: {
                                        xs: 54,
                                        sm: 58,
                                    },
                                    px: {
                                        xs: 2,
                                        sm: 3,
                                    },
                                    py: 1.5,
                                    fontWeight: 800,
                                    fontSize: {
                                        xs: '0.95rem',
                                        sm: '1rem',
                                    },
                                    whiteSpace: 'nowrap',
                                }}
                            >
                                {add.isPending
                                    ? 'در حال افزودن...'
                                    : 'افزودن به سبد خرید'}
                            </Button>

                            {/* Cart must always be accessible */}

                            <Button
                                component={Link}
                                to="/cart"
                                fullWidth
                                size="large"
                                variant="outlined"
                                startIcon={
                                    <ShoppingCartOutlined />
                                }
                                sx={{
                                    minHeight: {
                                        xs: 52,
                                        sm: 56,
                                    },
                                    borderRadius: 3,
                                    fontWeight: 800,
                                    fontSize: {
                                        xs: '0.95rem',
                                        sm: '1rem',
                                    },
                                    whiteSpace: 'nowrap',
                                }}
                            >
                                مشاهده سبد خرید
                            </Button>

                            {add.isError && (
                                <Alert
                                    severity="error"
                                >
                                    {getErrorMessage(
                                        add.error,
                                    )}
                                </Alert>
                            )}

                            {/* -------------------------------------------------- */}
                            {/* Variant details                                    */}
                            {/* -------------------------------------------------- */}

                            {selectedVariant && (
                                <Box
                                    sx={{
                                        p:
                                            2,

                                        border:
                                            '1px solid',

                                        borderColor:
                                            'divider',

                                        borderRadius:
                                            3,

                                        backgroundColor:
                                            'background.paper',
                                    }}
                                >
                                    <Typography
                                        variant="subtitle2"
                                        sx={{
                                            fontWeight:
                                                800,

                                            mb:
                                                1,
                                        }}
                                    >
                                        مشخصات انتخاب‌شده
                                    </Typography>

                                    <Stack
                                        spacing={
                                            0.75
                                        }
                                    >
                                        {getVariantDisplayAttributes(
                                            selectedVariant,
                                        ).map(
                                            (
                                                label,
                                                index,
                                            ) => (
                                                <Typography
                                                    key={`${selectedVariant.id}-${index}`}
                                                    variant="body2"
                                                    color="text.secondary"
                                                >
                                                    {
                                                        label
                                                    }
                                                </Typography>
                                            ),
                                        )}

                                        <Typography
                                            variant="body2"
                                            color="text.secondary"
                                        >
                                            SKU:{' '}
                                            {
                                                selectedVariant.sku
                                            }
                                        </Typography>
                                    </Stack>
                                </Box>
                            )}
                        </Stack>
                    </Box>
                </Box>

                {/* -------------------------------------------------------------- */}
                {/* Description                                                     */}
                {/* -------------------------------------------------------------- */}

                {product.description && (
                    <Box
                        sx={{
                            mt:
                                6,

                            p:
                                3,

                            backgroundColor:
                                '#fff',

                            border:
                                '1px solid',

                            borderColor:
                                'divider',

                            borderRadius:
                                4,
                        }}
                    >
                        <Typography
                            variant="h5"
                            sx={{
                                fontWeight:
                                    900,

                                mb:
                                    2,
                            }}
                        >
                            توضیحات محصول
                        </Typography>

                        <Typography
                            sx={{
                                lineHeight:
                                    2.2,

                                whiteSpace:
                                    'pre-wrap',
                            }}
                        >
                            {
                                product.description
                            }
                        </Typography>
                    </Box>
                )}
            </Container>
        </Box>
    );
}