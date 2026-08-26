import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent, CardMedia, Typography, Box, Button, Chip, IconButton, Rating, Stack, Tooltip, } from '@mui/material';
import { ShoppingCartOutlined, FavoriteBorder, Favorite, VisibilityOutlined, } from '@mui/icons-material';
const ProductCard = ({ product }) => {
    const [favorite, setFavorite] = useState(false);
    const price = product.finalPrice ?? product.price;
    const hasDiscount = product.comparePrice !== undefined &&
        product.comparePrice !== null &&
        product.comparePrice > price;
    const discountPercentage = hasDiscount && product.comparePrice
        ? Math.round(((product.comparePrice - price) / product.comparePrice) * 100)
        : product.discountPercentage ?? 0;
    const formatPrice = (value) => {
        return new Intl.NumberFormat('fa-IR').format(value);
    };
    const image = product.images && product.images.length > 0
        ? product.images[0]
        : '/placeholder.jpg';
    return (_jsxs(Card, { sx: {
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
        }, children: [discountPercentage > 0 && (_jsx(Chip, { label: `${discountPercentage}% تخفیف`, color: "error", size: "small", sx: {
                    position: 'absolute',
                    top: 12,
                    right: 12,
                    zIndex: 3,
                    fontWeight: 700,
                } })), _jsx(Tooltip, { title: favorite ? 'حذف از علاقه‌مندی‌ها' : 'افزودن به علاقه‌مندی‌ها', children: _jsx(IconButton, { onClick: () => setFavorite((value) => !value), sx: {
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        zIndex: 3,
                        backgroundColor: 'rgba(255,255,255,0.92)',
                        '&:hover': {
                            backgroundColor: '#fff',
                        },
                    }, children: favorite ? (_jsx(Favorite, { color: "error" })) : (_jsx(FavoriteBorder, {})) }) }), _jsx(Box, { component: Link, to: `/products/${product.id}`, sx: {
                    display: 'block',
                    textDecoration: 'none',
                    overflow: 'hidden',
                    backgroundColor: '#f7f7f7',
                }, children: _jsx(CardMedia, { component: "img", image: image, alt: product.name, sx: {
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
                    }, onError: (event) => {
                        event.currentTarget.src = '/placeholder.jpg';
                    } }) }), _jsxs(CardContent, { sx: {
                    display: 'flex',
                    flexDirection: 'column',
                    flexGrow: 1,
                    p: 2,
                    textAlign: 'right',
                }, children: [product.brandName && (_jsx(Typography, { variant: "caption", color: "text.secondary", sx: {
                            mb: 0.5,
                            fontWeight: 500,
                        }, children: product.brandName })), _jsx(Typography, { component: Link, to: `/products/${product.id}`, variant: "subtitle1", sx: {
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
                        }, children: product.name }), _jsxs(Stack, { direction: "row", spacing: 1, alignItems: "center", sx: {
                            mt: 1,
                            direction: 'ltr',
                        }, children: [_jsx(Rating, { value: 4.5, precision: 0.5, size: "small", readOnly: true }), _jsx(Typography, { variant: "caption", color: "text.secondary", children: "4.5" })] }), _jsx(Box, { sx: { flexGrow: 1 } }), _jsxs(Box, { sx: { mt: 2 }, children: [hasDiscount && product.comparePrice && (_jsxs(Typography, { variant: "body2", color: "text.secondary", sx: {
                                    textDecoration: 'line-through',
                                    mb: 0.3,
                                }, children: [formatPrice(product.comparePrice), " \u062A\u0648\u0645\u0627\u0646"] })), _jsxs(Typography, { variant: "h6", color: "primary", sx: {
                                    fontWeight: 800,
                                    fontSize: '1.15rem',
                                }, children: [formatPrice(price), " \u062A\u0648\u0645\u0627\u0646"] })] }), _jsx(Box, { sx: { mt: 1 }, children: _jsx(Typography, { variant: "caption", sx: {
                                color: product.isInStock
                                    ? 'success.main'
                                    : 'error.main',
                                fontWeight: 600,
                            }, children: product.isInStock
                                ? '● موجود در انبار'
                                : '● ناموجود' }) }), _jsxs(Stack, { direction: "row", spacing: 1, sx: {
                            mt: 2,
                        }, children: [_jsx(Button, { component: Link, to: `/products/${product.id}`, variant: "outlined", color: "primary", startIcon: _jsx(VisibilityOutlined, {}), sx: {
                                    minWidth: 48,
                                    borderRadius: 2,
                                    flex: 1,
                                    fontWeight: 600,
                                }, children: "\u0645\u0634\u0627\u0647\u062F\u0647" }), _jsx(Button, { variant: "contained", disabled: !product.isInStock, startIcon: _jsx(ShoppingCartOutlined, {}), sx: {
                                    borderRadius: 2,
                                    flex: 1.5,
                                    fontWeight: 700,
                                }, onClick: () => {
                                    console.log('Add product to cart:', product.id);
                                }, children: "\u0627\u0641\u0632\u0648\u062F\u0646 \u0628\u0647 \u0633\u0628\u062F" })] })] })] }));
};
export default ProductCard;
