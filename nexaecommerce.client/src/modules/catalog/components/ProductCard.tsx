import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
    Card,
    CardContent,
    CardMedia,
    Typography,
    Box,
    Button,
    Chip,
    IconButton,
    Rating,
    Stack,
    Tooltip,
} from '@mui/material';

import {
    ShoppingCartOutlined,
    FavoriteBorder,
    Favorite,
    VisibilityOutlined,
} from '@mui/icons-material';

import type { Product } from '../types/product.types';

interface ProductCardProps {
    product: Product;
}

const ProductCard: React.FC<ProductCardProps> = ({ product }) => {
    const [favorite, setFavorite] = useState(false);

    const price = product.finalPrice ?? product.price;

    const hasDiscount =
        product.comparePrice !== undefined &&
        product.comparePrice !== null &&
        product.comparePrice > price;

    const discountPercentage =
        hasDiscount && product.comparePrice
            ? Math.round(
                ((product.comparePrice - price) / product.comparePrice) * 100
            )
            : product.discountPercentage ?? 0;

    const formatPrice = (value: number) => {
        return new Intl.NumberFormat('fa-IR').format(value);
    };

    const image =
        product.images && product.images.length > 0
            ? product.images.find((item) => item.isMain)?.imageUrl ??
            product.images[0]?.imageUrl ??
            '/placeholder.jpg'
            : '/placeholder.jpg';

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
                borderColor: 'divider',
                backgroundColor: 'background.paper',
                transition: 'all 0.3s ease',

                '&:hover': {
                    transform: 'translateY(-6px)',
                    boxShadow: '0 14px 35px rgba(0,0,0,0.12)',
                    borderColor: 'primary.main',
                },
            }}
        >
            {/* Discount */}
            {discountPercentage > 0 && (
                <Chip
                    label={`${discountPercentage}% تخفیف`}
                    color="error"
                    size="small"
                    sx={{
                        position: 'absolute',
                        top: 12,
                        right: 12,
                        zIndex: 3,
                        fontWeight: 700,
                    }}
                />
            )}

            {/* Favorite */}
            <Tooltip title={favorite ? 'حذف از علاقه‌مندی‌ها' : 'افزودن به علاقه‌مندی‌ها'}>
                <IconButton
                    onClick={() => setFavorite((value) => !value)}
                    sx={{
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        zIndex: 3,
                        backgroundColor: 'rgba(255,255,255,0.92)',

                        '&:hover': {
                            backgroundColor: '#fff',
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

            {/* Image */}
            <Box
                component={Link}
                to={`/products/${product.id}`}
                sx={{
                    display: 'block',
                    textDecoration: 'none',
                    overflow: 'hidden',
                    backgroundColor: '#f7f7f7',
                }}
            >
                <CardMedia
                    component="img"
                    image={image}
                    alt={product.name}
                    sx={{
                        height: {
                            xs: 220,
                            sm: 230,
                            md: 240,
                        },
                        objectFit: 'cover',
                        transition: 'transform 0.5s ease',

                        '.MuiCard-root:hover &': {
                            transform: 'scale(1.05)',
                        },
                    }}
                    onError={(event) => {
                        event.currentTarget.src = '/placeholder.jpg';
                    }}
                />
            </Box>

            <CardContent
                sx={{
                    display: 'flex',
                    flexDirection: 'column',
                    flexGrow: 1,
                    p: 2,
                    textAlign: 'right',
                }}
            >
                {/* Brand */}
                {product.brandName && (
                    <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{
                            mb: 0.5,
                            fontWeight: 500,
                        }}
                    >
                        {product.brandName}
                    </Typography>
                )}

                {/* Product name */}
                <Typography
                    component={Link}
                    to={`/products/${product.id}`}
                    variant="subtitle1"
                    sx={{
                        color: 'text.primary',
                        textDecoration: 'none',
                        fontWeight: 700,
                        lineHeight: 1.8,
                        minHeight: 58,

                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        overflow: 'hidden',

                        '&:hover': {
                            color: 'primary.main',
                        },
                    }}
                >
                    {product.name}
                </Typography>

                {/* Rating */}
                <Stack
                    direction="row"
                    spacing={1}
                    sx={{
                        mt: 1,
                        direction: 'ltr',
                        alignItems: 'center',
                    }}
                
                    component="div">
                    <Rating
                        value={4.5}
                        precision={0.5}
                        size="small"
                        readOnly
                    />

                    <Typography
                        variant="caption"
                        color="text.secondary"
                    >
                        4.5
                    </Typography>
                </Stack>

                <Box sx={{ flexGrow: 1 }} />

                {/* Price */}
                <Box sx={{ mt: 2 }}>
                    {hasDiscount && product.comparePrice && (
                        <Typography
                            variant="body2"
                            color="text.secondary"
                            sx={{
                                textDecoration: 'line-through',
                                mb: 0.3,
                            }}
                        >
                            {formatPrice(product.comparePrice)} تومان
                        </Typography>
                    )}

                    <Typography
                        variant="h6"
                        color="primary"
                        sx={{
                            fontWeight: 800,
                            fontSize: '1.15rem',
                        }}
                    >
                        {formatPrice(price)} تومان
                    </Typography>
                </Box>

                {/* Stock */}
                <Box sx={{ mt: 1 }}>
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
                            ? '● موجود در انبار'
                            : '● ناموجود'}
                    </Typography>
                </Box>

                {/* Actions */}
                <Stack
                    direction="row"
                    spacing={1}
                    sx={{
                        mt: 2,
                    }}
                
                    component="div">
                    <Button
                        component={Link}
                        to={`/products/${product.id}`}
                        variant="outlined"
                        color="primary"
                        startIcon={<VisibilityOutlined />}
                        sx={{
                            minWidth: 48,
                            borderRadius: 2,
                            flex: 1,
                            fontWeight: 600,
                        }}
                    >
                        مشاهده
                    </Button>

                    <Button
                        variant="contained"
                        disabled={!product.isInStock}
                        startIcon={<ShoppingCartOutlined />}
                        sx={{
                            borderRadius: 2,
                            flex: 1.5,
                            fontWeight: 700,
                        }}
                        onClick={() => {
                            console.log(
                                'Add product to cart:',
                                product.id
                            );
                        }}
                    >
                        افزودن به سبد
                    </Button>
                </Stack>
            </CardContent>
        </Card>
    );
};

export default ProductCard;