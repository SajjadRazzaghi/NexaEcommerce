import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';

import {
    Alert,
    Box,
    Button,
    Card,
    Container,
    Divider,
    IconButton,
    Skeleton,
    Stack,
    TextField,
    Typography,
} from '@mui/material';

import {
    Add,
    ArrowBack,
    CheckCircleOutlined,
    Remove,
    ShoppingCartOutlined,
} from '@mui/icons-material';

import { useProduct } from '../hooks/useProducts';
import { useCartMutations } from '@/modules/cart/hooks/useCartMutations';

export default function ProductDetailPage() {
    const { id } = useParams<{ id: string }>();

    const {
        data: product,
        isLoading,
        error,
    } = useProduct(id);

    const { add } = useCartMutations();

    const [imageIndex, setImageIndex] = useState(0);
    const [variantId, setVariantId] = useState<string | null>(null);
    const [quantity, setQuantity] = useState(1);
    const [added, setAdded] = useState(false);

    const availableVariants = useMemo(
        () =>
            product?.variants?.filter(
                (variant) =>
                    variant.isActive &&
                    variant.stockQuantity > 0,
            ) ?? [],
        [product],
    );

    const activeVariantId =
        variantId ?? availableVariants[0]?.id ?? null;

    const activeVariant = availableVariants.find(
        (variant) => variant.id === activeVariantId,
    );

    const images = product?.images ?? [];

    const imageUrl =
        images[imageIndex]?.imageUrl ??
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

    const formatPrice = (value: number) =>
        new Intl.NumberFormat('en-US').format(value);

    if (isLoading) {
        return (
            <Container maxWidth="xl" sx={{ py: 5 }}>
                <Skeleton
                    variant="rectangular"
                    height={500}
                    sx={{ borderRadius: 3 }}
                />
            </Container>
        );
    }

    if (error) {
        return (
            <Container maxWidth="lg" sx={{ py: 6 }}>
                <Alert severity="error">
                    Failed to load product.
                </Alert>
            </Container>
        );
    }

    if (!product) {
        return (
            <Container maxWidth="lg" sx={{ py: 6 }}>
                <Typography
                    variant="h5"
                    sx={{ fontWeight: 800, mb: 2 }}
                >
                    Product not found
                </Typography>

                <Button
                    component={Link}
                    to="/products"
                    startIcon={<ArrowBack />}
                    variant="outlined"
                >
                    Back to products
                </Button>
            </Container>
        );
    }

    const decrease = () => {
        setQuantity((value) => Math.max(1, value - 1));
    };

    const increase = () => {
        setQuantity((value) =>
            Math.min(maxQuantity || 1, value + 1),
        );
    };

    const addToCart = () => {
        if (!activeVariantId || maxQuantity <= 0) {
            return;
        }

        add.mutate(
            {
                productVariantId: activeVariantId,
                quantity,
            },
            {
                onSuccess: () => {
                    setAdded(true);
                },
            },
        );
    };

    return (
        <Box sx={{ py: { xs: 3, md: 5 } }}>
            <Container maxWidth="xl">
                <Stack spacing={3}>
                    <Button
                        component={Link}
                        to="/products"
                        startIcon={<ArrowBack />}
                        sx={{ alignSelf: 'flex-start' }}
                    >
                        Back to products
                    </Button>

                    <Card
                        sx={{
                            p: { xs: 2, md: 4 },
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
                                    src={imageUrl}
                                    alt={product.name}
                                    sx={{
                                        width: '100%',
                                        height: {
                                            xs: 350,
                                            md: 520,
                                        },
                                        objectFit: 'contain',
                                        borderRadius: 3,
                                        bgcolor:
                                            'background.default',
                                    }}
                                    onError={(event) => {
                                        const element =
                                            event.currentTarget as HTMLImageElement;

                                        if (
                                            !element.src.endsWith(
                                                '/placeholder.jpg',
                                            )
                                        ) {
                                            element.src =
                                                '/placeholder.jpg';
                                        }
                                    }}
                                />

                                {images.length > 1 && (
                                    <Stack
                                        direction="row"
                                        spacing={1}
                                        sx={{
                                            mt: 2,
                                            overflowX: 'auto',
                                        }}
                                    >
                                        {images.map(
                                            (image, index) => (
                                                <IconButton
                                                    key={image.id}
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
                                                            imageIndex ===
                                                                index
                                                                ? 'primary.main'
                                                                : 'transparent',
                                                        borderRadius: 2,
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
                                                            width: 70,
                                                            height: 70,
                                                            objectFit:
                                                                'cover',
                                                            borderRadius: 1.5,
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
                                                fontWeight: 600,
                                            }}
                                        >
                                            {product.brandName}
                                        </Typography>
                                    )}

                                    <Typography
                                        variant="h3"
                                        component="h1"
                                        sx={{
                                            fontWeight: 900,
                                            lineHeight: 1.2,
                                        }}
                                    >
                                        {product.name}
                                    </Typography>

                                    <Typography
                                        variant="body2"
                                        color="text.secondary"
                                    >
                                        SKU: {product.sku}
                                    </Typography>

                                    <Divider />

                                    <Box>
                                        {product.comparePrice &&
                                            product.comparePrice >
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
                                                        product.comparePrice,
                                                    )}{' '}
                                                    {product.currency}
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
                                            {formatPrice(price)}{' '}
                                            {product.currency}
                                        </Typography>
                                    </Box>

                                    {availableVariants.length > 0 && (
                                        <Box>
                                            <Typography
                                                variant="h6"
                                                sx={{
                                                    fontWeight: 800,
                                                    mb: 1,
                                                }}
                                            >
                                                Choose variant
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
                                                    (variant) => (
                                                        <Button
                                                            key={
                                                                variant.id
                                                            }
                                                            variant={
                                                                activeVariantId ===
                                                                    variant.id
                                                                    ? 'contained'
                                                                    : 'outlined'
                                                            }
                                                            onClick={() => {
                                                                setVariantId(
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
                                                                borderRadius: 2,
                                                            }}
                                                        >
                                                            {[
                                                                variant.color,
                                                                variant.size,
                                                                variant.sku,
                                                            ]
                                                                .filter(
                                                                    Boolean,
                                                                )
                                                                .join(
                                                                    ' • ',
                                                                )}
                                                        </Button>
                                                    ),
                                                )}
                                            </Stack>
                                        </Box>
                                    )}

                                    {activeVariant && (
                                        <Alert
                                            severity="success"
                                            icon={
                                                <CheckCircleOutlined />
                                            }
                                        >
                                            In stock:{' '}
                                            {
                                                activeVariant.stockQuantity
                                            }
                                        </Alert>
                                    )}

                                    {availableVariants.length === 0 && (
                                        <Alert severity="warning">
                                            This product is currently
                                            out of stock.
                                        </Alert>
                                    )}

                                    <Box>
                                        <Typography
                                            variant="h6"
                                            sx={{
                                                fontWeight: 800,
                                                mb: 1,
                                            }}
                                        >
                                            Quantity
                                        </Typography>

                                        <Stack
                                            direction="row"
                                            spacing={1}
                                            sx={{
                                                alignItems: 'center',
                                            }}
                                        >
                                            <IconButton
                                                onClick={decrease}
                                                disabled={
                                                    quantity <= 1
                                                }
                                            >
                                                <Remove />
                                            </IconButton>

                                            <TextField
                                                value={quantity}
                                                onChange={(event) => {
                                                    const value =
                                                        Number(
                                                            event.target
                                                                .value,
                                                        );

                                                    if (
                                                        !Number.isFinite(
                                                            value,
                                                        )
                                                    ) {
                                                        return;
                                                    }

                                                    setQuantity(
                                                        Math.min(
                                                            Math.max(
                                                                1,
                                                                Math.floor(
                                                                    value,
                                                                ),
                                                            ),
                                                            maxQuantity ||
                                                            1,
                                                        ),
                                                    );
                                                }}
                                                sx={{
                                                    width: 90,
                                                    '& input': {
                                                        textAlign:
                                                            'center',
                                                    },
                                                }}
                                                slotProps={{
                                                    htmlInput: {
                                                        min: 1,
                                                        max:
                                                            maxQuantity ||
                                                            1,
                                                    },
                                                }}
                                            />

                                            <IconButton
                                                onClick={increase}
                                                disabled={
                                                    maxQuantity <= 0 ||
                                                    quantity >=
                                                    maxQuantity
                                                }
                                            >
                                                <Add />
                                            </IconButton>
                                        </Stack>
                                    </Box>

                                    <Button
                                        variant="contained"
                                        size="large"
                                        fullWidth
                                        startIcon={
                                            added ? (
                                                <CheckCircleOutlined />
                                            ) : (
                                                <ShoppingCartOutlined />
                                            )
                                        }
                                        disabled={
                                            !activeVariantId ||
                                            maxQuantity <= 0 ||
                                            add.isPending
                                        }
                                        onClick={addToCart}
                                        sx={{
                                            py: 1.5,
                                            borderRadius: 2.5,
                                            fontWeight: 800,
                                        }}
                                    >
                                        {add.isPending
                                            ? 'Adding...'
                                            : added
                                                ? 'Added to cart'
                                                : 'Add to cart'}
                                    </Button>

                                    {added && (
                                        <Button
                                            component={Link}
                                            to="/cart"
                                            variant="outlined"
                                            fullWidth
                                            sx={{
                                                borderRadius: 2.5,
                                                fontWeight: 700,
                                            }}
                                        >
                                            View cart
                                        </Button>
                                    )}

                                    {add.isError && (
                                        <Alert severity="error">
                                            Could not add the product
                                            to the cart.
                                        </Alert>
                                    )}

                                    {product.shortDescription && (
                                        <Box>
                                            <Typography
                                                variant="h6"
                                                sx={{
                                                    fontWeight: 800,
                                                    mb: 1,
                                                }}
                                            >
                                                Description
                                            </Typography>

                                            <Typography
                                                color="text.secondary"
                                                sx={{
                                                    lineHeight: 1.9,
                                                    whiteSpace:
                                                        'pre-line',
                                                }}
                                            >
                                                {
                                                    product.shortDescription
                                                }
                                            </Typography>
                                        </Box>
                                    )}

                                    {product.description && (
                                        <Box>
                                            <Divider
                                                sx={{ my: 2 }}
                                            />

                                            <Typography
                                                color="text.secondary"
                                                sx={{
                                                    lineHeight: 1.9,
                                                    whiteSpace:
                                                        'pre-line',
                                                }}
                                            >
                                                {product.description}
                                            </Typography>
                                        </Box>
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

