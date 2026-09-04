import { useState } from 'react';
import { Link } from 'react-router-dom';

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
} from '@mui/icons-material';

import type { ProductListItem } from '../api/products';

interface ProductCardProps {
    product: ProductListItem;
}

export default function ProductCard({ product }: ProductCardProps) {
    const [favorite, setFavorite] = useState(false);

    const price = product.finalPrice ?? product.price;

    const hasDiscount =
        product.comparePrice != null &&
        product.comparePrice > price;

    const discountPercentage =
        product.discountPercentage > 0
            ? Math.round(product.discountPercentage)
            : hasDiscount && product.comparePrice
                ? Math.round(
                    ((product.comparePrice - price) /
                        product.comparePrice) *
                    100,
                )
                : 0;

    const image = product.mainImage || '/placeholder.jpg';

    const formatPrice = (value: number) =>
        new Intl.NumberFormat('en-US').format(value);

    return (
        <Card
            sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                position: 'relative',
                overflow: 'hidden',
                borderRadius: 3,
                transition: 'transform .2s ease, box-shadow .2s ease',
                '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 6,
                },
            }}
        >
            {discountPercentage > 0 && (
                <Chip
                    label={`${discountPercentage}% OFF`}
                    color="error"
                    size="small"
                    sx={{
                        position: 'absolute',
                        top: 12,
                        right: 12,
                        zIndex: 2,
                        fontWeight: 700,
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
                        setFavorite((value) => !value)
                    }
                    sx={{
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        zIndex: 2,
                        bgcolor: 'rgba(255,255,255,.9)',
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
                to={`/products/${product.id}`}
                sx={{
                    display: 'block',
                    overflow: 'hidden',
                    bgcolor: 'background.default',
                }}
            >
                <Box
                    component="img"
                    src={image}
                    alt={product.name}
                    sx={{
                        display: 'block',
                        width: '100%',
                        height: 240,
                        objectFit: 'cover',
                        transition: 'transform .3s ease',
                    }}
                    onError={(event) => {
                        const element =
                            event.currentTarget as HTMLImageElement;

                        if (!element.src.endsWith('/placeholder.jpg')) {
                            element.src = '/placeholder.jpg';
                        }
                    }}
                />
            </Box>

            <CardContent
                sx={{
                    display: 'flex',
                    flexDirection: 'column',
                    flexGrow: 1,
                    gap: 1,
                }}
            >
                {product.brandName && (
                    <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{
                            fontWeight: 600,
                        }}
                    >
                        {product.brandName}
                    </Typography>
                )}

                <Typography
                    component={Link}
                    to={`/products/${product.id}`}
                    variant="subtitle1"
                    sx={{
                        color: 'text.primary',
                        textDecoration: 'none',
                        fontWeight: 700,
                        minHeight: 52,
                        lineHeight: 1.6,
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        overflow: 'hidden',
                    }}
                >
                    {product.name}
                </Typography>

                <Stack
                    direction="row"
                    spacing={1}
                    sx={{
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

                <Box sx={{ flexGrow: 1 }} />

                {hasDiscount && product.comparePrice != null && (
                    <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{
                            textDecoration: 'line-through',
                        }}
                    >
                        {formatPrice(product.comparePrice)}{' '}
                        {product.currency}
                    </Typography>
                )}

                <Typography
                    variant="h6"
                    color="primary"
                    sx={{
                        fontWeight: 800,
                    }}
                >
                    {formatPrice(price)} {product.currency}
                </Typography>

                <Typography
                    variant="caption"
                    sx={{
                        color: product.isInStock
                            ? 'success.main'
                            : 'error.main',
                        fontWeight: 600,
                    }}
                >
                    {product.isInStock
                        ? '● In stock'
                        : '● Out of stock'}
                </Typography>

                <Button
                    component={Link}
                    to={`/products/${product.id}`}
                    variant="contained"
                    startIcon={<ShoppingCartOutlined />}
                    disabled={!product.isInStock}
                    sx={{
                        mt: 1,
                        borderRadius: 2,
                        fontWeight: 700,
                    }}
                >
                    View & Buy
                </Button>
            </CardContent>
        </Card>
    );
}