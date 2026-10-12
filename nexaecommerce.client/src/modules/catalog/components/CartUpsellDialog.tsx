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

import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { resolveMediaUrl } from '@/lib/media-url';
import {
    productsApi,
    type Product,
    type ProductVariant,
    type ProductVariantAttribute,
} from '../api/products';
import { useCartMutations } from '@/modules/cart/hooks/useCartMutations';

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
    const roleValue = Number(attribute.roleValue);

    if (Number.isInteger(roleValue)) {
        return (roleValue & 4) === 4;
    }

    const normalizedRole = attribute.role
        ?.trim()
        .toLowerCase()
        .replace(/[\s_-]/g, '');

    if (normalizedRole) {
        return normalizedRole === 'variantdefining' ||
            normalizedRole.includes('variantdefining');
    }

    // Legacy records without a role are treated as variant-defining.
    return true;
}

function getVariantLabel(variant: ProductVariant): string {
    const values = (variant.attributes ?? [])
        .filter(isVariantDefiningAttribute)
        .map(attribute => attribute.displayValue || attribute.value)
        .filter(Boolean);

    return values.length > 0 ? values.join(' / ') : variant.sku;
}

function getAvailableVariants(product: Product): ProductVariant[] {
    return (product.variants ?? []).filter(
        variant => variant.isActive && variant.stockQuantity > 0,
    );
}

export default function CartUpsellDialog({
    open,
    sourceProductId,
    onClose,
}: CartUpsellDialogProps) {
    const { t, i18n } = useTranslation();
    const { add } = useCartMutations();
    const isFa = i18n.language?.toLowerCase().startsWith('fa') ?? false;
    const isRtl = /^(fa|ar)(-|$)/i.test(i18n.language ?? '');

    const [recommendations, setRecommendations] = useState<RecommendedProduct[]>([]);
    const [selectedVariants, setSelectedVariants] = useState<Record<string, string>>({});
    const [loading, setLoading] = useState(false);
    const [actionError, setActionError] = useState<string | null>(null);
    const onCloseRef = useRef(onClose);

    useEffect(() => {
        onCloseRef.current = onClose;
    }, [onClose]);

    useEffect(() => {
        if (!open || !sourceProductId) {
            setRecommendations([]);
            setSelectedVariants({});
            setActionError(null);
            setLoading(false);
            return;
        }

        let active = true;
        setLoading(true);
        setActionError(null);
        setRecommendations([]);
        setSelectedVariants({});

        productsApi.getRecommendations(sourceProductId)
            .then(result => {
                if (!active) return;

                const available = (result ?? [])
                    .filter(item => item.id !== sourceProductId)
                    .map(product => ({
                        ...product,
                        availableVariants: getAvailableVariants(product),
                    }))
                    .filter(product => product.availableVariants.length > 0)
                    .slice(0, 3);

                if (available.length === 0) {
                    onCloseRef.current();
                    return;
                }

                setRecommendations(available);
                setSelectedVariants(
                    Object.fromEntries(
                        available.map(product => [
                            product.id,
                            product.availableVariants[0].id,
                        ]),
                    ),
                );
            })
            .catch(() => {
                // Recommendation failure must never interrupt the completed add-to-cart action.
                if (active) onCloseRef.current();
            })
            .finally(() => {
                if (active) setLoading(false);
            });

        return () => {
            active = false;
        };
    }, [open, sourceProductId]);

    const numberFormatter = useMemo(
        () => new Intl.NumberFormat(
            isFa ? 'fa-IR' : (i18n.language || 'en-US'),
            { maximumFractionDigits: 0 },
        ),
        [i18n.language, isFa],
    );

    const handleClose = () => {
        setActionError(null);
        onCloseRef.current();
    };

    const handleAddRecommendation = async (
        product: RecommendedProduct,
        variant: ProductVariant | undefined,
    ) => {
        if (!variant?.id || add.isPending) return;

        setActionError(null);

        try {
            await add.mutateAsync({
                productVariantId: variant.id,
                quantity: 1,
            });

            const remaining = recommendations.filter(
                item => item.id !== product.id,
            );
            setRecommendations(remaining);

            if (remaining.length === 0) {
                onCloseRef.current();
            }
        } catch {
            setActionError(t('cartUpsell.addFailed'));
        }
    };

    return (
        <Dialog
            open={open}
            onClose={add.isPending ? undefined : handleClose}
            fullWidth
            maxWidth="md"
            dir={isRtl ? 'rtl' : 'ltr'}
            sx={{
                '& .MuiDialog-paper': {
                    borderRadius: { xs: 3, sm: 4 },
                    m: { xs: 1, sm: 2 },
                },
            }}
        >
            <DialogTitle sx={{ textAlign: isRtl ? 'right' : 'left' }}>
                <Typography component="div" variant="h6" sx={{ fontWeight: 900 }}>
                    {t('cartUpsell.title')}
                </Typography>
                <Typography component="div" variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                    {t('cartUpsell.subtitle')}
                </Typography>
            </DialogTitle>

            <DialogContent dividers>
                {loading ? (
                    <Box
                        sx={{
                            display: 'flex',
                            flexDirection: 'column',
                            alignItems: 'center',
                            justifyContent: 'center',
                            gap: 2,
                            minHeight: 180,
                            py: 4,
                        }}
                    >
                        <CircularProgress size={28} />
                        <Typography color="text.secondary">
                            {t('cartUpsell.loading')}
                        </Typography>
                    </Box>
                ) : (
                    <Stack spacing={2}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 800 }}>
                            {t('cartUpsell.recommendations')}
                        </Typography>

                        {actionError && (
                            <Alert severity="error" onClose={() => setActionError(null)}>
                                {actionError}
                            </Alert>
                        )}

                        {recommendations.map(product => {
                            const selectedVariantId =
                                selectedVariants[product.id] ?? product.availableVariants[0]?.id ?? '';
                            const selectedVariant = product.availableVariants.find(
                                variant => variant.id === selectedVariantId,
                            ) ?? product.availableVariants[0];

                            const overridePrice = Number(selectedVariant?.priceOverride ?? 0);
                            const basePrice = overridePrice > 0 ? overridePrice : product.price;
                            const discount = Math.min(
                                100,
                                Math.max(0, Number(product.discountPercentage ?? 0)),
                            );
                            const displayPrice = basePrice - (basePrice * discount / 100);
                            const imageUrl = product.images?.find(image => image.isMain)?.imageUrl
                                ?? product.images?.[0]?.imageUrl
                                ?? '/placeholder.jpg';

                            return (
                                <Card key={product.id} variant="outlined" sx={{ p: 2 }}>
                                    <Box
                                        sx={{
                                            display: 'flex',
                                            flexDirection: { xs: 'column', sm: 'row' },
                                            gap: 2,
                                            alignItems: { xs: 'stretch', sm: 'center' },
                                        }}
                                    >
                                        <Box
                                            component="img"
                                            src={resolveMediaUrl(imageUrl)}
                                            alt={product.name}
                                            loading="lazy"
                                            onError={event => {
                                                if (!event.currentTarget.src.endsWith('/placeholder.jpg')) {
                                                    event.currentTarget.src = '/placeholder.jpg';
                                                }
                                            }}
                                            sx={{
                                                width: { xs: '100%', sm: 100 },
                                                height: 104,
                                                objectFit: 'contain',
                                                borderRadius: 2,
                                                bgcolor: 'background.default',
                                            }}
                                        />

                                        <Box sx={{ flex: 1, minWidth: 0, textAlign: isRtl ? 'right' : 'left' }}>
                                            <Typography
                                                component={Link}
                                                to={`/products/${encodeURIComponent(product.slug)}`}
                                                onClick={handleClose}
                                                sx={{
                                                    color: 'text.primary',
                                                    display: 'inline-block',
                                                    fontWeight: 800,
                                                    textDecoration: 'none',
                                                    '&:hover': { color: 'primary.main' },
                                                }}
                                            >
                                                {product.name}
                                            </Typography>

                                            <Typography color="primary" sx={{ fontWeight: 900, my: 1 }}>
                                                {numberFormatter.format(displayPrice)} {product.currency}
                                            </Typography>

                                            {product.availableVariants.length > 1 ? (
                                                <FormControl fullWidth size="small">
                                                    <InputLabel id={`cart-upsell-variant-${product.id}`}>
                                                        {t('cartUpsell.selectVariant')}
                                                    </InputLabel>
                                                    <Select
                                                        labelId={`cart-upsell-variant-${product.id}`}
                                                        label={t('cartUpsell.selectVariant')}
                                                        value={selectedVariantId}
                                                        onChange={event => {
                                                            setSelectedVariants(current => ({
                                                                ...current,
                                                                [product.id]: String(event.target.value),
                                                            }));
                                                        }}
                                                    >
                                                        {product.availableVariants.map(variant => (
                                                            <MenuItem key={variant.id} value={variant.id}>
                                                                {getVariantLabel(variant)}
                                                            </MenuItem>
                                                        ))}
                                                    </Select>
                                                </FormControl>
                                            ) : (
                                                <Typography variant="body2" color="text.secondary">
                                                    {getVariantLabel(product.availableVariants[0])}
                                                </Typography>
                                            )}
                                        </Box>

                                        <Button
                                            variant="contained"
                                            disabled={!selectedVariant?.id || add.isPending}
                                            onClick={() => void handleAddRecommendation(product, selectedVariant)}
                                            sx={{
                                                flexShrink: 0,
                                                minHeight: 44,
                                                fontWeight: 800,
                                                whiteSpace: 'nowrap',
                                            }}
                                        >
                                            {add.isPending
                                                ? t('cartUpsell.adding')
                                                : t('cartUpsell.addToCart')}
                                        </Button>
                                    </Box>
                                </Card>
                            );
                        })}
                    </Stack>
                )}
            </DialogContent>

            <DialogActions sx={{ justifyContent: 'space-between', px: 3, py: 2 }}>
                <Button onClick={handleClose} disabled={add.isPending}>
                    {t('cartUpsell.continueShopping')}
                </Button>
                <Button component={Link} to="/cart" variant="contained" onClick={handleClose}>
                    {t('cartUpsell.viewCart')}
                </Button>
            </DialogActions>
        </Dialog>
    );
}
