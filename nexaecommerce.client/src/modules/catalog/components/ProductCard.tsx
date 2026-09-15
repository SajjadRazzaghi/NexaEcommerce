import {
    useState,
} from 'react';

import {
    Link,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    IconButton,
    Stack,
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
    type Product,
    type ProductListItem,
    type ProductVariant,
} from '../api/products';

import ProductVariantPickerDialog from './ProductVariantPickerDialog';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';

interface ProductCardProps {
    product: ProductListItem;
}

function getProductUrl(
    slug: string,
): string {
    return `/ products / ${
    encodeURIComponent(
        slug.trim(),
    )
} `;
}

export default function ProductCard({
    product,
}: ProductCardProps) {
    const {
        i18n,
    } = useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

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
    ] = useState<string | null>(
        null,
    );

    const [
        variantDialogOpen,
        setVariantDialogOpen,
    ] = useState(false);

    const [
        variantProduct,
        setVariantProduct,
    ] = useState<Product | null>(
        null,
    );

    const [
        variantLoading,
        setVariantLoading,
    ] = useState(false);

    const {
        add,
    } = useCartMutations();

    const price =
        typeof product.finalPrice ===
            'number'
            ? product.finalPrice
            : product.price;

    const hasDiscount =
        product.comparePrice != null &&
        product.comparePrice > price;

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
        product.mainImage?.trim() ||
        '/placeholder.jpg';

    const formatPrice = (
        value: number,
    ) =>
        new Intl.NumberFormat(
            isFa
                ? 'fa-IR'
                : undefined,
            {
                maximumFractionDigits: 0,
            },
        ).format(value);

    const closeVariantDialog =
        () => {
            setVariantDialogOpen(
                false,
            );

            setVariantProduct(
                null,
            );

            setVariantLoading(
                false,
            );
        };

    const handleAddToCart =
        async () => {
            if (
                !product.isInStock ||
                add.isPending ||
                variantLoading
            ) {
                return;
            }

            setErrorMessage(
                null,
            );

            setVariantLoading(
                true,
            );

            try {
                /*
                 * ProductListItem intentionally does not
                 * contain the complete variant collection.
                 * Load the canonical product by slug.
                 */
                const fullProduct =
                    await productsApi.getBySlug(
                        product.slug,
                    );

                const sellableVariants =
                    fullProduct.variants.filter(
                        variant =>
                            variant.isActive &&
                            variant.stockQuantity > 0,
                    );

                if (
                    sellableVariants.length === 0
                ) {
                    setErrorMessage(
                        isFa
                            ? 'این محصول در حال حاضر موجود نیست.'
                            : 'This product is currently out of stock.',
                    );

                    return;
                }

                if (
                    sellableVariants.length === 1
                ) {
                    await add.mutateAsync({
                        productVariantId:
                            sellableVariants[0]
                                .id,
                        quantity: 1,
                    });

                    setAdded(
                        true,
                    );

                    return;
                }

                /*
                 * Multiple sellable variants:
                 * customer must select the exact variant.
                 */
                setVariantProduct(
                    fullProduct,
                );

                setVariantDialogOpen(
                    true,
                );
            } catch {
                setErrorMessage(
                    isFa
                        ? 'افزودن محصول به سبد خرید انجام نشد.'
                        : 'The product could not be added to the cart.',
                );
            } finally {
                setVariantLoading(
                    false,
                );
            }
        };

    const handleVariantConfirm =
        async (
            variant: ProductVariant,
        ) => {
            if (
                !variant.isActive ||
                variant.stockQuantity <= 0 ||
                add.isPending
            ) {
                return;
            }

            setErrorMessage(
                null,
            );

            try {
                await add.mutateAsync({
                    productVariantId:
                        variant.id,
                    quantity: 1,
                });

                setAdded(
                    true,
                );

                closeVariantDialog();
            } catch {
                setErrorMessage(
                    isFa
                        ? 'افزودن محصول به سبد خرید انجام نشد.'
                        : 'The product could not be added to the cart.',
                );
            }
        };

    const productUrl =
        getProductUrl(
            product.slug,
        );

    const stockLabel =
        product.isInStock
            ? (
                isFa
                    ? 'موجود'
                    : 'In stock'
            )
            : (
                isFa
                    ? 'ناموجود'
                    : 'Out of stock'
            );

    const addedLabel =
        isFa
            ? 'مشاهده سبد'
            : 'View cart';

    const addLabel =
        isFa
            ? 'افزودن به سبد'
            : 'Add to cart';

    const addingLabel =
        isFa
            ? 'در حال افزودن...'
            : 'Adding...';

    const viewLabel =
        isFa
            ? 'مشاهده'
            : 'View';

    return (
        <>
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
                        boxShadow: 6,
                    },
                }}
            >
                {discountPercentage > 0 && (
                    <Chip
                        label={
                            isFa
                                ? `${ discountPercentage }٪ تخفیف`
                                : `${ discountPercentage }% OFF`
                        }
                        color="error"
                        size="small"
                        sx={{
                            position:
                                'absolute',
                            top: 12,
                            right: 12,
                            zIndex: 3,
                            fontWeight: 700,
                        }}
                    />
                )}

                <IconButton
                    aria-label={
                        favorite
                            ? (
                                isFa
                                    ? 'حذف از علاقه‌مندی‌ها'
                                    : 'Remove from favorites'
                            )
                            : (
                                isFa
                                    ? 'افزودن به علاقه‌مندی‌ها'
                                    : 'Add to favorites'
                            )
                    }
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
                        <Favorite
                            color="error"
                        />
                    ) : (
                        <FavoriteBorder />
                    )}
                </IconButton>

                <Box
                    component={Link}
                    to={productUrl}
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
                        alt={
                            product.name
                        }
                        loading="lazy"
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
                            if (
                                event
                                    .currentTarget
                                    .src.endsWith(
                                        '/placeholder.jpg',
                                    )
                            ) {
                                return;
                            }

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
                        flexGrow: 1,
                        p: 2,
                        gap: 1,
                        textAlign:
                            isFa
                                ? 'right'
                                : 'left',
                        direction:
                            isFa
                                ? 'rtl'
                                : 'ltr',
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
                        to={productUrl}
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
                                {
                                    formatPrice(
                                        product.comparePrice,
                                    )
                                }{' '}
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
                        {
                            formatPrice(
                                price,
                            )
                        }{' '}
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
                        • {stockLabel}
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
                            component={
                                Link
                            }
                            to={
                                productUrl
                            }
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
                            {
                                viewLabel
                            }
                        </Button>

                        {added ? (
                            <Button
                                component={
                                    Link
                                }
                                to="/cart"
                                variant="contained"
                                startIcon={
                                    <CheckCircleOutlined />
                                }
                                sx={{
                                    borderRadius:
                                        2,
                                    flex: 1.5,
                                    fontWeight:
                                        800,
                                    minHeight:
                                        46,
                                    whiteSpace:
                                        'nowrap',
                                }}
                            >
                                {
                                    addedLabel
                                }
                            </Button>
                        ) : (
                            <Button
                                type="button"
                                variant="contained"
                                disabled={
                                    !product.isInStock ||
                                    add.isPending ||
                                    variantLoading
                                }
                                startIcon={
                                    add.isPending ||
                                    variantLoading
                                        ? (
                                            <ShoppingCartOutlined />
                                        )
                                        : (
                                            <ShoppingCartOutlined />
                                        )
                                }
                                onClick={
                                    handleAddToCart
                                }
                                sx={{
                                    borderRadius:
                                        2,
                                    flex: 1.5,
                                    fontWeight:
                                        800,
                                    minHeight:
                                        46,
                                    whiteSpace:
                                        'nowrap',
                                }}
                            >
                                {add.isPending ||
                                variantLoading
                                    ? addingLabel
                                    : addLabel}
                            </Button>
                        )}
                    </Stack>
                </CardContent>
            </Card>

            <ProductVariantPickerDialog
                open={
                    variantDialogOpen
                }
                product={
                    variantProduct
                }
                loading={
                    variantLoading ||
                    add.isPending
                }
                onClose={
                    closeVariantDialog
                }
                onConfirm={
                    handleVariantConfirm
                }
            />
        </>
    );
}
