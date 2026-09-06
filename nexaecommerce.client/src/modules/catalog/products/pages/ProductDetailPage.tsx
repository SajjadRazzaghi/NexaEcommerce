import { Link, useNavigate, useParams } from 'react-router-dom';
import {
    ArrowLeft,
    Edit,
    Package,
} from 'lucide-react';

import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    CardHeader,
    Chip,
    Divider,
    Grid,
    Skeleton,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow,
    Typography,
} from '@mui/material';

import { useProduct } from '../hooks';
import type { ProductVariant } from '../../api/products';

function formatPrice(
    value: number,
    currency: string,
) {
    return `${ new Intl.NumberFormat('en-US').format(value) } ${ currency } `;
}

function StatusChip({
    label,
    active,
}: {
    label: string;
    active: boolean;
}) {
    return (
        <Chip
            label={label}
            size="small"
            color={active ? 'success' : 'default'}
            variant={active ? 'filled' : 'outlined'}
        />
    );
}

function VariantSummary({
    variant,
}: {
    variant: ProductVariant;
}) {
    const attributes = [
        variant.color,
        variant.size,
    ]
        .filter(Boolean)
        .join(' • ');

    return (
        <TableRow>
            <TableCell>
                <Typography
                    variant="body2"
                    sx={{ fontWeight: 700 }}
                >
                    {variant.sku}
                </Typography>

                {attributes && (
                    <Typography
                        variant="caption"
                        color="text.secondary"
                    >
                        {attributes}
                    </Typography>
                )}
            </TableCell>

            <TableCell>
                {formatPrice(
                    variant.priceOverride ?? 0,
                    'IRR',
                )}
            </TableCell>

            <TableCell>
                <Typography
                    sx={{
                        fontWeight: 700,
                        color:
                            variant.stockQuantity > 0
                                ? 'success.main'
                                : 'error.main',
                    }}
                >
                    {variant.stockQuantity}
                </Typography>
            </TableCell>

            <TableCell>
                <StatusChip
                    label={
                        variant.isActive
                            ? 'Active'
                            : 'Inactive'
                    }
                    active={variant.isActive}
                />
            </TableCell>
        </TableRow>
    );
}

export default function ProductDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const {
        data: product,
        isLoading,
        isError,
        error,
        refetch,
    } = useProduct(id);

    if (isLoading) {
        return (
            <Stack spacing={3}>
                <Skeleton
                    variant="rectangular"
                    height={56}
                    sx={{ borderRadius: 2 }}
                />

                <Grid container spacing={3}>
                    <Grid size={{ xs: 12, md: 5 }}>
                        <Skeleton
                            variant="rectangular"
                            height={420}
                            sx={{ borderRadius: 3 }}
                        />
                    </Grid>

                    <Grid size={{ xs: 12, md: 7 }}>
                        <Stack spacing={2}>
                            <Skeleton height={55} />
                            <Skeleton height={35} />
                            <Skeleton height={35} />
                            <Skeleton height={120} />
                        </Stack>
                    </Grid>
                </Grid>
            </Stack>
        );
    }

    if (isError || !product) {
        return (
            <Stack spacing={2}>
                <Alert
                    severity="error"
                    action={
                        <Button
                            color="inherit"
                            size="small"
                            onClick={() => refetch()}
                        >
                            Retry
                        </Button>
                    }
                >
                    {error instanceof Error
                        ? error.message
                        : 'Failed to load product.'}
                </Alert>

                <Button
                    component={Link}
                    to="/admin/products"
                    startIcon={<ArrowLeft />}
                    variant="outlined"
                    sx={{ alignSelf: 'flex-start' }}
                >
                    Back to products
                </Button>
            </Stack>
        );
    }

    const mainImage =
        product.images?.find(
            (image) => image.isMain,
        )?.imageUrl ??
        product.images?.[0]?.imageUrl ??
        '/placeholder.jpg';

    const variants =
        product.variants ?? [];

    const totalStock =
        variants.reduce(
            (sum, variant) =>
                sum + variant.stockQuantity,
            0,
        );

    return (
        <Stack spacing={3}>
            <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={2}
                sx={{
                    justifyContent: 'space-between',
                    alignItems: {
                        xs: 'stretch',
                        sm: 'center',
                    },
                }}
            >
                <Box>
                    <Typography
                        variant="h4"
                        sx={{
                            fontWeight: 900,
                            mb: 0.5,
                        }}
                    >
                        {product.name}
                    </Typography>

                    <Typography
                        color="text.secondary"
                    >
                        SKU: {product.sku}
                    </Typography>
                </Box>

                <Stack
                    direction="row"
                    spacing={1}
                >
                    <Button
                        variant="outlined"
                        startIcon={<ArrowLeft />}
                        onClick={() =>
                            navigate(
                                '/admin/products',
                            )
                        }
                    >
                        Back
                    </Button>

                    <Button
                        variant="contained"
                        startIcon={<Edit />}
                        onClick={() =>
                            navigate(
                                `/ admin / products / ${ product.id }/edit`,
                            )
                        }
                    >
    Edit Product
                    </Button >
                </Stack >
            </Stack >

            <Card>
                <CardContent>
                    <Grid container spacing={4}>
                        <Grid size={{ xs: 12, md: 5 }}>
                            <Box
                                component="img"
                                src={mainImage}
                                alt={product.name}
                                sx={{
                                    width: '100%',
                                    height: {
                                        xs: 300,
                                        md: 430,
                                    },
                                    objectFit:
                                        'contain',
                                    borderRadius: 3,
                                    bgcolor:
                                        'background.default',
                                }}
                                onError={(event) => {
                                    const image =
                                        event.currentTarget;

                                    if (
                                        !image.src.endsWith(
                                            '/placeholder.jpg',
                                        )
                                    ) {
                                        image.src =
                                            '/placeholder.jpg';
                                    }
                                }}
                            />

                            {product.images &&
                                product.images.length >
                                    1 && (
                                    <Stack
                                        direction="row"
                                        spacing={1}
                                        sx={{
                                            mt: 2,
                                            overflowX:
                                                'auto',
                                        }}
                                    >
                                        {product.images.map(
                                            (
                                                image,
                                            ) => (
                                                <Box
                                                    key={
                                                        image.id
                                                    }
                                                    component="img"
                                                    src={
                                                        image.imageUrl
                                                    }
                                                    alt={
                                                        image.altText ??
                                                        product.name
                                                    }
                                                    sx={{
                                                        width: 72,
                                                        height: 72,
                                                        objectFit:
                                                            'cover',
                                                        borderRadius: 2,
                                                        border:
                                                            '1px solid',
                                                        borderColor:
                                                            'divider',
                                                    }}
                                                />
                                            ),
                                        )}
                                    </Stack>
                                )}
                        </Grid>

                        <Grid size={{ xs: 12, md: 7 }}>
                            <Stack spacing={2.5}>
                                <Stack
                                    direction="row"
                                    spacing={1}
                                    useFlexGap
                                    sx={{
                                        flexWrap: 'wrap',
                                    }}
                                >
                                    <StatusChip
                                        label={
                                            product.isActive
                                                ? 'Active'
                                                : 'Inactive'
                                        }
                                        active={
                                            product.isActive
                                        }
                                    />

                                    <StatusChip
                                        label={
                                            product.isPublished
                                                ? 'Published'
                                                : 'Unpublished'
                                        }
                                        active={
                                            product.isPublished
                                        }
                                    />

                                    <StatusChip
                                        label={
                                            product.isFeatured
                                                ? 'Featured'
                                                : 'Not Featured'
                                        }
                                        active={
                                            product.isFeatured
                                        }
                                    />

                                    <StatusChip
                                        label={
                                            product.isInStock
                                                ? 'In Stock'
                                                : 'Out of Stock'
                                        }
                                        active={
                                            product.isInStock
                                        }
                                    />
                                </Stack>

                                {product.brandName && (
                                    <Typography
                                        color="text.secondary"
                                    >
                                        Brand:{' '}
                                        <strong>
                                            {
                                                product.brandName
                                            }
                                        </strong>
                                    </Typography>
                                )}

                                <Typography
                                    variant="h4"
                                    color="primary"
                                    sx={{
                                        fontWeight: 900,
                                    }}
                                >
                                    {formatPrice(
                                        product.finalPrice ??
                                            product.price,
                                        product.currency,
                                    )}
                                </Typography>

                                {product.comparePrice &&
                                    product.comparePrice >
                                        product.finalPrice && (
                                        <Typography
                                            color="text.secondary"
                                            sx={{
                                                textDecoration:
                                                    'line-through',
                                            }}
                                        >
                                            {formatPrice(
                                                product.comparePrice,
                                                product.currency,
                                            )}
                                        </Typography>
                                    )}

                                <Divider />

                                <Grid
                                    container
                                    spacing={2}
                                >
                                    <Grid
                                        size={{
                                            xs: 12,
                                            sm: 6,
                                        }}
                                    >
                                        <Card
                                            variant="outlined"
                                        >
                                            <CardContent>
                                                <Typography
                                                    variant="caption"
                                                    color="text.secondary"
                                                >
                                                    Base Price
                                                </Typography>

                                                <Typography
                                                    variant="h6"
                                                    sx={{
                                                        fontWeight: 800,
                                                    }}
                                                >
                                                    {formatPrice(
                                                        product.price,
                                                        product.currency,
                                                    )}
                                                </Typography>
                                            </CardContent>
                                        </Card>
                                    </Grid>

                                    <Grid
                                        size={{
                                            xs: 12,
                                            sm: 6,
                                        }}
                                    >
                                        <Card
                                            variant="outlined"
                                        >
                                            <CardContent>
                                                <Typography
                                                    variant="caption"
                                                    color="text.secondary"
                                                >
                                                    Total Stock
                                                </Typography>

                                                <Typography
                                                    variant="h6"
                                                    sx={{
                                                        fontWeight: 800,
                                                    }}
                                                >
                                                    {totalStock}
                                                </Typography>
                                            </CardContent>
                                        </Card>
                                    </Grid>
                                </Grid>

                                {product.shortDescription && (
                                    <Box>
                                        <Typography
                                            variant="h6"
                                            sx={{
                                                fontWeight: 800,
                                                mb: 1,
                                            }}
                                        >
                                            Short Description
                                        </Typography>

                                        <Typography
                                            color="text.secondary"
                                            sx={{
                                                whiteSpace:
                                                    'pre-line',
                                                lineHeight: 1.9,
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
                                                whiteSpace:
                                                    'pre-line',
                                                lineHeight: 1.9,
                                            }}
                                        >
                                            {
                                                product.description
                                            }
                                        </Typography>
                                    </Box>
                                )}

                                {product.categories?.length >
                                    0 && (
                                    <Box>
                                        <Typography
                                            variant="h6"
                                            sx={{
                                                fontWeight: 800,
                                                mb: 1,
                                            }}
                                        >
                                            Categories
                                        </Typography>

                                        <Stack
                                            direction="row"
                                            spacing={1}
                                            useFlexGap
                                            sx={{
                                                flexWrap: 'wrap',
                                            }}
                                        >
                                            {product.categories.map(
                                                (
                                                    category,
                                                ) => (
                                                    <Chip
                                                        key={
                                                            category
                                                        }
                                                        label={
                                                            category
                                                        }
                                                        size="small"
                                                    />
                                                ),
                                            )}
                                        </Stack>
                                    </Box>
                                )}
                            </Stack>
                        </Grid>
                    </Grid>
                </CardContent>
            </Card>

            <Card>
                <CardHeader
                    avatar={<Package />}
                    title="Variants"
                    subheader={`${variants.length} variant(s)`}
                />

                <CardContent>
                    {variants.length === 0 ? (
                        <Alert severity="info">
                            This product has no variants.
                        </Alert>
                    ) : (
                        <Box
                            sx={{
                                overflowX:
                                    'auto',
                            }}
                        >
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell>
                                            SKU / Attributes
                                        </TableCell>

                                        <TableCell>
                                            Price
                                        </TableCell>

                                        <TableCell>
                                            Stock
                                        </TableCell>

                                        <TableCell>
                                            Status
                                        </TableCell>
                                    </TableRow>
                                </TableHead>

                                <TableBody>
                                    {variants.map(
                                        (
                                            variant,
                                        ) => (
                                            <VariantSummary
                                                key={
                                                    variant.id
                                                }
                                                variant={
                                                    variant
                                                }
                                            />
                                        ),
                                    )}
                                </TableBody>
                            </Table>
                        </Box>
                    )}
                </CardContent>
            </Card>
        </Stack >
    );
}

