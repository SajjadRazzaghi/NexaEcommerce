import {
    useMemo,
    useState,
} from 'react';

import {
    Alert,
    Box,
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
} from '@mui/material';

import {
    Search,
    ShoppingBagOutlined,
    LocalFireDepartment,
} from '@mui/icons-material';

import {
    useSearchParams,
} from 'react-router-dom';

import {
    useProducts,
    useFeaturedProducts,
} from '../hooks/useProducts';

import ProductCard from '../components/ProductCard';

import type {
    ProductFilter,
} from '../api/products';

const PAGE_SIZE = 20;

export default function ProductListPage() {
    const [
        searchParams,
        setSearchParams,
    ] = useSearchParams();

    const [
        activeTab,
        setActiveTab,
    ] = useState(0);

    const page = Math.max(
        Number(
            searchParams.get(
                'page',
            ) ?? '1',
        ),
        1,
    );

    const search =
        searchParams.get(
            'search',
        ) ?? '';

    const sortBy =
        (searchParams.get(
            'sortBy',
        ) ?? 'newest') as ProductFilter['sortBy'];

    const categoryId =
        searchParams.get(
            'categoryId',
        ) ?? undefined;

    const minPrice =
        Number(
            searchParams.get(
                'minPrice',
            ) ?? '0',
        );

    const maxPrice =
        Number(
            searchParams.get(
                'maxPrice',
            ) ?? '0',
        );

    const isInStock =
        searchParams.get(
            'isInStock',
        ) === 'true';

    const filters =
        useMemo<ProductFilter>(
            () => ({
                page,
                pageSize:
                    PAGE_SIZE,
                search:
                    search ||
                    undefined,
                categoryId,
                minPrice:
                    minPrice > 0
                        ? minPrice
                        : undefined,
                maxPrice:
                    maxPrice > 0
                        ? maxPrice
                        : undefined,
                isInStock:
                    isInStock ||
                    undefined,
                sortBy,
            }),
            [
                page,
                search,
                categoryId,
                minPrice,
                maxPrice,
                isInStock,
                sortBy,
            ],
        );

    const {
        data,
        isLoading,
        isFetching,
        error,
    } = useProducts(
        filters,
    );

    const {
        data:
        featuredProducts =
        [],
    } =
        useFeaturedProducts(
            8,
        );

    const displayProducts =
        activeTab === 0
            ? data?.items ?? []
            : featuredProducts;

    const totalPages =
        data?.totalPages ?? 0;

    function updateParam(
        key: string,
        value:
            | string
            | number
            | boolean
            | null,
    ) {
        const next =
            new URLSearchParams(
                searchParams,
            );

        next.set(
            'page',
            '1',
        );

        if (
            value === null ||
            value === '' ||
            value === false
        ) {
            next.delete(key);
        } else {
            next.set(
                key,
                String(value),
            );
        }

        setSearchParams(
            next,
        );
    }

    if (
        isLoading &&
        !data
    ) {
        return (
            <Box
                sx={{
                    minHeight:
                        '65vh',
                    display:
                        'flex',
                    alignItems:
                        'center',
                    justifyContent:
                        'center',
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
                    خطا در دریافت محصولات.
                </Alert>
            </Container>
        );
    }

    return (
        <Box
            sx={{
                minHeight:
                    '100vh',
                backgroundColor:
                    '#fafafa',
                py: {
                    xs: 3,
                    md: 5,
                },
                direction:
                    'rtl',
            }}
        >
            <Container
                maxWidth="xl"
            >
                <Stack
                    spacing={3}
                >
                    <Box>
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
                            محصولات
                        </Typography>

                        <Typography
                            color="text.secondary"
                        >
                            جدیدترین محصولات
                            فروشگاه
                        </Typography>
                    </Box>

                    <Box
                        sx={{
                            backgroundColor:
                                '#fff',
                            border:
                                '1px solid',
                            borderColor:
                                'divider',
                            borderRadius: 3,
                            p: 2,
                        }}
                    >
                        <Stack
                            direction={{
                                xs: 'column',
                                md: 'row',
                            }}
                            spacing={2}
                        >
                            <TextField
                                fullWidth
                                size="small"
                                value={
                                    search
                                }
                                placeholder="جستجوی محصول..."
                                onChange={(
                                    event,
                                ) =>
                                    updateParam(
                                        'search',
                                        event
                                            .target
                                            .value,
                                    )
                                }
                                slotProps={{
                                    input: {
                                        startAdornment:
                                            (
                                                <InputAdornment position="start">
                                                    <Search />
                                                </InputAdornment>
                                            ),
                                    },
                                }}
                            />

                            <FormControl
                                size="small"
                                sx={{
                                    minWidth:
                                    {
                                        xs:
                                            '100%',
                                        md:
                                            200,
                                    },
                                }}
                            >
                                <InputLabel>
                                    مرتب‌سازی
                                </InputLabel>

                                <Select
                                    value={
                                        sortBy
                                    }
                                    label="مرتب‌سازی"
                                    onChange={(
                                        event,
                                    ) =>
                                        updateParam(
                                            'sortBy',
                                            event
                                                .target
                                                .value,
                                        )
                                    }
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
                                        نام
                                    </MenuItem>

                                    <MenuItem value="popular">
                                        محبوب‌ترین
                                    </MenuItem>
                                </Select>
                            </FormControl>
                        </Stack>
                    </Box>

                    <Tabs
                        value={
                            activeTab
                        }
                        onChange={(
                            _event,
                            value,
                        ) =>
                            setActiveTab(
                                value,
                            )
                        }
                    >
                        <Tab
                            icon={
                                <ShoppingBagOutlined />
                            }
                            iconPosition="start"
                            label={`همه محصولات (${data?.total ??
                                0
                                })`}
                        />

                        <Tab
                            icon={
                                <LocalFireDepartment />
                            }
                            iconPosition="start"
                            label={`پیشنهاد ویژه (${featuredProducts.length})`}
                        />
                    </Tabs>

                    {isFetching && (
                        <Typography
                            variant="caption"
                            color="text.secondary"
                        >
                            Updating products...
                        </Typography>
                    )}

                    {displayProducts.length ===
                        0 ? (
                        <Box
                            sx={{
                                py: 10,
                                textAlign:
                                    'center',
                            }}
                        >
                            <Typography
                                variant="h5"
                                sx={{
                                    fontWeight:
                                        800,
                                }}
                            >
                                محصولی پیدا نشد
                            </Typography>

                            <Typography
                                color="text.secondary"
                            >
                                فیلترها یا
                                عبارت جستجو
                                را تغییر
                                دهید.
                            </Typography>
                        </Box>
                    ) : (
                        <Grid
                            container
                            spacing={3}
                        >
                            {displayProducts.map(
                                (
                                    product,
                                ) => (
                                    <Grid
                                        key={
                                            product.id
                                        }
                                        size={{
                                            xs: 12,
                                            sm: 6,
                                            md: 4,
                                            lg: 3,
                                        }}
                                    >
                                        <ProductCard
                                            product={
                                                product
                                            }
                                        />
                                    </Grid>
                                ),
                            )}
                        </Grid>
                    )}

                    {activeTab ===
                        0 &&
                        totalPages >
                        1 && (
                            <Box
                                sx={{
                                    display:
                                        'flex',
                                    justifyContent:
                                        'center',
                                    py: 4,
                                }}
                            >
                                <Pagination
                                    page={
                                        page
                                    }
                                    count={
                                        totalPages
                                    }
                                    onChange={(
                                        _event,
                                        value,
                                    ) => {
                                        const next =
                                            new URLSearchParams(
                                                searchParams,
                                            );

                                        next.set(
                                            'page',
                                            String(
                                                value,
                                            ),
                                        );

                                        setSearchParams(
                                            next,
                                        );
                                    }}
                                    showFirstButton
                                    showLastButton
                                />
                            </Box>
                        )}
                </Stack>
            </Container>
        </Box>
    );
}