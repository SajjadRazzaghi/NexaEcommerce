import {
    useMemo,
} from 'react';

import {
    Alert,
    Box,
    Button,
    Checkbox,
    CircularProgress,
    Container,
    Divider,
    FormControl,
    FormControlLabel,
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
    RestartAlt,
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

import {
    useCategories,
} from '../hooks/useCategories';

import ProductCard from '../components/ProductCard';

import type {
    ProductFilter,
} from '../api/products';

const PAGE_SIZE = 20;

const SORT_VALUES =
    [
        'newest',
        'price_asc',
        'price_desc',
        'name',
        'popular',
    ] as const;

type SortValue =
    typeof SORT_VALUES[number];

function parsePage(
    value: string | null,
): number {
    const parsed =
        Number(value);

    if (
        !Number.isInteger(
            parsed,
        ) ||
        parsed < 1
    ) {
        return 1;
    }

    return parsed;
}

function parsePrice(
    value: string | null,
): number | undefined {
    if (
        value === null ||
        value.trim() === ''
    ) {
        return undefined;
    }

    const parsed =
        Number(value);

    if (
        !Number.isFinite(
            parsed,
        ) ||
        parsed <= 0
    ) {
        return undefined;
    }

    return parsed;
}

function parseSort(
    value: string | null,
): SortValue {
    if (
        value &&
        (
            SORT_VALUES as readonly string[]
        ).includes(value)
    ) {
        return value as SortValue;
    }

    return 'newest';
}

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
                'محصولات فروشگاه را جستجو و فیلتر کنید.',
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
            category:
                'دسته‌بندی',
            allCategories:
                'همه دسته‌بندی‌ها',
            minPrice:
                'حداقل قیمت',
            maxPrice:
                'حداکثر قیمت',
            inStock:
                'فقط کالاهای موجود',
            reset:
                'حذف فیلترها',
            all:
                'همه محصولات',
            featured:
                'پیشنهاد ویژه',
            updating:
                'در حال بروزرسانی محصولات...',
            loading:
                'در حال بارگذاری محصولات...',
            categoryLoading:
                'در حال بارگذاری دسته‌بندی‌ها...',
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
                'Search and filter the products available in the store.',
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
            category:
                'Category',
            allCategories:
                'All categories',
            minPrice:
                'Minimum price',
            maxPrice:
                'Maximum price',
            inStock:
                'In-stock only',
            reset:
                'Clear filters',
            all:
                'All products',
            featured:
                'Featured',
            updating:
                'Updating products...',
            loading:
                'Loading products...',
            categoryLoading:
                'Loading categories...',
            error:
                'Failed to load products.',
            emptyTitle:
                'No products found',
            emptyText:
                'Try changing your search or filters.',
        };

    const activeTabRaw =
        Number(
            searchParams.get(
                'tab',
            ) ?? '0',
        );

    const activeTab =
        activeTabRaw === 1
            ? 1
            : 0;

    const page =
        parsePage(
            searchParams.get(
                'page',
            ),
        );

    const search =
        searchParams.get(
            'search',
        ) ?? '';

    const sortBy =
        parseSort(
            searchParams.get(
                'sortBy',
            ),
        );

    const categoryId =
        searchParams.get(
            'categoryId',
        ) ?? '';

    const minPrice =
        parsePrice(
            searchParams.get(
                'minPrice',
            ),
        );

    const maxPrice =
        parsePrice(
            searchParams.get(
                'maxPrice',
            ),
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
                    search.trim() ||
                    undefined,
                categoryId:
                    categoryId ||
                    undefined,
                minPrice,
                maxPrice,
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
        isError,
    } =
        useProducts(
            filters,
        );

    const {
        data:
            featuredProducts = [],
    } =
        useFeaturedProducts(
            8,
        );

    const {
        data:
            categories = [],
        isLoading:
            categoriesLoading,
    } =
        useCategories();

    const normalizedCategories =
        Array.isArray(
            categories,
        )
            ? categories
            : [];

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
        data?.totalPages ??
        0;

    const updateParam = (
        key: string,
        value:
            | string
            | number
            | boolean
            | null,
    ) => {
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
            next.delete(
                key,
            );
        } else {
            next.set(
                key,
                String(value),
            );
        }

        setSearchParams(
            next,
        );
    };

    const resetFilters =
        () => {
            const next =
                new URLSearchParams();

            if (
                activeTab === 1
            ) {
                next.set(
                    'tab',
                    '1',
                );
            }

            setSearchParams(
                next,
            );
        };

    const changeTab =
        (
            value: number,
        ) => {
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
        };

    if (
        isLoading &&
        !data &&
        activeTab === 0
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
                    {
                        text.loading
                    }
                </Typography>
            </Box>
        );
    }

    if (
        isError &&
        activeTab === 0
    ) {
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

                <Button
                    sx={{
                        mt: 2,
                    }}
                    startIcon={
                        <RestartAlt />
                    }
                    onClick={() => {
                        window.location.reload();
                    }}
                >
                    {text.reset}
                </Button>
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
                                fontWeight:
                                    900,
                                fontSize: {
                                    xs:
                                        '2rem',
                                    md:
                                        '3rem',
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
                            spacing={2}
                        >
                            <Stack
                                direction={{
                                    xs:
                                        'column',
                                    md:
                                        'row',
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
                                        minWidth:
                                            {
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

                            <Divider />

                            <Grid
                                container
                                spacing={2}
                            >
                                <Grid
                                    size={{
                                        xs:
                                            12,
                                        md:
                                            4,
                                    }}
                                >
                                    <FormControl
                                        fullWidth
                                        size="small"
                                    >
                                        <InputLabel>
                                            {
                                                text.category
                                            }
                                        </InputLabel>

                                        <Select
                                            value={
                                                categoryId
                                            }
                                            label={
                                                text.category
                                            }
                                            onChange={event =>
                                                updateParam(
                                                    'categoryId',
                                                    event
                                                        .target
                                                        .value,
                                                )
                                            }
                                        >
                                            <MenuItem value="">
                                                {
                                                    text.allCategories
                                                }
                                            </MenuItem>

                                            {normalizedCategories.map(
                                                category => (
                                                    <MenuItem
                                                        key={
                                                            category.id
                                                        }
                                                        value={
                                                            category.id
                                                        }
                                                    >
                                                        {
                                                            category.name
                                                        }
                                                    </MenuItem>
                                                ),
                                            )}
                                        </Select>
                                    </FormControl>

                                    {categoriesLoading && (
                                        <Typography
                                            variant="caption"
                                            color="text.secondary"
                                            sx={{
                                                mt:
                                                    0.5,
                                                display:
                                                    'block',
                                            }}
                                        >
                                            {
                                                text.categoryLoading
                                            }
                                        </Typography>
                                    )}
                                </Grid>

                                <Grid
                                    size={{
                                        xs:
                                            12,
                                        sm:
                                            6,
                                        md:
                                            3,
                                    }}
                                >
                                   
                                    <TextField
                                        fullWidth
                                        size="small"
                                        type="number"
                                        value={minPrice}
                                        label={isFa ? 'حداقل قیمت' : 'Minimum price'}
                                        slotProps={{
                                            htmlInput: {
                                                min: 0,
                                            },
                                        }}
                                        onChange={event =>
                                            updateParam(
                                                'minPrice',
                                                event.target.value,
                                            )
                                        }
                                    />

                                   

                                </Grid>

                                <Grid
                                    size={{
                                        xs:
                                            12,
                                        sm:
                                            6,
                                        md:
                                            3,
                                    }}
                                >
                                    <TextField
                                        fullWidth
                                        size="small"
                                        type="number"
                                        value={maxPrice}
                                        label={isFa ? 'حداکثر قیمت' : 'Maximum price'}
                                        slotProps={{
                                            htmlInput: {
                                                min: 0,
                                            },
                                        }}
                                        onChange={event =>
                                            updateParam(
                                                'maxPrice',
                                                event.target.value,
                                            )
                                        }
                                    />

                                </Grid>

                                <Grid
                                    size={{
                                        xs:
                                            12,
                                        md:
                                            2,
                                    }}
                                    sx={{
                                        display:
                                            'flex',
                                        alignItems:
                                            'center',
                                    }}
                                >
                                    <FormControlLabel
                                        control={
                                            <Checkbox
                                                checked={
                                                    isInStock
                                                }
                                                onChange={event =>
                                                    updateParam(
                                                        'isInStock',
                                                        event
                                                            .target
                                                            .checked,
                                                    )
                                                }
                                            />
                                        }
                                        label={
                                            text.inStock
                                        }
                                    />
                                </Grid>
                            </Grid>

                            <Box
                                sx={{
                                    display:
                                        'flex',
                                    justifyContent:
                                        'flex-end',
                                }}
                            >
                                <Button
                                    variant="text"
                                    startIcon={
                                        <RestartAlt />
                                    }
                                    onClick={
                                        resetFilters
                                    }
                                    disabled={
                                        !search &&
                                        !categoryId &&
                                        minPrice ===
                                            undefined &&
                                        maxPrice ===
                                            undefined &&
                                        !isInStock
                                    }
                                >
                                    {
                                        text.reset
                                    }
                                </Button>
                            </Box>
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

                    {activeTab === 0 &&
                        isFetching && (
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
                                            xs:
                                                12,
                                            sm:
                                                6,
                                            md:
                                                4,
                                            lg:
                                                3,
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
                                        Math.min(
                                            page,
                                            totalPages,
                                        )
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
