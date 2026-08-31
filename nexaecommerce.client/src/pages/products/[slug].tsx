import React, {
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
    useParams,
} from 'react-router-dom';

import {
    useProductBySlug,
} from '@/modules/catalog/hooks/useProducts';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';

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

export default function ProductDetailPage() {
    const {
        slug,
    } = useParams();

    const {
        data: product,
        isLoading,
        error,
    } = useProductBySlug(
        slug,
    );

    const {
        add,
    } =
        useCartMutations();

    const [
        selectedImage,
        setSelectedImage,
    ] = useState(0);

    const [
        selectedVariantId,
        setSelectedVariantId,
    ] = useState<
        string | null
    >(null);

    const [
        quantity,
        setQuantity,
    ] = useState(1);

    const selectedVariant =
        useMemo(
            () =>
                product?.variants.find(
                    x =>
                        x.id ===
                        selectedVariantId,
                ) ??
                null,
            [
                product,
                selectedVariantId,
            ],
        );

    if (isLoading) {
        return (
            <Box
                sx={{
                    minHeight: '65vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    if (error) {
        return (
            <Container
                maxWidth="lg"
                sx={{
                    py: 6,
                }}
            >
                <Alert severity="error">
                    خطا در دریافت محصول.
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
                }}
            >
                <Alert severity="warning">
                    محصول پیدا نشد.
                </Alert>
            </Container>
        );
    }

    const activeImages =
        product.images.length > 0
            ? product.images
            : [
                {
                    id: 'fallback',
                    imageUrl:
                        '/placeholder.jpg',
                    altText:
                        product.name,
                    displayOrder: 0,
                    isMain: true,
                },
            ];

    const price =
        selectedVariant?.priceOverride ??
        product.finalPrice;

    const maxStock =
        selectedVariant?.stockQuantity ??
        product.stockQuantity;

    const hasDiscount =
        Boolean(
            product.comparePrice &&
            product.comparePrice >
            price,
        );

    const handleAddToCart =
        () => {
            if (
                !product.isInStock ||
                maxStock <= 0
            ) {
                return;
            }

            const variantId =
                selectedVariant?.id ??
                product.variants.find(
                    x =>
                        x.isActive &&
                        x.stockQuantity > 0,
                )?.id;

            if (!variantId) {
                return;
            }

            add.mutate({
                productVariantId:
                    variantId,
                quantity,
            });
        };

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
                        display: 'grid',
                        gridTemplateColumns:
                        {
                            xs: '1fr',
                            md: '1.1fr 1fr',
                        },
                        gap: {
                            xs: 3,
                            md: 6,
                        },
                    }}
                >
                    <Box>
                        <Box
                            sx={{
                                backgroundColor:
                                    '#fff',
                                border:
                                    '1px solid',
                                borderColor:
                                    'divider',
                                borderRadius: 4,
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
                                    width: '100%',
                                    height: {
                                        xs: 350,
                                        md: 560,
                                    },
                                    objectFit:
                                        'contain',
                                    backgroundColor:
                                        '#f7f7f7',
                                }}
                                onError={(
                                    e: React.SyntheticEvent<HTMLImageElement>,
                                ) => {
                                    e.currentTarget.src =
                                        '/placeholder.jpg';
                                }}
                            />
                        </Box>

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
                                            width: 82,
                                            height: 82,
                                            flexShrink: 0,
                                            border:
                                                '2px solid',
                                            borderColor:
                                                selectedImage ===
                                                    index
                                                    ? 'primary.main'
                                                    : 'divider',
                                            borderRadius: 2,
                                            overflow:
                                                'hidden',
                                            p: 0,
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
                                        lineHeight: 2,
                                    }}
                                >
                                    {
                                        product.shortDescription
                                    }
                                </Typography>
                            )}

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
                                            product.comparePrice!,
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
                            </Box>

                            {product.variants.some(
                                x =>
                                    x.isActive,
                            ) && (
                                    <Box>
                                        <Typography
                                            variant="subtitle1"
                                            sx={{
                                                fontWeight:
                                                    800,
                                                mb: 1.5,
                                            }}
                                        >
                                            انتخاب مشخصات
                                        </Typography>

                                        <Stack
                                            direction="row"
                                            spacing={1}
                                            sx={{
                                                flexWrap:
                                                    'wrap',
                                            }}
                                        >
                                            {product.variants
                                                .filter(
                                                    x =>
                                                        x.isActive,
                                                )
                                                .map(
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
                                                                variant={
                                                                    selected
                                                                        ? 'contained'
                                                                        : 'outlined'
                                                                }
                                                                disabled={
                                                                    disabled
                                                                }
                                                                onClick={() =>
                                                                    setSelectedVariantId(
                                                                        variant.id,
                                                                    )
                                                                }
                                                                sx={{
                                                                    borderRadius:
                                                                        2,
                                                                }}
                                                            >
                                                                {variant.color ??
                                                                    variant.size ??
                                                                    variant.sku}
                                                            </Button>
                                                        );
                                                    },
                                                )}
                                        </Stack>
                                    </Box>
                                )}

                            <Stack
                                direction="row"
                                spacing={2}
                                sx={{
                                    alignItems:
                                        'center',
                                }}
                            >
                                <Button
                                    variant="outlined"
                                    disabled={
                                        quantity <=
                                        1 ||
                                        add.isPending
                                    }
                                    onClick={() =>
                                        setQuantity(
                                            value =>
                                                value -
                                                1,
                                        )
                                    }
                                >
                                    −
                                </Button>

                                <Typography
                                    sx={{
                                        minWidth: 30,
                                        textAlign:
                                            'center',
                                        fontWeight:
                                            800,
                                    }}
                                >
                                    {
                                        quantity
                                    }
                                </Typography>

                                <Button
                                    variant="outlined"
                                    disabled={
                                        quantity >=
                                        maxStock ||
                                        add.isPending
                                    }
                                    onClick={() =>
                                        setQuantity(
                                            value =>
                                                value +
                                                1,
                                        )
                                    }
                                >
                                    +
                                </Button>

                                <Typography
                                    variant="caption"
                                    color="text.secondary"
                                >
                                    موجودی:{' '}
                                    {
                                        maxStock
                                    }
                                </Typography>
                            </Stack>

                            <Button
                                fullWidth
                                size="large"
                                variant="contained"
                                startIcon={
                                    <ShoppingCartOutlined />
                                }
                                disabled={
                                    !product.isInStock ||
                                    maxStock <=
                                    0 ||
                                    add.isPending
                                }
                                onClick={
                                    handleAddToCart
                                }
                                sx={{
                                    borderRadius: 3,
                                    py: 1.8,
                                    fontWeight:
                                        800,
                                }}
                            >
                                {add.isPending
                                    ? 'در حال افزودن...'
                                    : 'افزودن به سبد'}
                            </Button>

                            {add.isSuccess && (
                                <Alert severity="success">
                                    محصول به سبد خرید
                                    اضافه شد.
                                </Alert>
                            )}

                            {add.error && (
                                <Alert severity="error">
                                    خطا در افزودن محصول
                                    به سبد خرید.
                                </Alert>
                            )}

                            <Divider />

                            {product.description && (
                                <Box>
                                    <Typography
                                        variant="h6"
                                        sx={{
                                            fontWeight:
                                                800,
                                            mb: 1,
                                        }}
                                    >
                                        توضیحات محصول
                                    </Typography>

                                    <Typography
                                        color="text.secondary"
                                        sx={{
                                            lineHeight:
                                                2,
                                            whiteSpace:
                                                'pre-line',
                                        }}
                                    >
                                        {
                                            product.description
                                        }
                                    </Typography>
                                </Box>
                            )}
                        </Stack>
                    </Box>
                </Box>
            </Container>
        </Box>
    );
}