import {
    useEffect,
    useMemo,
    useRef,
    useState,
} from 'react';

import {
    Alert,
    Box,
    Button,
    Card,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    FormControl,
    InputLabel,
    MenuItem,
    Select,
    Stack,
    Typography,
} from '@mui/material';

import {
    ShoppingCartOutlined,
} from '@mui/icons-material';

import {
    Link,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    resolveMediaUrl,
} from '@/lib/media-url';

import {
    productsApi,
    type Product,
    type ProductVariant,
    type ProductVariantAttribute,
} from '../api/products';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';

interface CartUpsellDialogProps {
    open: boolean;
    sourceProductId: string | null;
    onClose: () => void;
}

type RecommendedProduct = Product & {
    availableVariants: ProductVariant[];
};

function isVariantDefiningAttribute(
    attribute: ProductVariantAttribute,
): boolean {
    const roleValue = Number(
        attribute.roleValue,
    );

    if (Number.isInteger(roleValue)) {
        return (
            (roleValue & 4) === 4
        );
    }

    const normalizedRole =
        attribute.role
            ?.trim()
            .toLowerCase()
            .replace(
                /[\s_-]/g,
                '',
            );

    if (normalizedRole) {
        return (
            normalizedRole ===
            'variantdefining' ||
            normalizedRole.includes(
                'variantdefining',
            )
        );
    }

    // Legacy records without a role
    // are treated as variant-defining.
    return true;
}

function getVariantLabel(
    variant: ProductVariant,
): string {
    const values = (
        variant.attributes ?? []
    )
        .filter(
            isVariantDefiningAttribute,
        )
        .map(
            attribute =>
                attribute.displayValue ||
                attribute.value,
        )
        .filter(Boolean);

    return values.length > 0
        ? values.join(' / ')
        : variant.sku;
}

function getAvailableVariants(
    product: Product,
): ProductVariant[] {
    return (
        product.variants ?? []
    ).filter(
        variant =>
            variant.isActive &&
            variant.stockQuantity > 0,
    );
}

export default function CartUpsellDialog({
    open,
    sourceProductId,
    onClose,
}: CartUpsellDialogProps) {
    const {
        t,
        i18n,
    } = useTranslation();

    const {
        add,
    } = useCartMutations();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa') ?? false;

    const isRtl =
        /^(fa|ar)(-|$)/i.test(
            i18n.language ?? '',
        );

    const [
        recommendations,
        setRecommendations,
    ] = useState<RecommendedProduct[]>(
        [],
    );

    const [
        selectedVariants,
        setSelectedVariants,
    ] = useState<
        Record<string, string>
    >({});

    const [
        loading,
        setLoading,
    ] = useState(false);

    const [
        actionError,
        setActionError,
    ] = useState<string | null>(
        null,
    );

    const [
        addingProductId,
        setAddingProductId,
    ] = useState<string | null>(
        null,
    );

    const onCloseRef = useRef(
        onClose,
    );

    useEffect(() => {
        onCloseRef.current = onClose;
    }, [onClose]);

    useEffect(() => {
        if (
            !open ||
            !sourceProductId
        ) {
            setRecommendations([]);
            setSelectedVariants({});
            setActionError(null);
            setLoading(false);
            setAddingProductId(null);

            return;
        }

        let active = true;

        setLoading(true);
        setActionError(null);
        setRecommendations([]);
        setSelectedVariants({});
        setAddingProductId(null);

        productsApi
            .getRecommendations(
                sourceProductId,
            )
            .then(result => {
                if (!active) {
                    return;
                }

                const available = (
                    result ?? []
                )
                    .filter(
                        item =>
                            item.id !==
                            sourceProductId,
                    )
                    .map(product => ({
                        ...product,

                        availableVariants:
                            getAvailableVariants(
                                product,
                            ),
                    }))
                    .filter(
                        product =>
                            product
                                .availableVariants
                                .length > 0,
                    )
                    .slice(0, 3);

                if (
                    available.length === 0
                ) {
                    onCloseRef.current();
                    return;
                }

                setRecommendations(
                    available,
                );

                setSelectedVariants(
                    Object.fromEntries(
                        available.map(
                            product => [
                                product.id,
                                product
                                    .availableVariants[0]
                                    .id,
                            ],
                        ),
                    ),
                );
            })
            .catch(() => {
                // Recommendation failure must not
                // interrupt the completed add-to-cart action.
                if (active) {
                    onCloseRef.current();
                }
            })
            .finally(() => {
                if (active) {
                    setLoading(false);
                }
            });

        return () => {
            active = false;
        };
    }, [
        open,
        sourceProductId,
    ]);

    const numberFormatter = useMemo(
        () =>
            new Intl.NumberFormat(
                isFa
                    ? 'fa-IR'
                    : (
                        i18n.language ||
                        'en-US'
                    ),
                {
                    maximumFractionDigits: 0,
                },
            ),
        [
            i18n.language,
            isFa,
        ],
    );

    const handleClose = () => {
        if (add.isPending) {
            return;
        }

        setActionError(null);
        onCloseRef.current();
    };

    const handleAddRecommendation =
        async (
            product: RecommendedProduct,
            variant:
                | ProductVariant
                | undefined,
        ) => {
            if (
                !variant?.id ||
                add.isPending ||
                addingProductId
            ) {
                return;
            }

            setActionError(null);
            setAddingProductId(
                product.id,
            );

            try {
                await add.mutateAsync({
                    productVariantId:
                        variant.id,

                    quantity: 1,
                });

                const remaining =
                    recommendations.filter(
                        item =>
                            item.id !==
                            product.id,
                    );

                setRecommendations(
                    remaining,
                );

                if (
                    remaining.length === 0
                ) {
                    onCloseRef.current();
                }
            } catch {
                setActionError(
                    t(
                        'cartUpsell.addFailed',
                    ),
                );
            } finally {
                setAddingProductId(null);
            }
        };

    return (
        <Dialog
            open={open}
            onClose={
                add.isPending
                    ? undefined
                    : handleClose
            }
            fullWidth
            maxWidth="md"
            scroll="paper"
            dir={
                isRtl
                    ? 'rtl'
                    : 'ltr'
            }
            slotProps={{
                paper: {
                    sx: {
                        display: 'flex',
                        flexDirection: 'column',
                        overflow: 'hidden',

                        maxHeight: {
                            xs: 'calc(100dvh - 16px)',
                            sm: 'calc(100dvh - 64px)',
                        },

                        m: {
                            xs: 1,
                            sm: 2,
                        },

                        borderRadius: {
                            xs: 3,
                            sm: 4,
                        },
                    },
                },
            }}
        >
            <DialogTitle
                sx={{
                    flexShrink: 0,

                    px: {
                        xs: 2,
                        sm: 3,
                    },

                    py: {
                        xs: 1.5,
                        sm: 2.5,
                    },

                    textAlign: isRtl
                        ? 'right'
                        : 'left',
                }}
            >
                <Typography
                    component="div"
                    variant="h6"
                    sx={{
                        fontWeight: 900,

                        fontSize: {
                            xs: '1.05rem',
                            sm: '1.25rem',
                        },

                        lineHeight: 1.6,
                    }}
                >
                    {t(
                        'cartUpsell.title',
                    )}
                </Typography>

                <Typography
                    component="div"
                    variant="body2"
                    color="text.secondary"
                    sx={{
                        mt: 0.5,
                        lineHeight: 1.8,
                    }}
                >
                    {t(
                        'cartUpsell.subtitle',
                    )}
                </Typography>
            </DialogTitle>

            <DialogContent
                dividers
                sx={{
                    flex: '1 1 auto',
                    minHeight: 0,

                    overflowY: 'auto',
                    overscrollBehavior:
                        'contain',

                    WebkitOverflowScrolling:
                        'touch',

                    px: {
                        xs: 1.5,
                        sm: 3,
                    },

                    py: {
                        xs: 1.5,
                        sm: 2,
                    },
                }}
            >
                {loading ? (
                    <Box
                        sx={{
                            display: 'flex',
                            flexDirection: 'column',

                            alignItems: 'center',
                            justifyContent: 'center',

                            gap: 2,

                            minHeight: {
                                xs: 145,
                                sm: 180,
                            },

                            py: 3,
                        }}
                    >
                        <CircularProgress
                            size={28}
                        />

                        <Typography
                            variant="body2"
                            color="text.secondary"
                        >
                            {t(
                                'cartUpsell.loading',
                            )}
                        </Typography>
                    </Box>
                ) : (
                    <Stack
                        spacing={{
                            xs: 1.5,
                            sm: 2,
                        }}
                    >
                        <Typography
                            variant="subtitle1"
                            sx={{
                                fontWeight: 800,

                                textAlign:
                                    isRtl
                                        ? 'right'
                                        : 'left',
                            }}
                        >
                            {t(
                                'cartUpsell.recommendations',
                            )}
                        </Typography>

                        {actionError && (
                            <Alert
                                severity="error"
                                onClose={() =>
                                    setActionError(
                                        null,
                                    )
                                }
                            >
                                {actionError}
                            </Alert>
                        )}

                        {recommendations.map(
                            product => {
                                const selectedVariantId =
                                    selectedVariants[
                                    product.id
                                    ] ??
                                    product
                                        .availableVariants[0]
                                        ?.id ??
                                    '';

                                const selectedVariant =
                                    product
                                        .availableVariants
                                        .find(
                                            variant =>
                                                variant.id ===
                                                selectedVariantId,
                                        ) ??
                                    product
                                        .availableVariants[0];

                                const overridePrice =
                                    Number(
                                        selectedVariant
                                            ?.priceOverride ??
                                        0,
                                    );

                                const basePrice =
                                    overridePrice > 0
                                        ? overridePrice
                                        : product.price;

                                const discount =
                                    Math.min(
                                        100,
                                        Math.max(
                                            0,
                                            Number(
                                                product
                                                    .discountPercentage ??
                                                0,
                                            ),
                                        ),
                                    );

                                const displayPrice =
                                    basePrice -
                                    (
                                        basePrice *
                                        discount /
                                        100
                                    );

                                const imageUrl =
                                    product.images
                                        ?.find(
                                            image =>
                                                image.isMain,
                                        )
                                        ?.imageUrl ??
                                    product.images
                                        ?.[0]
                                        ?.imageUrl ??
                                    '/placeholder.jpg';

                                const isAdding =
                                    addingProductId ===
                                    product.id;

                                const hasMultipleVariants =
                                    product
                                        .availableVariants
                                        .length > 1;

                                const variantLabelId =
                                    `cart-upsell-variant-${product.id}`;

                                return (
                                    <Card
                                        key={product.id}
                                        variant="outlined"
                                        sx={{
                                            p: {
                                                xs: 1.25,
                                                sm: 1.75,
                                            },

                                            minWidth: 0,
                                            borderRadius: 3,

                                            transition:
                                                'border-color 160ms ease',

                                            '&:hover': {
                                                borderColor:
                                                    'primary.main',
                                            },
                                        }}
                                    >
                                        <Box
                                            sx={{
                                                display: 'grid',

                                                gridTemplateColumns: {
                                                    xs: '80px minmax(0, 1fr)',
                                                    sm: '104px minmax(0, 1fr) auto',
                                                },

                                                gridTemplateRows: {
                                                    xs: 'auto auto',
                                                    sm: 'auto',
                                                },

                                                alignItems: 'center',

                                                gap: {
                                                    xs: 1.25,
                                                    sm: 2,
                                                },

                                                minWidth: 0,
                                            }}
                                        >
                                            <Box
                                                component="img"
                                                src={resolveMediaUrl(
                                                    imageUrl,
                                                )}
                                                alt={
                                                    product.name
                                                }
                                                loading="lazy"
                                                onError={event => {
                                                    if (
                                                        !event
                                                            .currentTarget
                                                            .src
                                                            .endsWith(
                                                                '/placeholder.jpg',
                                                            )
                                                    ) {
                                                        event
                                                            .currentTarget
                                                            .src =
                                                            '/placeholder.jpg';
                                                    }
                                                }}
                                                sx={{
                                                    gridColumn: 1,
                                                    gridRow: 1,

                                                    display: 'block',

                                                    width: '100%',

                                                    height: {
                                                        xs: 92,
                                                        sm: 112,
                                                    },

                                                    objectFit:
                                                        'contain',

                                                    borderRadius: 2,

                                                    bgcolor:
                                                        'background.default',
                                                }}
                                            />

                                            <Box
                                                sx={{
                                                    gridColumn: 2,
                                                    gridRow: 1,

                                                    minWidth: 0,

                                                    textAlign:
                                                        isRtl
                                                            ? 'right'
                                                            : 'left',
                                                }}
                                            >
                                                <Typography
                                                    component={Link}
                                                    to={`/products/${encodeURIComponent(
                                                        product.slug,
                                                    )}`}
                                                    onClick={
                                                        handleClose
                                                    }
                                                    sx={{
                                                        color: 'text.primary',

                                                        display:
                                                            '-webkit-box',

                                                        WebkitLineClamp: 2,
                                                        WebkitBoxOrient:
                                                            'vertical',

                                                        overflow:
                                                            'hidden',

                                                        overflowWrap:
                                                            'anywhere',

                                                        fontSize: {
                                                            xs: '0.875rem',
                                                            sm: '1rem',
                                                        },

                                                        lineHeight: 1.65,
                                                        fontWeight: 800,

                                                        textDecoration:
                                                            'none',

                                                        '&:hover': {
                                                            color: 'primary.main',
                                                        },

                                                        '&:focus-visible': {
                                                            outline:
                                                                '2px solid',
                                                            outlineColor:
                                                                'primary.main',
                                                            outlineOffset: 2,
                                                            borderRadius: 1,
                                                        },
                                                    }}
                                                >
                                                    {product.name}
                                                </Typography>

                                                <Typography
                                                    color="primary"
                                                    sx={{
                                                        fontWeight: 900,

                                                        fontSize: {
                                                            xs: '0.9rem',
                                                            sm: '1rem',
                                                        },

                                                        lineHeight: 1.8,
                                                        my: 0.5,

                                                        overflowWrap:
                                                            'anywhere',
                                                    }}
                                                >
                                                    {numberFormatter.format(
                                                        displayPrice,
                                                    )}{' '}
                                                    {
                                                        product.currency
                                                    }
                                                </Typography>

                                                {hasMultipleVariants ? (
                                                    <FormControl
                                                        fullWidth
                                                        size="small"
                                                        sx={{
                                                            mt: 0.5,
                                                            minWidth: 0,
                                                        }}
                                                    >
                                                        <InputLabel
                                                            id={
                                                                variantLabelId
                                                            }
                                                        >
                                                            {t(
                                                                'cartUpsell.selectVariant',
                                                            )}
                                                        </InputLabel>

                                                        <Select
                                                            labelId={
                                                                variantLabelId
                                                            }
                                                            label={t(
                                                                'cartUpsell.selectVariant',
                                                            )}
                                                            value={
                                                                selectedVariantId
                                                            }
                                                            onChange={event => {
                                                                setSelectedVariants(
                                                                    current => ({
                                                                        ...current,

                                                                        [product.id]:
                                                                            String(
                                                                                event
                                                                                    .target
                                                                                    .value,
                                                                            ),
                                                                    }),
                                                                );
                                                            }}
                                                            sx={{
                                                                minHeight: 42,

                                                                fontSize: {
                                                                    xs: '0.8rem',
                                                                    sm: '0.875rem',
                                                                },

                                                                textAlign:
                                                                    isRtl
                                                                        ? 'right'
                                                                        : 'left',
                                                            }}
                                                        >
                                                            {product
                                                                .availableVariants
                                                                .map(
                                                                    variant => (
                                                                        <MenuItem
                                                                            key={
                                                                                variant.id
                                                                            }
                                                                            value={
                                                                                variant.id
                                                                            }
                                                                            sx={{
                                                                                whiteSpace:
                                                                                    'normal',
                                                                            }}
                                                                        >
                                                                            {getVariantLabel(
                                                                                variant,
                                                                            )}
                                                                        </MenuItem>
                                                                    ),
                                                                )}
                                                        </Select>
                                                    </FormControl>
                                                ) : (
                                                    <Typography
                                                        variant="caption"
                                                        color="text.secondary"
                                                        sx={{
                                                            display:
                                                                'block',

                                                            mt: 0.5,

                                                            overflowWrap:
                                                                'anywhere',

                                                            lineHeight: 1.7,
                                                        }}
                                                    >
                                                        {getVariantLabel(
                                                            product
                                                                .availableVariants[0],
                                                        )}
                                                    </Typography>
                                                )}
                                            </Box>

                                            <Button
                                                variant="contained"
                                                startIcon={
                                                    <ShoppingCartOutlined
                                                        fontSize="small"
                                                    />
                                                }
                                                disabled={
                                                    !selectedVariant?.id ||
                                                    add.isPending
                                                }
                                                onClick={() =>
                                                    void handleAddRecommendation(
                                                        product,
                                                        selectedVariant,
                                                    )
                                                }
                                                sx={{
                                                    gridColumn: {
                                                        xs: '1 / -1',
                                                        sm: 3,
                                                    },

                                                    gridRow: {
                                                        xs: 2,
                                                        sm: 1,
                                                    },

                                                    width: {
                                                        xs: '100%',
                                                        sm: 'auto',
                                                    },

                                                    minWidth: {
                                                        xs: 0,
                                                        sm: 140,
                                                    },

                                                    minHeight: 44,

                                                    px: {
                                                        xs: 1.5,
                                                        sm: 2,
                                                    },

                                                    flexShrink: 0,

                                                    fontWeight: 800,

                                                    fontSize: {
                                                        xs: '0.82rem',
                                                        sm: '0.875rem',
                                                    },

                                                    whiteSpace: 'nowrap',
                                                }}
                                            >
                                                {isAdding
                                                    ? t(
                                                        'cartUpsell.adding',
                                                    )
                                                    : t(
                                                        'cartUpsell.addToCart',
                                                    )}
                                            </Button>
                                        </Box>
                                    </Card>
                                );
                            },
                        )}
                    </Stack>
                )}
            </DialogContent>

            <DialogActions
                sx={{
                    flexShrink: 0,

                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',

                    gap: 1,

                    px: {
                        xs: 1.5,
                        sm: 3,
                    },

                    py: {
                        xs: 1.25,
                        sm: 2,
                    },

                    pb: {
                        xs: 'calc(10px + env(safe-area-inset-bottom))',
                        sm: 2,
                    },

                    bgcolor: 'background.paper',
                }}
            >
                <Button
                    onClick={handleClose}
                    disabled={add.isPending}
                    sx={{
                        flex: {
                            xs: 1,
                            sm: 'initial',
                        },

                        minWidth: 0,
                        minHeight: 42,

                        px: {
                            xs: 1,
                            sm: 2,
                        },

                        fontSize: {
                            xs: '0.8rem',
                            sm: '0.875rem',
                        },

                        whiteSpace: {
                            xs: 'normal',
                            sm: 'nowrap',
                        },

                        lineHeight: 1.5,
                    }}
                >
                    {t(
                        'cartUpsell.continueShopping',
                    )}
                </Button>

                <Button
                    component={Link}
                    to="/cart"
                    variant="contained"
                    startIcon={
                        <ShoppingCartOutlined
                            fontSize="small"
                        />
                    }
                    onClick={handleClose}
                    disabled={add.isPending}
                    sx={{
                        flex: {
                            xs: 1,
                            sm: 'initial',
                        },

                        minWidth: 0,
                        minHeight: 42,

                        px: {
                            xs: 1,
                            sm: 2,
                        },

                        fontWeight: 800,

                        fontSize: {
                            xs: '0.8rem',
                            sm: '0.875rem',
                        },

                        whiteSpace: {
                            xs: 'normal',
                            sm: 'nowrap',
                        },

                        lineHeight: 1.5,
                    }}
                >
                    {t(
                        'cartUpsell.viewCart',
                    )}
                </Button>
            </DialogActions>
        </Dialog>
    );
}