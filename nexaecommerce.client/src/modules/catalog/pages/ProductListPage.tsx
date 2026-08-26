import React, { useEffect, useState } from 'react';

import {
    useDispatch,
    useSelector,
} from 'react-redux';

import type { RootState } from '@/store';

import {
    fetchProducts,
    fetchFeaturedProducts,
} from '../store/productSlice';

import ProductCard from '../components/ProductCard';

import {
    Alert,
    Box,
    Chip,
    CircularProgress,
    Container,
    FormControl,
    Grid,
    InputAdornment,
    InputLabel,
    MenuItem,
    Pagination,
    Select,
    Stack,
    Tab,
    Tabs,
    TextField,
    Typography,
    useMediaQuery,
    useTheme,
} from '@mui/material';

import {
    Search,
    Tune,
    LocalFireDepartment,
    ShoppingBagOutlined,
} from '@mui/icons-material';

const ProductListPage: React.FC = () => {
    const dispatch = useDispatch();

    const theme = useTheme();

    const isMobile = useMediaQuery(
        theme.breakpoints.down('sm')
    );

    const {
        products,
        featuredProducts,
        loading,
        error,
        total,
    } = useSelector(
        (state: RootState) => state.products
    );

    const [activeTab, setActiveTab] = useState(0);

    const [searchTerm, setSearchTerm] = useState('');

    const [sortBy, setSortBy] = useState('newest');

    const [page, setPage] = useState(1);

    const pageSize = 20;

    /*
     * Load products
     */
    useEffect(() => {
        dispatch(
            fetchProducts({
                page,
                sort: sortBy,
                search: searchTerm,
            }) as any
        );

        dispatch(
            fetchFeaturedProducts(6) as any
        );
    }, [
        dispatch,
        page,
        sortBy,
        searchTerm,
    ]);

    /*
     * Search
     */
    const handleSearch = (
        event: React.ChangeEvent<HTMLInputElement>
    ) => {
        setSearchTerm(event.target.value);
        setPage(1);
    };

    /*
     * Tab
     */
    const handleTabChange = (
        _event: React.SyntheticEvent,
        value: number
    ) => {
        setActiveTab(value);
        setPage(1);
    };

    /*
     * Products
     */
    const displayProducts =
        activeTab === 0
            ? products
            : featuredProducts;

    const totalPages =
        Math.ceil((total || 0) / pageSize);

    /*
     * Loading
     */
    if (loading) {
        return (
            <Box
                sx={{
                    minHeight: '65vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                }}
            >
                <Stack
                    spacing={2}
                    sx={{ alignItems: 'center' }}
                    component="div">
                    <CircularProgress
                        size={48}
                        thickness={4}
                    />

                    <Typography
                        color="text.secondary"
                    >
                        در حال دریافت محصولات...
                    </Typography>
                </Stack>
            </Box>
        );
    }

    /*
     * Error
     */
    if (error) {
        return (
            <Container
                maxWidth="lg"
                sx={{ py: 5 }}
            >
                <Alert
                    severity="error"
                    sx={{
                        borderRadius: 3,
                    }}
                >
                    {error}
                </Alert>
            </Container>
        );
    }

    return (
        <Box
            sx={{
                minHeight: '100vh',
                backgroundColor: '#fafafa',
                direction: 'rtl',
            }}
        >
            {/* =========================
                Hero Header
            ========================= */}
            <Box
                sx={{
                    background:
                        'linear-gradient(135deg, #0d47a1 0%, #1976d2 55%, #42a5f5 100%)',
                    color: '#fff',
                    py: {
                        xs: 4,
                        md: 6,
                    },
                    mb: 4,
                }}
            >
                <Container maxWidth="xl">
                    <Stack
                        direction={{
                            xs: 'column',
                            md: 'row',
                        }}
                        spacing={3}
                        sx={{
                            alignItems: { xs: 'flex-start', md: 'center' },
                            justifyContent: 'space-between',
                        }}
                    component="div">
                        <Box>
                            <Stack
                                direction="row"
                                spacing={1}
                                sx={{ mb: 1, alignItems: 'center' }}
                            
                    component="div">
                                <ShoppingBagOutlined />

                                <Typography
                                    variant="overline"
                                    sx={{
                                        fontWeight: 700,
                                        letterSpacing: 1,
                                    }}
                                >
                                    فروشگاه آنلاین
                                </Typography>
                            </Stack>

                            <Typography
                                variant="h3"
                                component="h1"
                                sx={{
                                    fontWeight: 900,
                                    fontSize: {
                                        xs: '2rem',
                                        md: '3rem',
                                    },
                                }}
                            >
                                محصولات NexaEcommerce
                            </Typography>

                            <Typography
                                variant="body1"
                                sx={{
                                    mt: 1,
                                    opacity: 0.9,
                                    maxWidth: 650,
                                }}
                            >
                                جدیدترین محصولات را با بهترین
                                قیمت مشاهده و انتخاب کنید.
                            </Typography>
                        </Box>

                        <Box
                            sx={{
                                minWidth: {
                                    xs: '100%',
                                    md: 220,
                                },
                                p: 2.5,
                                borderRadius: 3,
                                backgroundColor:
                                    'rgba(255,255,255,0.12)',
                                backdropFilter: 'blur(10px)',
                            }}
                        >
                            <Typography
                                variant="body2"
                                sx={{ opacity: 0.85 }}
                            >
                                تعداد محصولات
                            </Typography>

                            <Typography
                                variant="h4"
                                sx={{
                                    fontWeight: 900,
                                    mt: 0.5,
                                }}
                            >
                                {total || 0}
                            </Typography>
                        </Box>
                    </Stack>
                </Container>
            </Box>

            <Container
                maxWidth="xl"
                sx={{
                    pb: 7,
                }}
            >
                {/* =========================
                    Search / Sort
                ========================= */}
                <Box
                    sx={{
                        backgroundColor: '#fff',
                        borderRadius: 3,
                        p: {
                            xs: 2,
                            md: 2.5,
                        },
                        mb: 3,
                        border: '1px solid',
                        borderColor: 'divider',
                        boxShadow:
                            '0 4px 18px rgba(0,0,0,0.04)',
                    }}
                >
                    <Stack
                        direction={{
                            xs: 'column',
                            md: 'row',
                        }}
                        spacing={2}
                    
                    component="div">
                        <TextField
                            fullWidth
                            size="small"
                            value={searchTerm}
                            onChange={handleSearch}
                            placeholder="جستجوی محصول..."
                            slotProps={{
                                input: {
                                    startAdornment: (
                                    <InputAdornment position="start">
                                        <Search />
                                    </InputAdornment>
                                ),
                                },
                            }}
                            sx={{
                                '& .MuiOutlinedInput-root': {
                                    borderRadius: 2,
                                },
                            }}
                        />

                        <FormControl
                            size="small"
                            sx={{
                                minWidth: {
                                    xs: '100%',
                                    md: 190,
                                },
                            }}
                        >
                            <InputLabel>
                                مرتب‌سازی
                            </InputLabel>

                            <Select
                                value={sortBy}
                                label="مرتب‌سازی"
                                onChange={(event) => {
                                    setSortBy(
                                        event.target.value
                                    );
                                    setPage(1);
                                }}
                            >
                                <MenuItem value="newest">
                                    جدیدترین
                                </MenuItem>

                                <MenuItem value="price_asc">
                                    ارزان‌ترین
                                </MenuItem>

                                <MenuItem value="price_desc">
                                    گران‌ترین
                                </MenuItem>

                                <MenuItem value="name">
                                    بر اساس نام
                                </MenuItem>
                            </Select>
                        </FormControl>

                        <Chip
                            icon={<Tune />}
                            label="فیلترها"
                            variant="outlined"
                            sx={{
                                height: 40,
                                borderRadius: 2,
                                px: 1,
                                alignSelf: {
                                    xs: 'flex-start',
                                    md: 'center',
                                },
                            }}
                        />
                    </Stack>
                </Box>

                {/* =========================
                    Tabs
                ========================= */}
                <Box
                    sx={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        mb: 3,
                    }}
                >
                    <Tabs
                        value={activeTab}
                        onChange={handleTabChange}
                        variant={
                            isMobile
                                ? 'fullWidth'
                                : 'standard'
                        }
                    >
                        <Tab
                            icon={
                                <ShoppingBagOutlined />
                            }
                            iconPosition="start"
                            label={`همه محصولات (${total || 0})`}
                        />

                        <Tab
                            icon={
                                <LocalFireDepartment />
                            }
                            iconPosition="start"
                            label={`پیشنهاد ویژه (${featuredProducts.length})`}
                        />
                    </Tabs>
                </Box>

                {/* =========================
                    Result Info
                ========================= */}
                <Stack
                    direction="row"
                    sx={{ mb: 2, justifyContent: 'space-between', alignItems: 'center' }}
                
                    component="div">
                    <Typography
                        variant="body2"
                        color="text.secondary"
                    >
                        نمایش {displayProducts.length} محصول
                    </Typography>

                    {searchTerm && (
                        <Chip
                            label={`جستجو: ${searchTerm}`}
                            size="small"
                            onDelete={() =>
                                setSearchTerm('')
                            }
                        />
                    )}
                </Stack>

                {/* =========================
                    Products
                ========================= */}
                {displayProducts.length === 0 ? (
                    <Box
                        sx={{
                            backgroundColor: '#fff',
                            borderRadius: 3,
                            py: 10,
                            textAlign: 'center',
                            border: '1px solid',
                            borderColor: 'divider',
                        }}
                    >
                        <Search
                            sx={{
                                fontSize: 60,
                                color: 'text.disabled',
                                mb: 2,
                            }}
                        />

                        <Typography
                            variant="h5"
                            sx={{ fontWeight: 700 }}
                            gutterBottom
                        >
                            محصولی پیدا نشد
                        </Typography>

                        <Typography
                            color="text.secondary"
                        >
                            عبارت جستجو یا فیلترهای خود را
                            تغییر دهید.
                        </Typography>
                    </Box>
                ) : (
                    <Grid
                        container
                        spacing={{
                            xs: 2,
                            sm: 2.5,
                            md: 3,
                        }}
                    >
                        {displayProducts.map(
                            (product) => (
                                <Grid
                                    size={{
                                        xs: 12,
                                        sm: 6,
                                        md: 4,
                                        lg: 3,
                                    }}
                                    key={product.id}
                                >
                                    <ProductCard
                                        product={product}
                                    />
                                </Grid>
                            )
                        )}
                    </Grid>
                )}

                {/* =========================
                    Pagination
                ========================= */}
                {activeTab === 0 &&
                    totalPages > 1 && (
                        <Box
                            sx={{
                                display: 'flex',
                                justifyContent:
                                    'center',
                                mt: 6,
                            }}
                        >
                            <Pagination
                                count={totalPages}
                                page={page}
                                onChange={(
                                    _,
                                    value
                                ) =>
                                    setPage(value)
                                }
                                color="primary"
                                size={
                                    isMobile
                                        ? 'small'
                                        : 'medium'
                                }
                                showFirstButton
                                showLastButton
                            />
                        </Box>
                    )}
            </Container>
        </Box>
    );
};

export default ProductListPage;