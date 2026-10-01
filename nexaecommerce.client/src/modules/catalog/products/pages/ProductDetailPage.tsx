import {
    Link,
    useNavigate,
    useParams,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import type {
    TFunction,
} from 'i18next';

import {
    ArrowLeft,
    Edit,
    Package,
} from 'lucide-react';

import {
    resolveMediaUrl,
} from '@/lib/media-url';

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

import {
    useProduct,
} from '../hooks';

import type {
    ProductVariant,
    ProductVariantAttribute,
} from '../../api/products';


/* -------------------------------------------------------------------------- */
/*                                Price Helper                                */
/* -------------------------------------------------------------------------- */

function formatPrice(
    value: number,
    currency: string,
    locale: string,
) {
    return`${new Intl.NumberFormat(locale).format(value)} ${currency}`;
}


/* -------------------------------------------------------------------------- */
/*                              Status Chip                                   */
/* -------------------------------------------------------------------------- */

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
            color={
                active
                    ? 'success'
                    : 'default'
           }
            variant={
                active
                    ? 'filled'
                    : 'outlined'
           }
       />
    );
}


/* -------------------------------------------------------------------------- */
/*                         Variant Attribute Helpers                           */
/* -------------------------------------------------------------------------- */

/**
 * Creates a human-readable attribute summary for a variant.
 *
 * Example:
 *
 * Color: Black
 * Size: XL
 * Material: Cotton
 *
 * becomes:
 *
 * Black • XL • Cotton
 *
 * We intentionally use generic`variant.attributes`
 * rather than legacy`variant.color`/`variant.size`.
 */
function getVariantAttributeSummary(
    attributes:
        | ProductVariantAttribute[]
        | undefined,
): string {
    if (
        !attributes ||
        attributes.length === 0
    ) {
        return '';
   }

    return attributes
        .map(
            attribute =>
                attribute.displayValue ||
                attribute.value,
        )
        .filter(Boolean)
        .join(' • ');
}


/**
 * Render all dynamic variant attributes.
 *
 * This is useful when a variant has:
 *
 * - Size only
 * - Color only
 * - Size + Color
 * - Color + Material
 * - Size + Color + Material
 * - Any future catalog attribute
 *
 * The renderer does not assume specific attribute names.
 */
function VariantAttributes({
    attributes,
    t,
}: {
    attributes:
        | ProductVariantAttribute[]
        | undefined;

    t: TFunction;
}) {
    if (
        !attributes ||
        attributes.length === 0
    ) {
        return null;
   }

    return (
        <Stack
            direction="row"
            spacing={0.75}
            useFlexGap
            sx={{
                flexWrap: 'wrap',
                mt: 0.75,
           }}
        >
            {attributes
                .slice()
                .sort(
                    (
                        a,
                        b,
                    ) =>
                        a.attributeName.localeCompare(
                            b.attributeName,
                        ),
                )
                .map(
                    attribute => {
                        const displayValue =
                            attribute.displayValue ||
                            attribute.value;

                        if (!displayValue) {
                            return null;
                       }

                        return (
                            <Chip
                                key={
                                    attribute.attributeValueId
                               }
                                size="small"
                                variant="outlined"
                                label={
                                    <>
                                        {attribute.colorHex && (
                                            <Box
                                                component="span"
                                                sx={{
                                                    display:
                                                        'inline-block',
                                                    width: 10,
                                                    height: 10,
                                                    borderRadius:
                                                        '50%',
                                                    bgcolor:
                                                        attribute.colorHex,
                                                    border:
                                                        '1px solid',
                                                    borderColor:
                                                        'divider',
                                                    mr: 0.75,
                                                    verticalAlign:
                                                        'middle',
                                               }}
                                           />
                                        )}

                                        {attribute.attributeName
                                            ?`${attribute.attributeName}: ${displayValue}`
                                            : displayValue}
                                    </>
                               }
                           />
                        );
                   },
                )}

            {attributes.length ===
                0 && (
                <Typography
                    variant="caption"
                    color="text.secondary"
                >
                    {t(
                        'productAdminDetail.variants.noAttributes',
                    )}
                </Typography>
            )}
        </Stack>
    );
}


/* -------------------------------------------------------------------------- */
/*                         Variant Summary Row                                */
/* -------------------------------------------------------------------------- */

function VariantSummary({
    variant,
    currency,
    locale,
    t,
}: {
    variant: ProductVariant;
    currency: string;
    locale: string;
    t: TFunction;
}) {
    const attributeSummary =
        getVariantAttributeSummary(
            variant.attributes,
        );

    return (
        <TableRow>
            {/* -------------------------------------------------------------- */}
            {/* SKU/Attributes                                               */}
            {/* -------------------------------------------------------------- */}

            <TableCell>
                <Typography
                    variant="body2"
                    sx={{
                        fontWeight: 700,
                   }}
                >
                    {variant.sku}
                </Typography>

                {attributeSummary && (
                    <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{
                            display: 'block',
                            mt: 0.5,
                       }}
                    >
                        {attributeSummary}
                    </Typography>
                )}

                <VariantAttributes
                    attributes={
                        variant.attributes
                   }
                    t={t}
               />
            </TableCell>

            {/* -------------------------------------------------------------- */}
            {/* Price                                                          */}
            {/* -------------------------------------------------------------- */}

            <TableCell>
                {formatPrice(
                    variant.priceOverride ??
                        0,
                    currency,
                    locale,
                )}
            </TableCell>

            {/* -------------------------------------------------------------- */}
            {/* Stock                                                          */}
            {/* -------------------------------------------------------------- */}

            <TableCell>
                <Typography
                    sx={{
                        fontWeight: 700,
                        color:
                            variant.stockQuantity >
                            0
                                ? 'success.main'
                                : 'error.main',
                   }}
                >
                    {
                        variant.stockQuantity
                   }
                </Typography>
            </TableCell>

            {/* -------------------------------------------------------------- */}
            {/* Status                                                         */}
            {/* -------------------------------------------------------------- */}

            <TableCell>
                <StatusChip
                    label={
                        variant.isActive
                            ? t(
                                'productAdminDetail.status.active',
                            )
                            : t(
                                'productAdminDetail.status.inactive',
                            )
                   }
                    active={
                        variant.isActive
                   }
               />
            </TableCell>
        </TableRow>
    );
}


/* -------------------------------------------------------------------------- */
/*                            Loading Skeleton                                */
/* -------------------------------------------------------------------------- */

function ProductDetailSkeleton() {
    return (
        <Stack spacing={3}>
            <Skeleton
                variant="rectangular"
                height={56}
                sx={{
                    borderRadius: 2,
               }}
           />

            <Grid
                container
                spacing={3}
            >
                <Grid
                    size={{
                        xs: 12,
                        md: 5,
                   }}
                >
                    <Skeleton
                        variant="rectangular"
                        height={420}
                        sx={{
                            borderRadius: 3,
                       }}
                   />
                </Grid>

                <Grid
                    size={{
                        xs: 12,
                        md: 7,
                   }}
                >
                    <Stack spacing={2}>
                        <Skeleton height={55}/>
                        <Skeleton height={35}/>
                        <Skeleton height={35}/>
                        <Skeleton height={120}/>
                    </Stack>
                </Grid>
            </Grid>
        </Stack>
    );
}


/* -------------------------------------------------------------------------- */
/*                              Main Page                                     */
/* -------------------------------------------------------------------------- */

export default function ProductDetailPage() {
    const {
        id,
   } = useParams<{
        id: string;
   }>();


    const navigate =
        useNavigate();


    const {
        t,
        i18n,
   } = useTranslation();


    const locale =
        i18n.resolvedLanguage === 'fa'
            ? 'fa-IR'
            : 'en-US';


   /* ---------------------------------------------------------------------- */
   /*                               Product                                  */
   /* ---------------------------------------------------------------------- */

    const {
        data: product,
        isLoading,
        isError,
        error,
        refetch,
   } = useProduct(id);


   /* ---------------------------------------------------------------------- */
   /*                               Loading                                  */
   /* ---------------------------------------------------------------------- */

    if (isLoading) {
        return (
            <ProductDetailSkeleton/>
        );
   }


   /* ---------------------------------------------------------------------- */
   /*                                 Error                                  */
   /* ---------------------------------------------------------------------- */

    if (
        isError ||
        !product
    ) {
        return (
            <Stack spacing={2}>
                <Alert
                    severity="error"
                    action={
                        <Button
                            color="inherit"
                            size="small"
                            onClick={() =>
                                refetch()
                           }
                        >
                            {t(
                                'productAdminDetail.retry',
                            )}
                        </Button>
                   }
                >
                    {error instanceof Error
                        ? error.message
                        : t(
                            'productAdminDetail.loadError',
                        )}
                </Alert>

                <Button
                    component={Link}
                    to="/admin/products"
                    startIcon={
                        <ArrowLeft/>
                   }
                    variant="outlined"
                    sx={{
                        alignSelf:
                            'flex-start',
                   }}
                >
                    {t(
                        'productAdminDetail.actions.backToProducts',
                    )}
                </Button>
            </Stack>
        );
   }


   /* ---------------------------------------------------------------------- */
   /*                              Derived Data                               */
   /* ---------------------------------------------------------------------- */

    const mainImage =
        product.images?.find(
            image =>
                image.isMain,
        )?.imageUrl ??
        product.images?.[0]
            ?.imageUrl ??
        '/placeholder.jpg';


    const variants =
        product.variants ??
        [];


   /*
     * Backend's ProductDto calculates stock from active variants only.
     *
     * Keep the admin detail page consistent with that behavior.
     */
    const totalStock =
        variants
            .filter(
                variant =>
                    variant.isActive,
            )
            .reduce(
                (
                    sum,
                    variant,
                ) =>
                    sum +
                    variant.stockQuantity,
                0,
            );


    const variantCount =
        variants.length;


    const activeVariantCount =
        variants.filter(
            variant =>
                variant.isActive,
        ).length;


    const finalPrice =
        product.finalPrice ??
        product.price;


    const hasComparePrice =
        product.comparePrice != null &&
        product.comparePrice >
            finalPrice;


   /* ---------------------------------------------------------------------- */
   /*                                   UI                                   */
   /* ---------------------------------------------------------------------- */

    return (
        <Stack spacing={3}>

            {/* ================================================================ */}
            {/* Header                                                           */}
            {/* ================================================================ */}

            <Stack
                direction={{
                    xs: 'column',
                    sm: 'row',
               }}
                spacing={2}
                sx={{
                    justifyContent:
                        'space-between',
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
                        {t(
                            'productAdminDetail.fields.sku',
                        )}
                        : {product.sku}
                    </Typography>
                </Box>

                <Stack
                    direction="row"
                    spacing={1}
                    sx={{
                        flexWrap: 'wrap',
                   }}
                    useFlexGap
                >
                    <Button
                        variant="outlined"
                        startIcon={
                            <ArrowLeft/>
                       }
                        onClick={() =>
                            navigate(
                                '/admin/products',
                            )
                       }
                    >
                        {t(
                            'productAdminDetail.actions.back',
                        )}
                    </Button>

                    <Button
                        variant="contained"
                        startIcon={
                            <Edit/>
                       }
                        onClick={() =>
                            navigate(
                               `/admin/products/${product.id}/edit`,
                            )
                       }
                    >
{
    t(
                            'productAdminDetail.actions.editProduct',
    )
}
                    </Button >
                </Stack >
            </Stack >


    {/* ================================================================ */}
{/* Main Product Information                                         */}
{/* ================================================================ */}

<Card>
    <CardContent>
        <Grid
            container
            spacing={4}
        >

            {/* ==================================================== */}
            {/* Images                                               */}
            {/* ==================================================== */}

            <Grid
                size={{
                    xs: 12,
                    md: 5,
               }}
            >
                <Box
                    component="img"
                    src={resolveMediaUrl(
                        mainImage,
                    )}
                    alt={
                        product.name
                   }
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
                    onError={event => {
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
                                image => (
                                    <Box
                                        key={
                                            image.id
                                       }
                                        component="img"
                                        src={resolveMediaUrl(
                                            image.imageUrl,
                                        )}
                                        alt={
                                            image.altText ??
                                            product.name
                                       }
                                        sx={{
                                            width: 72,
                                            height: 72,
                                            flexShrink: 0,
                                            objectFit:
                                                'cover',
                                            borderRadius: 2,
                                            border:
                                                '1px solid',
                                            borderColor:
                                                image.isMain
                                                    ? 'primary.main'
                                                    : 'divider',
                                       }}
                                   />
                                ),
                            )}
                        </Stack>
                    )}
            </Grid>


            {/* ==================================================== */}
            {/* Product Details                                      */}
            {/* ==================================================== */}

            <Grid
                size={{
                    xs: 12,
                    md: 7,
               }}
            >
                <Stack spacing={2.5}>

                    {/* -------------------------------------------- */}
                    {/* Status                                       */}
                    {/* -------------------------------------------- */}

                    <Stack
                        direction="row"
                        spacing={1}
                        useFlexGap
                        sx={{
                            flexWrap:
                                'wrap',
                       }}
                    >
                        <StatusChip
                            label={
                                product.isActive
                                    ? t(
                                        'productAdminDetail.status.active',
                                    )
                                    : t(
                                        'productAdminDetail.status.inactive',
                                    )
                           }
                            active={
                                product.isActive
                           }
                       />

                        <StatusChip
                            label={
                                product.isPublished
                                    ? t(
                                        'productAdminDetail.status.published',
                                    )
                                    : t(
                                        'productAdminDetail.status.unpublished',
                                    )
                           }
                            active={
                                product.isPublished
                           }
                       />

                        <StatusChip
                            label={
                                product.isFeatured
                                    ? t(
                                        'productAdminDetail.status.featured',
                                    )
                                    : t(
                                        'productAdminDetail.status.notFeatured',
                                    )
                           }
                            active={
                                product.isFeatured
                           }
                       />

                        <StatusChip
                            label={
                                product.isInStock
                                    ? t(
                                        'productAdminDetail.status.inStock',
                                    )
                                    : t(
                                        'productAdminDetail.status.outOfStock',
                                    )
                           }
                            active={
                                product.isInStock
                           }
                       />
                    </Stack>


                    {/* -------------------------------------------- */}
                    {/* Brand                                        */}
                    {/* -------------------------------------------- */}

                    {product.brandName && (
                        <Typography
                            color="text.secondary"
                        >
                            {t(
                                'productAdminDetail.fields.brand',
                            )}
                            :{' '}
                            <strong>
                                {
                                    product.brandName
                               }
                            </strong>
                        </Typography>
                    )}


                    {/* -------------------------------------------- */}
                    {/* Final Price                                  */}
                    {/* -------------------------------------------- */}

                    <Typography
                        variant="h4"
                        color="primary"
                        sx={{
                            fontWeight: 900,
                       }}
                    >
                        {formatPrice(
                            finalPrice,
                            product.currency,
                            locale,
                        )}
                    </Typography>


                    {/* -------------------------------------------- */}
                    {/* Compare Price                                */}
                    {/* -------------------------------------------- */}

                    {hasComparePrice && (
                        <Typography
                            color="text.secondary"
                            sx={{
                                textDecoration:
                                    'line-through',
                           }}
                        >
                            {formatPrice(
                                product.comparePrice!,
                                product.currency,
                                locale,
                            )}
                        </Typography>
                    )}


                    <Divider/>


                    {/* -------------------------------------------- */}
                    {/* Price/Stock/Variants                    */}
                    {/* -------------------------------------------- */}

                    <Grid
                        container
                        spacing={2}
                    >
                        <Grid
                            size={{
                                xs: 12,
                                sm: 4,
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
                                        {t(
                                            'productAdminDetail.fields.basePrice',
                                        )}
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
                                            locale,
                                        )}
                                    </Typography>
                                </CardContent>
                            </Card>
                        </Grid>

                        <Grid
                            size={{
                                xs: 12,
                                sm: 4,
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
                                        {t(
                                            'productAdminDetail.fields.totalStock',
                                        )}
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

                        <Grid
                            size={{
                                xs: 12,
                                sm: 4,
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
                                        {t(
                                            'productAdminDetail.variants.title',
                                        )}
                                    </Typography>

                                    <Typography
                                        variant="h6"
                                        sx={{
                                            fontWeight: 800,
                                       }}
                                    >
                                        {
                                            variantCount
                                       }
                                    </Typography>

                                    {activeVariantCount !==
                                        variantCount && (
                                            <Typography
                                                variant="caption"
                                                color="text.secondary"
                                            >
                                                {
                                                    activeVariantCount
                                               }{' '}
                                               /{' '}
                                                {
                                                    variantCount
                                               }{' '}
                                                {
                                                    t(
                                                        'productAdminDetail.status.active',
                                                    )
                                               }
                                            </Typography>
                                        )}
                                </CardContent>
                            </Card>
                        </Grid>
                    </Grid>


                    {/* -------------------------------------------- */}
                    {/* Short Description                            */}
                    {/* -------------------------------------------- */}

                    {product.shortDescription && (
                        <Box>
                            <Typography
                                variant="h6"
                                sx={{
                                    fontWeight: 800,
                                    mb: 1,
                               }}
                            >
                                {t(
                                    'productAdminDetail.fields.shortDescription',
                                )}
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


                    {/* -------------------------------------------- */}
                    {/* Description                                  */}
                    {/* -------------------------------------------- */}

                    {product.description && (
                        <Box>
                            <Typography
                                variant="h6"
                                sx={{
                                    fontWeight: 800,
                                    mb: 1,
                               }}
                            >
                                {t(
                                    'productAdminDetail.fields.description',
                                )}
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


                    {/* -------------------------------------------- */}
                    {/* Categories                                   */}
                    {/* -------------------------------------------- */}

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
                                    {t(
                                        'productAdminDetail.fields.categories',
                                    )}
                                </Typography>

                                <Stack
                                    direction="row"
                                    spacing={1}
                                    useFlexGap
                                    sx={{
                                        flexWrap:
                                            'wrap',
                                   }}
                                >
                                    {product.categories.map(
                                        category => (
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


{/* ================================================================ */}
{/* Variants                                                         */}
{/* ================================================================ */}

<Card>
    <CardHeader
        avatar={
            <Package/>
       }
        title={t(
            'productAdminDetail.variants.title',
        )}
        subheader={t(
            variantCount === 1
                ? 'productAdminDetail.variants.count_one'
                : 'productAdminDetail.variants.count_other',
            {
                count:
                    variantCount,
           },
        )}
   />

    <CardContent>
        {variants.length ===
            0 ? (
            <Alert severity="info">
                {t(
                    'productAdminDetail.variants.empty',
                )}
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
                                {t(
                                    'productAdminDetail.variants.skuAttributes',
                                )}
                            </TableCell>

                            <TableCell>
                                {t(
                                    'productAdminDetail.variants.price',
                                )}
                            </TableCell>

                            <TableCell>
                                {t(
                                    'productAdminDetail.variants.stock',
                                )}
                            </TableCell>

                            <TableCell>
                                {t(
                                    'productAdminDetail.variants.status',
                                )}
                            </TableCell>

                        </TableRow>
                    </TableHead>

                    <TableBody>
                        {variants.map(
                            variant => (
                                <VariantSummary
                                    key={
                                        variant.id
                                   }
                                    variant={
                                        variant
                                   }
                                    currency={
                                        product.currency
                                   }
                                    locale={
                                        locale
                                   }
                                    t={
                                        t
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

