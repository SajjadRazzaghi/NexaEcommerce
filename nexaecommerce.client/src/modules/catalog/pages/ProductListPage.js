import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector, } from 'react-redux';
import { fetchProducts, fetchFeaturedProducts, } from '../store/productSlice';
import ProductCard from '../components/ProductCard';
import { Alert, Box, Chip, CircularProgress, Container, FormControl, Grid, InputAdornment, InputLabel, MenuItem, Pagination, Select, Stack, Tab, Tabs, TextField, Typography, useMediaQuery, useTheme, } from '@mui/material';
import { Search, Tune, LocalFireDepartment, ShoppingBagOutlined, } from '@mui/icons-material';
const ProductListPage = () => {
    const dispatch = useDispatch();
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
    const { products, featuredProducts, loading, error, total, } = useSelector((state) => state.products);
    const [activeTab, setActiveTab] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [sortBy, setSortBy] = useState('newest');
    const [page, setPage] = useState(1);
    const pageSize = 20;
    /*
     * Load products
     */
    useEffect(() => {
        dispatch(fetchProducts({
            page,
            sort: sortBy,
            search: searchTerm,
        }));
        dispatch(fetchFeaturedProducts(6));
    }, [
        dispatch,
        page,
        sortBy,
        searchTerm,
    ]);
    /*
     * Search
     */
    const handleSearch = (event) => {
        setSearchTerm(event.target.value);
        setPage(1);
    };
    /*
     * Tab
     */
    const handleTabChange = (_event, value) => {
        setActiveTab(value);
        setPage(1);
    };
    /*
     * Products
     */
    const displayProducts = activeTab === 0
        ? products
        : featuredProducts;
    const totalPages = Math.ceil((total || 0) / pageSize);
    /*
     * Loading
     */
    if (loading) {
        return (_jsx(Box, { sx: {
                minHeight: '65vh',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
            }, children: _jsxs(Stack, { spacing: 2, alignItems: "center", children: [_jsx(CircularProgress, { size: 48, thickness: 4 }), _jsx(Typography, { color: "text.secondary", children: "\u062F\u0631 \u062D\u0627\u0644 \u062F\u0631\u06CC\u0627\u0641\u062A \u0645\u062D\u0635\u0648\u0644\u0627\u062A..." })] }) }));
    }
    /*
     * Error
     */
    if (error) {
        return (_jsx(Container, { maxWidth: "lg", sx: { py: 5 }, children: _jsx(Alert, { severity: "error", sx: {
                    borderRadius: 3,
                }, children: error }) }));
    }
    return (_jsxs(Box, { sx: {
            minHeight: '100vh',
            backgroundColor: '#fafafa',
            direction: 'rtl',
        }, children: [_jsx(Box, { sx: {
                    background: 'linear-gradient(135deg, #0d47a1 0%, #1976d2 55%, #42a5f5 100%)',
                    color: '#fff',
                    py: {
                        xs: 4,
                        md: 6,
                    },
                    mb: 4,
                }, children: _jsx(Container, { maxWidth: "xl", children: _jsxs(Stack, { direction: {
                            xs: 'column',
                            md: 'row',
                        }, spacing: 3, alignItems: {
                            xs: 'flex-start',
                            md: 'center',
                        }, justifyContent: "space-between", children: [_jsxs(Box, { children: [_jsxs(Stack, { direction: "row", spacing: 1, alignItems: "center", sx: { mb: 1 }, children: [_jsx(ShoppingBagOutlined, {}), _jsx(Typography, { variant: "overline", sx: {
                                                    fontWeight: 700,
                                                    letterSpacing: 1,
                                                }, children: "\u0641\u0631\u0648\u0634\u06AF\u0627\u0647 \u0622\u0646\u0644\u0627\u06CC\u0646" })] }), _jsx(Typography, { variant: "h3", component: "h1", sx: {
                                            fontWeight: 900,
                                            fontSize: {
                                                xs: '2rem',
                                                md: '3rem',
                                            },
                                        }, children: "\u0645\u062D\u0635\u0648\u0644\u0627\u062A NexaEcommerce" }), _jsx(Typography, { variant: "body1", sx: {
                                            mt: 1,
                                            opacity: 0.9,
                                            maxWidth: 650,
                                        }, children: "\u062C\u062F\u06CC\u062F\u062A\u0631\u06CC\u0646 \u0645\u062D\u0635\u0648\u0644\u0627\u062A \u0631\u0627 \u0628\u0627 \u0628\u0647\u062A\u0631\u06CC\u0646 \u0642\u06CC\u0645\u062A \u0645\u0634\u0627\u0647\u062F\u0647 \u0648 \u0627\u0646\u062A\u062E\u0627\u0628 \u06A9\u0646\u06CC\u062F." })] }), _jsxs(Box, { sx: {
                                    minWidth: {
                                        xs: '100%',
                                        md: 220,
                                    },
                                    p: 2.5,
                                    borderRadius: 3,
                                    backgroundColor: 'rgba(255,255,255,0.12)',
                                    backdropFilter: 'blur(10px)',
                                }, children: [_jsx(Typography, { variant: "body2", sx: { opacity: 0.85 }, children: "\u062A\u0639\u062F\u0627\u062F \u0645\u062D\u0635\u0648\u0644\u0627\u062A" }), _jsx(Typography, { variant: "h4", sx: {
                                            fontWeight: 900,
                                            mt: 0.5,
                                        }, children: total || 0 })] })] }) }) }), _jsxs(Container, { maxWidth: "xl", sx: {
                    pb: 7,
                }, children: [_jsx(Box, { sx: {
                            backgroundColor: '#fff',
                            borderRadius: 3,
                            p: {
                                xs: 2,
                                md: 2.5,
                            },
                            mb: 3,
                            border: '1px solid',
                            borderColor: 'divider',
                            boxShadow: '0 4px 18px rgba(0,0,0,0.04)',
                        }, children: _jsxs(Stack, { direction: {
                                xs: 'column',
                                md: 'row',
                            }, spacing: 2, children: [_jsx(TextField, { fullWidth: true, size: "small", value: searchTerm, onChange: handleSearch, placeholder: "\u062C\u0633\u062A\u062C\u0648\u06CC \u0645\u062D\u0635\u0648\u0644...", InputProps: {
                                        startAdornment: (_jsx(InputAdornment, { position: "start", children: _jsx(Search, {}) })),
                                    }, sx: {
                                        '& .MuiOutlinedInput-root': {
                                            borderRadius: 2,
                                        },
                                    } }), _jsxs(FormControl, { size: "small", sx: {
                                        minWidth: {
                                            xs: '100%',
                                            md: 190,
                                        },
                                    }, children: [_jsx(InputLabel, { children: "\u0645\u0631\u062A\u0628\u200C\u0633\u0627\u0632\u06CC" }), _jsxs(Select, { value: sortBy, label: "\u0645\u0631\u062A\u0628\u200C\u0633\u0627\u0632\u06CC", onChange: (event) => {
                                                setSortBy(event.target.value);
                                                setPage(1);
                                            }, children: [_jsx(MenuItem, { value: "newest", children: "\u062C\u062F\u06CC\u062F\u062A\u0631\u06CC\u0646" }), _jsx(MenuItem, { value: "price_asc", children: "\u0627\u0631\u0632\u0627\u0646\u200C\u062A\u0631\u06CC\u0646" }), _jsx(MenuItem, { value: "price_desc", children: "\u06AF\u0631\u0627\u0646\u200C\u062A\u0631\u06CC\u0646" }), _jsx(MenuItem, { value: "name", children: "\u0628\u0631 \u0627\u0633\u0627\u0633 \u0646\u0627\u0645" })] })] }), _jsx(Chip, { icon: _jsx(Tune, {}), label: "\u0641\u06CC\u0644\u062A\u0631\u0647\u0627", variant: "outlined", sx: {
                                        height: 40,
                                        borderRadius: 2,
                                        px: 1,
                                        alignSelf: {
                                            xs: 'flex-start',
                                            md: 'center',
                                        },
                                    } })] }) }), _jsx(Box, { sx: {
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            mb: 3,
                        }, children: _jsxs(Tabs, { value: activeTab, onChange: handleTabChange, variant: isMobile
                                ? 'fullWidth'
                                : 'standard', children: [_jsx(Tab, { icon: _jsx(ShoppingBagOutlined, {}), iconPosition: "start", label: `همه محصولات (${total || 0})` }), _jsx(Tab, { icon: _jsx(LocalFireDepartment, {}), iconPosition: "start", label: `پیشنهاد ویژه (${featuredProducts.length})` })] }) }), _jsxs(Stack, { direction: "row", justifyContent: "space-between", alignItems: "center", sx: { mb: 2 }, children: [_jsxs(Typography, { variant: "body2", color: "text.secondary", children: ["\u0646\u0645\u0627\u06CC\u0634 ", displayProducts.length, " \u0645\u062D\u0635\u0648\u0644"] }), searchTerm && (_jsx(Chip, { label: `جستجو: ${searchTerm}`, size: "small", onDelete: () => setSearchTerm('') }))] }), displayProducts.length === 0 ? (_jsxs(Box, { sx: {
                            backgroundColor: '#fff',
                            borderRadius: 3,
                            py: 10,
                            textAlign: 'center',
                            border: '1px solid',
                            borderColor: 'divider',
                        }, children: [_jsx(Search, { sx: {
                                    fontSize: 60,
                                    color: 'text.disabled',
                                    mb: 2,
                                } }), _jsx(Typography, { variant: "h5", fontWeight: 700, gutterBottom: true, children: "\u0645\u062D\u0635\u0648\u0644\u06CC \u067E\u06CC\u062F\u0627 \u0646\u0634\u062F" }), _jsx(Typography, { color: "text.secondary", children: "\u0639\u0628\u0627\u0631\u062A \u062C\u0633\u062A\u062C\u0648 \u06CC\u0627 \u0641\u06CC\u0644\u062A\u0631\u0647\u0627\u06CC \u062E\u0648\u062F \u0631\u0627 \u062A\u063A\u06CC\u06CC\u0631 \u062F\u0647\u06CC\u062F." })] })) : (_jsx(Grid, { container: true, spacing: {
                            xs: 2,
                            sm: 2.5,
                            md: 3,
                        }, children: displayProducts.map((product) => (_jsx(Grid, { item: true, xs: 12, sm: 6, md: 4, lg: 3, children: _jsx(ProductCard, { product: product }) }, product.id))) })), activeTab === 0 &&
                        totalPages > 1 && (_jsx(Box, { sx: {
                            display: 'flex',
                            justifyContent: 'center',
                            mt: 6,
                        }, children: _jsx(Pagination, { count: totalPages, page: page, onChange: (_, value) => setPage(value), color: "primary", size: isMobile
                                ? 'small'
                                : 'medium', showFirstButton: true, showLastButton: true }) }))] })] }));
};
export default ProductListPage;
