import {
    useMemo,
    useState,
} from 'react';

import {
    Alert,
    Box,
    Button,
    Card,
    Chip,
    Container,
    Divider,
    IconButton,
    Rating,
    Skeleton,
    Stack,
    Typography,
} from '@mui/material';

import {
    Add,
    ArrowBack,
    CheckCircleOutlined,
    Remove,
    ShoppingCartOutlined,
} from '@mui/icons-material';

import {
    Link,
    useParams,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    useProductBySlug,
} from '../hooks/useProducts';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';

import type {
     ProductVariant,
} from '../api/products';

type SelectedAttributes = Record<
    string,
    string
>;

function getAttributeValue(
    variant: ProductVariant,
    code: string,
) {
    return (
        variant.attributes?.find(
            attribute =>
                attribute.attributeCode
                    .trim()
                    .toLowerCase() ===
                code
                    .trim()
                    .toLowerCase(),
        )?.attributeValueId ?? null
    );
}

function getVariantLabel(
    variant: ProductVariant,
) {
    const genericAttributes =
        variant.attributes ?? [];

    if (
        genericAttributes.length > 0
    ) {
        return genericAttributes
            .map(
                attribute =>
                    attribute.displayValue ||
                    attribute.value,
            )
            .filter(Boolean)
            .join(' / ');
    }

    const legacyParts = [
        variant.color?.trim(),
        variant.size?.trim(),
    ].filter(Boolean);

    if (
        legacyParts.length > 0
    ) {
        return legacyParts.join(' / ');
    }

    return variant.sku;
}



export default function ProductDetailPage() {
    const {
        id: slug,
    } = useParams<{
        id: string;
    }>();

    const {
        t,
        i18n,
    } = useTranslation();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa');

    const getText = (
        key: string,
        fallback: string,
    ) =>
        t(
            key,
            {
                defaultValue:
                    fallback,
            },
        );

    const {
        data: product,
        isLoading,
        error,
    } =
        useProductBySlug(slug);

    const {
        add,
    } = useCartMutations();

    const [
        imageIndex,
        setImageIndex,
    ] = useState(0);

    const [
        selectedAttributes,
        setSelectedAttributes,
    ] =
        useState<SelectedAttributes>(
            {},
        );

    const [
        selectedLegacyVariantId,
        setSelectedLegacyVariantId,
    ] =
        useState<string | null>(
            null,
        );

    const [
        quantity,
        setQuantity,
    ] = useState(1);

    const [
        added,
        setAdded,
    ] = useState(false);

    const availableVariants =
        useMemo(
            () =>
                (
                    product?.variants ??
                    []
                ).filter(
                    variant =>
                        variant.isActive &&
                        variant.stockQuantity >
                            0,
                ),
            [product],
        );

    const genericVariantMode =
        availableVariants.some(
            variant =>
                (
                    variant.attributes
                        ?.length ?? 0
                ) > 0,
        );

    const attributeGroups =
        useMemo(() => {
            if (
                !genericVariantMode
            ) {
                return [];
            }

            const groups = new Map<
                string,
                {
                    code: string;
                    name: string;
                    values: {
                        id: string;
                        label: string;
                        colorHex?:
                            | string
                            | null;
                    }[];
                }
            >();

            for (
                const variant of
                availableVariants
            ) {
                for (
                    const attribute of
                    variant.attributes ??
                    []
                ) {
                    const key =
                        attribute.attributeCode
                            .trim()
                            .toLowerCase();

                    if (
                        !groups.has(key)
                    ) {
                        groups.set(
                            key,
                            {
                                code:
                                    attribute.attributeCode,
                                name:
                                    attribute.attributeName,
                                values: [],
                            },
                        );
                    }

                    const group =
                        groups.get(
                            key,
                        );

                    if (
                        !group
                    ) {
                        continue;
                    }

                    const exists =
                        group.values.some(
                            value =>
                                value.id ===
                                attribute.attributeValueId,
                        );

                    if (!exists) {
                        group.values.push(
                            {
                                id:
                                    attribute.attributeValueId,
                                label:
                                    attribute.displayValue ||
                                    attribute.value,
                                colorHex:
                                    attribute.colorHex,
                            },
                        );
                    }
                }
            }

            return Array.from(
                groups.values(),
            );
        }, [
            availableVariants,
            genericVariantMode,
        ]);

    const selectedEntries =
        Object.entries(
            selectedAttributes,
        );

    const findMatchingVariant =
        (
            selection: SelectedAttributes,
        ) => {
            const entries =
                Object.entries(
                    selection,
                );

            if (
                entries.length === 0
            ) {
                return null;
            }

            return (
                availableVariants.find(
                    variant =>
                        entries.every(
                            ([code, valueId]) =>
                                getAttributeValue(
                                    variant,
                                    code,
                                ) === valueId,
                        ),
                ) ?? null
            );
        };

    const matchingVariant =
        useMemo(
            () =>
                genericVariantMode
                    ? findMatchingVariant(
                          selectedAttributes,
                      )
                    : null,
            [
                availableVariants,
                genericVariantMode,
                selectedAttributes,
            ],
        );

    const activeLegacyVariant =
        useMemo(
            () => {
                if (
                    !selectedLegacyVariantId
                ) {
                    return (
                        availableVariants[0] ??
                        product?.variants?.find(
                            variant =>
                                variant.isActive,
                        ) ??
                        null
                    );
                }

                return (
                    availableVariants.find(
                        variant =>
                            variant.id ===
                            selectedLegacyVariantId,
                    ) ?? null
                );
            },
            [
                availableVariants,
                product,
                selectedLegacyVariantId,
            ],
        );

    const activeVariant =
        genericVariantMode
            ? matchingVariant ??
              availableVariants[0] ??
              null
            : activeLegacyVariant;

    const hasCompleteGenericSelection =
        genericVariantMode &&
        attributeGroups.length > 0 &&
        attributeGroups.every(
            group =>
                Boolean(
                    selectedAttributes[
                        group.code
                            .trim()
                            .toLowerCase()
                    ],
                ),
        );

    const canAddToCart =
        Boolean(
            activeVariant?.id,
        ) &&
        Boolean(
            activeVariant &&
                activeVariant.stockQuantity >
                    0,
        ) &&
        (
            !genericVariantMode ||
            hasCompleteGenericSelection
        );

    const images =
        product?.images ?? [];

    const safeImageIndex =
        Math.min(
            imageIndex,
            Math.max(
                images.length - 1,
                0,
            ),
        );

    const imageUrl =
        images[
            safeImageIndex
        ]?.imageUrl ??
        images[0]?.imageUrl ??
        '/placeholder.jpg';

    const maxQuantity =
        activeVariant?.stockQuantity ??
        product?.stockQuantity ??
        0;

    const price =
        activeVariant?.priceOverride ??
        product?.finalPrice ??
        product?.price ??
        0;

    const comparePrice =
        activeVariant?.comparePrice ??
        product?.comparePrice ??
        null;

    const currency =
        product?.currency ?? '';

    const formatPrice = (
        value: number,
    ) =>
        new Intl.NumberFormat(
            isFa
                ? 'fa-IR'
                : 'en-US',
            {
                maximumFractionDigits: 0,
            },
        ).format(value);

    const localizedRating =
        product?.averageRating ?? 0;

    const handleAttributeSelect =
        (
            code: string,
            valueId: string,
        ) => {
            setSelectedAttributes(
                current => ({
                    ...current,
                    [code
                        .trim()
                        .toLowerCase()]:
                        valueId,
                }),
            );

            setQuantity(1);
            setAdded(false);
        };

    const isAttributeValueAvailable =
        (
            code: string,
            valueId: string,
        ) =>
            availableVariants.some(
                variant => {
                    if (
                        getAttributeValue(
                            variant,
                            code,
                        ) !== valueId
                    ) {
                        return false;
                    }

                    return selectedEntries
                        .filter(
                            ([selectedCode]) =>
                                selectedCode
                                    .trim()
                                    .toLowerCase() !==
                                code
                                    .trim()
                                    .toLowerCase(),
                        )
                        .every(
                            ([
                                selectedCode,
                                selectedValueId,
                            ]) =>
                                getAttributeValue(
                                    variant,
                                    selectedCode,
                                ) ===
                                selectedValueId,
                        );
                },
            );

    const decrease =
        () => {
            setQuantity(
                value =>
                    Math.max(
                        1,
                        value - 1,
                    ),
            );

            setAdded(false);
        };

    const increase =
        () => {
            setQuantity(
                value =>
                    Math.min(
                        Math.max(
                            maxQuantity,
                            1,
                        ),
                        value + 1,
                    ),
            );

            setAdded(false);
        };

    const addToCart =
        () => {
            if (
                !activeVariant?.id ||
                maxQuantity <= 0 ||
                !canAddToCart
            ) {
                return;
            }

            add.mutate(
                {
                    productVariantId:
                        activeVariant.id,
                    quantity,
                },
                {
                    onSuccess:
                        () => {
                            setAdded(
                                true,
                            );
                        },
                },
            );
        };

    if (isLoading) {
        return (
            <Container
                maxWidth="xl"
                sx={{
                    py: 5,
                    direction:
                        isFa
                            ? 'rtl'
                            : 'ltr',
                }}
            >
                <Stack
                    spacing={3}
                >
                    <Skeleton
                        variant="rounded"
                        height={40}
                        width={180}
                    />

                    <Card
                        sx={{
                            p: 4,
                            borderRadius: 4,
                        }}
                    >
                        <Stack
                            direction={{
                                xs: 'column',
                                lg: 'row',
                            }}
                            spacing={5}
                        >
                            <Skeleton
                                variant="rounded"
                                sx={{
                                    width:
                                        '100%',
                                    height: {
                                        xs: 350,
                                        md: 520,
                                    },
                                }}
                            />

                            <Stack
                                spacing={2}
                                sx={{
                                    flex: 1,
                                }}
                            >
                                <Skeleton
                                    height={30}
                                    width="40%"
                                />

                                <Skeleton
                                    height={60}
                                    width="80%"
                                />

                                <Skeleton
                                    height={30}
                                    width="30%"
                                />

                                <Skeleton
                                    height={100}
                                />

                                <Skeleton
                                    height={50}
                                />
                            </Stack>
                        </Stack>
                    </Card>
                </Stack>
            </Container>
        );
    }

    if (error) {
        return (
            <Container
                maxWidth="lg"
                sx={{
                    py: 6,
                    direction:
                        isFa
                            ? 'rtl'
                            : 'ltr',
                }}
            >
                <Alert
                    severity="error"
                    action={
                        <Button
                            color="inherit"
                            size="small"
                            onClick={() =>
                                window.location.reload()
                            }
                        >
                            {getText(
                                'common.retry',
                                'Retry',
                            )}
                        </Button>
                    }
                >
                    {getText(
                        'catalog.productLoadError',
                        'Failed to load product.',
                    )}
                </Alert>
            </Container>
        );
    }

    if (!product) {
        return (
            <Container
                maxWidth="lg"
                sx={{
                    py: 6,
                    direction:
                        isFa
                            ? 'rtl'
                            : 'ltr',
                }}
            >
                <Typography
                    variant="h5"
                    sx={{
                        fontWeight: 800,
                        mb: 2,
                    }}
                >
                    {getText(
                        'catalog.productNotFound',
                        'Product not found.',
                    )}
                </Typography>

                <Button
                    component={Link}
                    to="/products"
                    startIcon={
                        <ArrowBack />
                    }
                    variant="outlined"
                >
                    {getText(
                        'catalog.backToProducts',
                        'Back to products',
                    )}
                </Button>
            </Container>
        );
    }

    return (
        <Box
            sx={{
                minHeight:
                    '100vh',
                py: {
                    xs: 3,
                    md: 5,
                },
                direction:
                    isFa
                        ? 'rtl'
                        : 'ltr',
            }}
        >
            <Container maxWidth="xl">
                <Stack spacing={3}>
                    <Button
                        component={Link}
                        to="/products"
                        startIcon={
                            <ArrowBack />
                        }
                        sx={{
                            alignSelf:
                                'flex-start',
                        }}
                    >
                        {getText(
                            'catalog.backToProducts',
                            'Back to products',
                        )}
                    </Button>

                    <Card
                        sx={{
                            p: {
                                xs: 2,
                                md: 4,
                            },
                            borderRadius: 4,
                        }}
                    >
                        <Stack
                            direction={{
                                xs: 'column',
                                lg: 'row',
                            }}
                            spacing={5}
                        >
                            <Box
                                sx={{
                                    width: {
                                        xs: '100%',
                                        lg: '50%',
                                    },
                                }}
                            >
                                <Box
                                    component="img"
                                    src={
                                        imageUrl
                                    }
                                    alt={
                                        product.name
                                    }
                                    loading="eager"
                                    sx={{
                                        width:
                                            '100%',
                                        height: {
                                            xs: 350,
                                            md: 520,
                                        },
                                        objectFit:
                                            'contain',
                                        borderRadius: 3,
                                        bgcolor:
                                            'background.default',
                                    }}
                                    onError={event => {
                                        const image =
                                            event.currentTarget;

                                        if (
                                            !image.src.endsWith(
                                                '/placeholder.jpg',
                                            )
                                        ) {
                                            image.src =
                                                '/placeholder.jpg';
                                        }
                                    }}
                                />

                                {images.length >
                                    1 && (
                                    <Stack
                                        direction="row"
                                        spacing={1}
                                        sx={{
                                            mt: 2,
                                            overflowX:
                                                'auto',
                                            pb: 1,
                                        }}
                                    >
                                        {images.map(
                                            (
                                                image,
                                                index,
                                            ) => (
                                                <IconButton
                                                    key={
                                                        image.id
                                                    }
                                                    onClick={() =>
                                                        setImageIndex(
                                                            index,
                                                        )
                                                    }
                                                    sx={{
                                                        p: 0.5,
                                                        border:
                                                            '2px solid',
                                                        borderColor:
                                                            safeImageIndex ===
                                                            index
                                                                ? 'primary.main'
                                                                : 'transparent',
                                                        borderRadius:
                                                            2,
                                                        flexShrink: 0,
                                                    }}
                                                >
                                                    <Box
                                                        component="img"
                                                        src={
                                                            image.imageUrl
                                                        }
                                                        alt={
                                                            image.altText ||
                                                            product.name
                                                        }
                                                        loading="lazy"
                                                        sx={{
                                                            width: 70,
                                                            height: 70,
                                                            objectFit:
                                                                'cover',
                                                            borderRadius:
                                                                1.5,
                                                        }}
                                                    />
                                                </IconButton>
                                            ),
                                        )}
                                    </Stack>
                                )}
                            </Box>

                            <Box
                                sx={{
                                    width: {
                                        xs: '100%',
                                        lg: '50%',
                                    },
                                }}
                            >
                                <Stack spacing={2.5}>
                                    {product.brandName && (
                                        <Typography
                                            variant="body2"
                                            color="text.secondary"
                                            sx={{
                                                fontWeight: 700,
                                            }}
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
                                            fontWeight: 900,
                                            lineHeight: 1.2,
                                            fontSize: {
                                                xs: '2rem',
                                                md: '3rem',
                                            },
                                        }}
                                    >
                                        {
                                            product.name
                                        }
                                    </Typography>

                                    <Stack
                                        direction="row"
                                        spacing={2}
                                        useFlexGap
                                        sx={{
                                            flexWrap: 'wrap',
                                            alignItems: 'center',
                                        }}
                                    >
                                        <Stack
                                            direction="row"
                                            spacing={1}
                                            sx={{
                                               
                                                alignItems: 'center',
                                            }}
                                        >
                                            <Rating
                                                value={
                                                    localizedRating
                                                }
                                                precision={
                                                    0.1
                                                }
                                                readOnly
                                                size="small"
                                            />

                                            <Typography
                                                variant="body2"
                                                color="text.secondary"
                                            >
                                                {formatPrice(
                                                    product.averageRating,
                                                )}{' '}
                                                (
                                                {
                                                    product.reviewCount
                                                }
                                                )
                                            </Typography>
                                        </Stack>

                                        {product.isInStock ? (
                                            <Chip
                                                icon={
                                                    <CheckCircleOutlined />
                                                }
                                                label={getText(
                                                    'catalog.inStock',
                                                    'In stock',
                                                )}
                                                color="success"
                                                size="small"
                                                variant="outlined"
                                            />
                                        ) : (
                                            <Chip
                                                label={getText(
                                                    'catalog.outOfStock',
                                                    'Out of stock',
                                                )}
                                                color="default"
                                                size="small"
                                            />
                                        )}
                                    </Stack>

                                    <Typography
                                        variant="body2"
                                        color="text.secondary"
                                    >
                                        {getText(
                                            'catalog.sku',
                                            'SKU',
                                        )}
                                        :{' '}
                                        {
                                            activeVariant?.sku ||
                                            product.sku
                                        }
                                    </Typography>

                                    <Divider />

                                    <Box>
                                        {comparePrice !==
                                            null &&
                                            comparePrice >
                                                price && (
                                                <Typography
                                                    variant="body2"
                                                    color="text.secondary"
                                                    sx={{
                                                        textDecoration:
                                                            'line-through',
                                                    }}
                                                >
                                                    {formatPrice(
                                                        comparePrice,
                                                    )}{' '}
                                                    {
                                                        currency
                                                    }
                                                </Typography>
                                            )}

                                        <Typography
                                            variant="h4"
                                            color="primary"
                                            sx={{
                                                fontWeight: 900,
                                                mt: 0.5,
                                            }}
                                        >
                                            {formatPrice(
                                                price,
                                            )}{' '}
                                            {
                                                currency
                                            }
                                        </Typography>

                                        {product.discountPercentage >
                                            0 && (
                                            <Typography
                                                variant="body2"
                                                color="success.main"
                                                sx={{
                                                    mt: 0.5,
                                                    fontWeight: 700,
                                                }}
                                            >
                                                -
                                                {
                                                    product.discountPercentage
                                                }
                                                %
                                            </Typography>
                                        )}
                                    </Box>

                                    {genericVariantMode &&
                                        attributeGroups.length >
                                            0 && (
                                            <Stack
                                                spacing={2.5}
                                            >
                                                {attributeGroups.map(
                                                    group => {
                                                        const code =
                                                            group.code
                                                                .trim()
                                                                .toLowerCase();

                                                        const selectedValueId =
                                                            selectedAttributes[
                                                                code
                                                            ] ??
                                                            '';

                                                        return (
                                                            <Box
                                                                key={
                                                                    code
                                                                }
                                                            >
                                                                <Typography
                                                                    variant="h6"
                                                                    sx={{
                                                                        fontWeight: 800,
                                                                        mb: 1,
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
                                                                        flexWrap: 'wrap',
                                                                    }}
                                                                >
                                                                    {group.values.map(
                                                                        value => {
                                                                            const selected =
                                                                                selectedValueId ===
                                                                                value.id;

                                                                            const available =
                                                                                isAttributeValueAvailable(
                                                                                    group.code,
                                                                                    value.id,
                                                                                );

                                                                            return (
                                                                                <Button
                                                                                    key={
                                                                                        value.id
                                                                                    }
                                                                                    variant={
                                                                                        selected
                                                                                            ? 'contained'
                                                                                            : 'outlined'
                                                                                    }
                                                                                    disabled={
                                                                                        !available
                                                                                    }
                                                                                    onClick={() =>
                                                                                        handleAttributeSelect(
                                                                                            group.code,
                                                                                            value.id,
                                                                                        )
                                                                                    }
                                                                                    sx={{
                                                                                        minWidth:
                                                                                            80,
                                                                                        minHeight:
                                                                                            42,
                                                                                        borderRadius:
                                                                                            2,
                                                                                        textTransform:
                                                                                            'none',
                                                                                        opacity:
                                                                                            available
                                                                                                ? 1
                                                                                                : 0.45,
                                                                                    }}
                                                                                >
                                                                                    {value.colorHex && (
                                                                                        <Box
                                                                                            component="span"
                                                                                            sx={{
                                                                                                width: 16,
                                                                                                height: 16,
                                                                                                borderRadius:
                                                                                                    '50%',
                                                                                                backgroundColor:
                                                                                                    value.colorHex,
                                                                                                border:
                                                                                                    '1px solid',
                                                                                                borderColor:
                                                                                                    'divider',
                                                                                                mr: 1,
                                                                                            }}
                                                                                        />
                                                                                    )}

                                                                                    {
                                                                                        value.label
                                                                                    }
                                                                                </Button>
                                                                            );
                                                                        },
                                                                    )}
                                                                </Stack>
                                                            </Box>
                                                        );
                                                    },
                                                )}
                                            </Stack>
                                        )}

                                    {!genericVariantMode &&
                                        availableVariants.length >
                                            1 && (
                                            <Box>
                                                <Typography
                                                    variant="h6"
                                                    sx={{
                                                        fontWeight: 800,
                                                        mb: 1,
                                                    }}
                                                >
                                                    {getText(
                                                        'catalog.variant',
                                                        'Variant',
                                                    )}
                                                </Typography>

                                                <Stack
                                                    direction="row"
                                                    spacing={1}
                                                    useFlexGap
                                                sx={{
                                                    flexWrap: 'wrap',
                                                }}
                                                >
                                                    {availableVariants.map(
                                                        variant => {
                                                            const selected =
                                                                variant.id ===
                                                                activeLegacyVariant?.id;

                                                            return (
                                                                <Button
                                                                    key={
                                                                        variant.id
                                                                    }
                                                                    variant={
                                                                        selected
                                                                            ? 'contained'
                                                                            : 'outlined'
                                                                    }
                                                                    onClick={() => {
                                                                        setSelectedLegacyVariantId(
                                                                            variant.id,
                                                                        );
                                                                        setQuantity(
                                                                            1,
                                                                        );
                                                                        setAdded(
                                                                            false,
                                                                        );
                                                                    }}
                                                                    sx={{
                                                                        textTransform:
                                                                            'none',
                                                                        borderRadius:
                                                                            2,
                                                                    }}
                                                                >
                                                                    {getVariantLabel(
                                                                        variant,
                                                                    )}
                                                                </Button>
                                                            );
                                                        },
                                                    )}
                                                </Stack>
                                            </Box>
                                        )}

                                    {genericVariantMode &&
                                        !hasCompleteGenericSelection && (
                                            <Alert
                                                severity="info"
                                                variant="outlined"
                                            >
                                                {getText(
                                                    'catalog.selectAllVariants',
                                                    'Please select all available options before adding the product to your cart.',
                                                )}
                                            </Alert>
                                        )}

                                    {genericVariantMode &&
                                        hasCompleteGenericSelection &&
                                        !matchingVariant && (
                                            <Alert
                                                severity="warning"
                                                variant="outlined"
                                            >
                                                {getText(
                                                    'catalog.variantUnavailable',
                                                    'The selected combination is not available.',
                                                )}
                                            </Alert>
                                        )}

                                    <Stack
                                        direction={{
                                            xs: 'column',
                                            sm: 'row',
                                        }}
                                        spacing={2}
                                        sx={{
                                            alignItems: {
                                                xs: 'stretch',
                                                sm: 'center',
                                            },
                                        }}
                                    >
                                        <Stack
                                            direction="row"
                                            spacing={1}
                                            sx={{
                                                alignItems: 'center',
                                            }}
                                        >
                                            <IconButton
                                                onClick={
                                                    decrease
                                                }
                                                disabled={
                                                    quantity <=
                                                    1
                                                }
                                                aria-label={getText(
                                                    'catalog.decreaseQuantity',
                                                    'Decrease quantity',
                                                )}
                                            >
                                                <Remove />
                                            </IconButton>

                                            <Typography
                                                sx={{
                                                    minWidth: 32,
                                                    textAlign:
                                                        'center',
                                                    fontWeight: 800,
                                                }}
                                            >
                                                {
                                                    quantity
                                                }
                                            </Typography>

                                            <IconButton
                                                onClick={
                                                    increase
                                                }
                                                disabled={
                                                    maxQuantity <=
                                                        0 ||
                                                    quantity >=
                                                        maxQuantity
                                                }
                                                aria-label={getText(
                                                    'catalog.increaseQuantity',
                                                    'Increase quantity',
                                                )}
                                            >
                                                <Add />
                                            </IconButton>
                                        </Stack>

                                        <Button
                                            fullWidth
                                            size="large"
                                            variant="contained"
                                            startIcon={
                                                added ? (
                                                    <CheckCircleOutlined />
                                                ) : (
                                                    <ShoppingCartOutlined />
                                                )
                                            }
                                            onClick={
                                                addToCart
                                            }
                                            disabled={
                                                !canAddToCart ||
                                                add.isPending
                                            }
                                            sx={{
                                                minHeight: 52,
                                                borderRadius: 2.5,
                                                fontWeight: 800,
                                            }}
                                        >
                                            {add.isPending
                                                ? getText(
                                                      'catalog.addingToCart',
                                                      'Adding...',
                                                  )
                                                : added
                                                  ? getText(
                                                        'catalog.addedToCart',
                                                        'Added to cart',
                                                    )
                                                  : getText(
                                                        'catalog.addToCart',
                                                        'Add to cart',
                                                    )}
                                        </Button>
                                    </Stack>

                                    {maxQuantity > 0 && (
                                        <Typography
                                            variant="caption"
                                            color="text.secondary"
                                        >
                                            {getText(
                                                'catalog.availableQuantity',
                                                'Available quantity',
                                            )}
                                            :{' '}
                                            {formatPrice(
                                                maxQuantity,
                                            )}
                                        </Typography>
                                    )}

                                    {product.shortDescription && (
                                        <>
                                            <Divider />

                                            <Typography
                                                color="text.secondary"
                                                sx={{
                                                    lineHeight: 1.9,
                                                }}
                                            >
                                                {
                                                    product.shortDescription
                                                }
                                            </Typography>
                                        </>
                                    )}

                                    {(product.description ||
                                        product.categories.length >
                                            0 ||
                                        product.manufacturerName) && (
                                        <>
                                            <Divider />

                                            <Stack
                                                spacing={2}
                                            >
                                                {product.description && (
                                                    <Box>
                                                        <Typography
                                                            variant="h6"
                                                            sx={{
                                                                fontWeight: 800,
                                                                mb: 1,
                                                            }}
                                                        >
                                                            {getText(
                                                                'catalog.description',
                                                                'Description',
                                                            )}
                                                        </Typography>

                                                        <Typography
                                                            color="text.secondary"
                                                            sx={{
                                                                whiteSpace:
                                                                    'pre-line',
                                                                lineHeight: 1.9,
                                                            }}
                                                        >
                                                            {
                                                                product.description
                                                            }
                                                        </Typography>
                                                    </Box>
                                                )}

                                                {product.categories.length >
                                                    0 && (
                                                    <Box>
                                                        <Typography
                                                            variant="body2"
                                                            color="text.secondary"
                                                            sx={{
                                                                mb: 1,
                                                                fontWeight: 700,
                                                            }}
                                                        >
                                                            {getText(
                                                                'catalog.categories',
                                                                'Categories',
                                                            )}
                                                        </Typography>

                                                        <Stack
                                                            direction="row"
                                                            spacing={1}
                                                            useFlexGap
                                                            sx={{
                                                                flexWrap: 'wrap',
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
                                                    </Box>
                                                )}

                                                {product.manufacturerName && (
                                                    <Typography
                                                        variant="body2"
                                                        color="text.secondary"
                                                    >
                                                        {getText(
                                                            'catalog.manufacturer',
                                                            'Manufacturer',
                                                        )}
                                                        :{' '}
                                                        {
                                                            product.manufacturerName
                                                        }
                                                    </Typography>
                                                )}
                                            </Stack>
                                        </>
                                    )}
                                </Stack>
                            </Box>
                        </Stack>
                    </Card>
                </Stack>
            </Container>
        </Box>
    );
}
