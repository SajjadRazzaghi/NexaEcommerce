import { useState } from 'react';

import {
    Link,
} from 'react-router-dom';

import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    IconButton,
    Rating,
    Stack,
    Tooltip,
    Typography,
} from '@mui/material';

import {
    Favorite,
    FavoriteBorder,
    ShoppingCartOutlined,
    CheckCircleOutlined,
} from '@mui/icons-material';

import {
    productsApi,
    type ProductListItem,
} from '../api/products';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';

interface ProductCardProps {
    product: ProductListItem;
}

export default function ProductCard({
    product,
}: ProductCardProps) {
    const [
        favorite,
        setFavorite,
    ] = useState(false);

    const [
        added,
        setAdded,
    ] = useState(false);

    const [
        errorMessage,
        setErrorMessage,
    ] = useState<string | null>(null);

    const {
        add,
    } = useCartMutations();

    const price =
        product.finalPrice ??
        product.price;

    const hasDiscount =
        product.comparePrice != null &&
        product.comparePrice >
        price;

    const discountPercentage =
        product.discountPercentage > 0
            ? Math.round(
                product.discountPercentage,
            )
            : hasDiscount &&
                product.comparePrice
                ? Math.round(
                    (
                        (
                            product.comparePrice -
                            price
                        ) /
                        product.comparePrice
                    ) *
                    100,
                )
                : 0;

    const image =
        product.mainImage ||
        '/placeholder.jpg';

    const formatPrice = (
        value: number,
    ) =>
        new Intl.NumberFormat(
            'fa-IR',
        ).format(value);

    const handleAddToCart =
        async () => {
            if (
                !product.isInStock ||
                add.isPending
            ) {
                return;
            }

            setErrorMessage(
                null,
            );

            try {
                /*
                 * Product list items do not contain
                 * the variant id required by Cart.
                 *
                 * Load the complete product first and
                 * select the first active sellable variant.
                 */
                const fullProduct =
                    await productsApi.getBySlug(
                        product.slug,
                    );

                const sellableVariant =
                    fullProduct.variants.find(
                        variant =>
                            variant.isActive &&
                            variant.stockQuantity >
                            0,
                    );

                if (
                    !sellableVariant
                ) {
                    setErrorMessage(
                        'این محصول در حال حاضر موجود نیست.',
                    );

                    return;
                }

                await add.mutateAsync({
                    productVariantId:
                        sellableVariant.id,
                    quantity: 1,
                });

                setAdded(
                    true,
                );
            } catch {
                setErrorMessage(
                    'افزودن محصول به سبد خرید انجام نشد.',
                );
            }
        };

    return (
        <Card
            sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                position: 'relative',
                overflow: 'hidden',
                borderRadius: 3,
                border: '1px solid',
                borderColor:
                    'divider',
                backgroundColor:
                    'background.paper',
                transition:
                    'transform .2s ease, box-shadow .2s ease',
                '&:hover': {
                    transform:
                        'translateY(-4px)',
                    boxShadow:
                        6,
                },
            }}
        >
            {discountPercentage > 0 && (
                <Chip
                    label={`${discountPercentage}% OFF`}
                    color="error"
                    size="small"
                    sx={{
                        position:
                            'absolute',
                        top: 12,
                        right: 12,
                        zIndex: 3,
                        fontWeight:
                            700,
                    }}
                />
            )}

            <Tooltip
                title={
                    favorite
                        ? 'Remove from favorites'
                        : 'Add to favorites'
                }
            >
                <IconButton
                    onClick={() =>
                        setFavorite(
                            value =>
                                !value,
                        )
                    }
                    sx={{
                        position:
                            'absolute',
                        top: 8,
                        left: 8,
                        zIndex: 3,
                        backgroundColor:
                            'rgba(255,255,255,.92)',
                        '&:hover': {
                            backgroundColor:
                                '#fff',
                        },
                    }}
                >
                    {favorite ? (
                        <Favorite color="error" />
                    ) : (
                        <FavoriteBorder />
                    )}
                </IconButton>
            </Tooltip>

            <Box
                component={Link}
                to={`/products/${encodeURIComponent(
                    product.slug,
                )}`}
                sx={{
                    display:
                        'block',
                    overflow:
                        'hidden',
                    backgroundColor:
                        '#f7f7f7',
                    textDecoration:
                        'none',
                }}
            >
                <Box
                    component="img"
                    src={image}
                    alt={product.name}
                    sx={{
                        display:
                            'block',
                        width:
                            '100%',
                        height: {
                            xs: 220,
                            sm: 230,
                            md: 240,
                        },
                        objectFit:
                            'cover',
                        transition:
                            'transform .4s ease',
                        '.MuiCard-root:hover &':
                        {
                            transform:
                                'scale(1.05)',
                        },
                    }}
                    onError={event => {
                        event.currentTarget.src =
                            '/placeholder.jpg';
                    }}
                />
            </Box>

            <CardContent
                sx={{
                    display:
                        'flex',
                    flexDirection:
                        'column',
                    flexGrow:
                        1,
                    p: 2,
                    gap: 1,
                    textAlign:
                        'right',
                    direction:
                        'rtl',
                }}
            >
                {product.brandName && (
                    <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{
                            fontWeight:
                                600,
                        }}
                    >
                        {
                            product.brandName
                        }
                    </Typography>
                )}

                <Typography
                    component={Link}
                    to={`/products/${encodeURIComponent(
                        product.slug,
                    )}`}
                    variant="subtitle1"
                    sx={{
                        color:
                            'text.primary',
                        textDecoration:
                            'none',
                        fontWeight:
                            700,
                        lineHeight:
                            1.7,
                        minHeight:
                            56,
                        display:
                            '-webkit-box',
                        WebkitLineClamp:
                            2,
                        WebkitBoxOrient:
                            'vertical',
                        overflow:
                            'hidden',
                        '&:hover': {
                            color:
                                'primary.main',
                        },
                    }}
                >
                    {
                        product.name
                    }
                </Typography>

                <Stack
                    direction="row"
                    spacing={1}
                    sx={{
                        direction: 'ltr',
                        alignItems: 'center',
                    }}
                >
                    <Rating
                        value={0}
                        precision={0.5}
                        size="small"
                        readOnly
                    />

                    <Typography
                        variant="caption"
                        color="text.secondary"
                    >
                        No reviews
                    </Typography>
                </Stack>

                <Box
                    sx={{
                        flexGrow:
                            1,
                    }}
                />

                {hasDiscount &&
                    product.comparePrice !=
                    null && (
                        <Typography
                            variant="body2"
                            color="text.secondary"
                            sx={{
                                textDecoration:
                                    'line-through',
                            }}
                        >
                            {formatPrice(
                                product.comparePrice,
                            )}{' '}
                            {
                                product.currency
                            }
                        </Typography>
                    )}

                <Typography
                    variant="h6"
                    color="primary"
                    sx={{
                        fontWeight:
                            900,
                        fontSize:
                            '1.15rem',
                    }}
                >
                    {formatPrice(
                        price,
                    )}{' '}
                    {
                        product.currency
                    }
                </Typography>

                <Typography
                    variant="caption"
                    sx={{
                        color:
                            product.isInStock
                                ? 'success.main'
                                : 'error.main',
                        fontWeight:
                            700,
                    }}
                >
                    {product.isInStock
                        ? '● موجود در انبار'
                        : '● ناموجود'}
                </Typography>

                {errorMessage && (
                    <Typography
                        variant="caption"
                        color="error"
                        sx={{
                            fontWeight:
                                600,
                            lineHeight:
                                1.6,
                        }}
                    >
                        {
                            errorMessage
                        }
                    </Typography>
                )}

                <Stack
                    direction="row"
                    spacing={1}
                    sx={{
                        mt: 1,
                    }}
                >
                    <Button
                        component={Link}
                        to={`/products/${encodeURIComponent(
                            product.slug,
                        )}`}
                        variant="outlined"
                        sx={{
                            minWidth:
                                48,
                            borderRadius:
                                2,
                            flex: 1,
                            fontWeight:
                                700,
                            minHeight:
                                46,
                        }}
                    >
                        مشاهده
                    </Button>

                    <Button
                        type="button"
                        variant="contained"
                        disabled={
                            !product.isInStock ||
                            add.isPending
                        }
                        startIcon={
                            add.isPending ? (
                                <ShoppingCartOutlined />
                            ) : added ? (
                                <CheckCircleOutlined />
                            ) : (
                                <ShoppingCartOutlined />
                            )
                        }
                        onClick={
                            added
                                ? undefined
                                : handleAddToCart
                        }
                        component={
                            added
                                ? Link
                                : 'button'
                        }
                        to={
                            added
                                ? '/cart'
                                : undefined
                        }
                        sx={{
                            borderRadius:
                                2,
                            flex:
                                1.5,
                            fontWeight:
                                800,
                            minHeight:
                                46,
                            whiteSpace:
                                'nowrap',
                        }}
                    >
                        {add.isPending
                            ? 'در حال افزودن...'
                            : added
                                ? 'مشاهده سبد'
                                : 'افزودن به سبد'}
                    </Button>
                </Stack>
            </CardContent>
        </Card>
    );
}