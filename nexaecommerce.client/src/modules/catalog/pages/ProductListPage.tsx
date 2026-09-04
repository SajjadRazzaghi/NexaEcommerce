import {
    useMemo,
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
    useTranslation,
} from 'react-i18next';

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
    ] =
        useSearchParams();

    
const {
    i18n,
} =
    useTranslation();

const isFa =
    i18n.language
        .toLowerCase()
        .startsWith('fa');

const text = isFa
    ? {
          title:
              'محصولات',
          subtitle:
              'جدیدترین محصولات فروشگاه',
          search:
              'جستجوی محصول...',
          sort:
              'مرتب‌سازی',
          newest:
              'جدیدترین',
          priceAsc:
              'ارزان‌ترین',
          priceDesc:
              'گران‌ترین',
          name:
              'نام',
          popular:
              'محبوب‌ترین',
          all:
              'همه محصولات',
          featured:
              'پیشنهاد ویژه',
          updating:
              'در حال بروزرسانی محصولات...',
          loading:
              'در حال بارگذاری محصولات...',
          error:
              'خطا در دریافت محصولات.',
          emptyTitle:
              'محصولی پیدا نشد',
          emptyText:
              'فیلترها یا عبارت جستجو را تغییر دهید.',
      }
    : {
          title:
              'Products',
          subtitle:
              'Discover the latest products in our store.',
          search:
              'Search products...',
          sort:
              'Sort by',
          newest:
              'Newest',
          priceAsc:
              'Lowest price',
          priceDesc:
              'Highest price',
          name:
              'Name',
          popular:
              'Most popular',
          all:
              'All products',
          featured:
              'Featured',
          updating:
              'Updating products...',
          loading:
              'Loading products...',
          error:
              'Failed to load products.',
          emptyTitle:
              'No products found',
          emptyText:
              'Try changing your search or filters.',
      };

    const activeTab = Number(
        searchParams.get('tab') ?? '0',
    );

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
    ) ??
        'newest') as ProductFilter['sortBy'];

const categoryId =
    searchParams.get(
        'categoryId',
    ) ??
    undefined;

const minPrice = Number(
    searchParams.get(
        'minPrice',
    ) ?? '0',
);

const maxPrice = Number(
    searchParams.get(
        'maxPrice',
    ) ?? '0',
);

const isInStock =
    searchParams.get(
        'isInStock',
    ) ===
    'true';

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
} =
    useProducts(
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
        : featuredProducts.map(
              product => ({
                  id:
                      product.id,
                  name:
                      product.name,
                  sku:
                      product.sku,
                  slug:
                      product.slug,
                  price:
                      product.price,
                  comparePrice:
                      product.comparePrice,
                  finalPrice:
                      product.finalPrice,
                  discountPercentage:
                      product.discountPercentage,
                  currency:
                      product.currency,
                  brandId:
                      product.brandId,
                  brandName:
                      product.brandName,
                  isActive:
                      product.isActive,
                  isFeatured:
                      product.isFeatured,
                  isPublished:
                      product.isPublished,
                  isInStock:
                      product.isInStock,
                  stockQuantity:
                      product.stockQuantity,
                  mainImage:
                      product.images.find(
                          image =>
                              image.isMain,
                      )?.imageUrl ??
                      product.images[0]
                          ?.imageUrl ??
                      null,
                  categoryNames:
                      product.categories,
                  categoryIds:
                      product.categoryIds,
                  createdAt:
                      product.createdAt,
              }),
          );

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

function changeTab(
    value: number,
) {
    const next =
        new URLSearchParams(
            searchParams,
        );

    next.set(
        'tab',
        String(value),
    );
    next.set(
        'page',
        '1',
    );

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
                flexDirection:
                    'column',
                alignItems:
                    'center',
                justifyContent:
                    'center',
                gap: 2,
                direction:
                    isFa
                        ? 'rtl'
                        : 'ltr',
            }}
        >
            <CircularProgress />
            <Typography
                color="text.secondary"
            >
                {text.loading}
            </Typography>
        </Box>
    );
}

if (error) {
    return (
        <Container
            maxWidth="lg"
            sx={{
                py: 6,
                direction:
                    isFa
                        ? 'rtl'
                        : 'ltr',
            }}
        >
            <Alert severity="error">
                {
                    text.error
                }
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
                isFa
                    ? 'rtl'
                    : 'ltr',
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
                            fontWeight: 900,
                            fontSize: {
                                xs: '2rem',
                                md: '3rem',
                            },
                        }}
                    >
                        {
                            text.title
                        }
                    </Typography>

                    <Typography
                        color="text.secondary"
                        sx={{
                            mt: 1,
                        }}
                    >
                        {
                            text.subtitle
                        }
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
                        borderRadius:
                            3,
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
                            placeholder={
                                text.search
                            }
                            onChange={event =>
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
                                minWidth: {
                                    xs:
                                        '100%',
                                    md:
                                        210,
                                },
                            }}
                        >
                            <InputLabel>
                                {
                                    text.sort
                                }
                            </InputLabel>

                            <Select
                                value={
                                    sortBy
                                }
                                label={
                                    text.sort
                                }
                                onChange={event =>
                                    updateParam(
                                        'sortBy',
                                        event
                                            .target
                                            .value,
                                    )
                                }
                            >
                                <MenuItem value="newest">
                                    {
                                        text.newest
                                    }
                                </MenuItem>

                                <MenuItem value="price_asc">
                                    {
                                        text.priceAsc
                                    }
                                </MenuItem>

                                <MenuItem value="price_desc">
                                    {
                                        text.priceDesc
                                    }
                                </MenuItem>

                                <MenuItem value="name">
                                    {
                                        text.name
                                    }
                                </MenuItem>

                                <MenuItem value="popular">
                                    {
                                        text.popular
                                    }
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
                        changeTab(
                            value,
                        )
                    }
                >
                    <Tab
                        icon={
                            <ShoppingBagOutlined />
                        }
                        iconPosition="start"
                        label={`${ text.all } (${ data?.total ?? 0 })`}
                    />

                    <Tab
                        icon={
                            <LocalFireDepartment />
                        }
                        iconPosition="start"
                        label={`${ text.featured } (${ featuredProducts.length })`}
                    />
                </Tabs>

                {isFetching && (
                    <Typography
                        variant="caption"
                        color="text.secondary"
                    >
                        {
                            text.updating
                        }
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
                            {
                                text.emptyTitle
                            }
                        </Typography>

                        <Typography
                            color="text.secondary"
                            sx={{
                                mt: 1,
                            }}
                        >
                            {
                                text.emptyText
                            }
                        </Typography>
                    </Box>
                ) : (
                    <Grid
                        container
                        spacing={3}
                    >
                        {displayProducts.map(
                            product => (
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

                {activeTab === 0 &&
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
