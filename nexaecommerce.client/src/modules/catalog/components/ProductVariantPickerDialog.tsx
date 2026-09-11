import {
    useEffect,
    useMemo,
    useState,
} from 'react';

import {
    Button,
    Chip,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    Stack,
    Typography,
} from '@mui/material';

import {
    CheckCircle,
    ShoppingCartOutlined,
} from '@mui/icons-material';

import type {
    Product,
    ProductVariant,
} from '../api/products';

interface ProductVariantPickerDialogProps {
    open: boolean;
    product: Product | null;
    loading?: boolean;
    onClose: () => void;
    onConfirm: (
        variant: ProductVariant,
    ) => void | Promise<void>;
}

type AttributeOption = {
    value: string;
    displayValue: string;
    colorHex?: string | null;
};

function getVariantAttributeOptions(
    product: Product,
): Map<
    string,
    AttributeOption[]
> {
    const result =
        new Map<
            string,
            AttributeOption[]
        >();

    for (const variant of product.variants) {
        if (
            !variant.isActive ||
            variant.stockQuantity <= 0
        ) {
            continue;
        }

        for (const attribute of
            variant.attributes ?? []) {
            const key =
                attribute.attributeCode ||
                attribute.attributeName;

            if (!key) {
                continue;
            }

            const value =
                attribute.value?.trim();

            if (!value) {
                continue;
            }

            const values =
                result.get(key) ?? [];

            if (
                !values.some(
                    item =>
                        item.value ===
                        value,
                )
            ) {
                values.push({
                    value,
                    displayValue:
                        attribute.displayValue?.trim() ||
                        value,
                    colorHex:
                        attribute.colorHex,
                });
            }

            result.set(
                key,
                values,
            );
        }
    }

    const hasColorAttribute =
        Array.from(
            result.keys(),
        ).some(
            key =>
                key.toLowerCase() ===
                'color',
        );

    if (!hasColorAttribute) {
        const colors =
            product.variants
                .filter(
                    variant =>
                        variant.isActive &&
                        variant.stockQuantity >
                        0 &&
                        !!variant.color?.trim(),
                )
                .map(
                    variant =>
                        variant.color!.trim(),
                );

        const uniqueColors =
            Array.from(
                new Set(colors),
            );

        if (
            uniqueColors.length > 0
        ) {
            result.set(
                'color',
                uniqueColors.map(
                    value => ({
                        value,
                        displayValue:
                            value,
                    }),
                ),
            );
        }
    }

    const hasSizeAttribute =
        Array.from(
            result.keys(),
        ).some(
            key =>
                key.toLowerCase() ===
                'size',
        );

    if (!hasSizeAttribute) {
        const sizes =
            product.variants
                .filter(
                    variant =>
                        variant.isActive &&
                        variant.stockQuantity >
                        0 &&
                        !!variant.size?.trim(),
                )
                .map(
                    variant =>
                        variant.size!.trim(),
                );

        const uniqueSizes =
            Array.from(
                new Set(sizes),
            );

        if (
            uniqueSizes.length > 0
        ) {
            result.set(
                'size',
                uniqueSizes.map(
                    value => ({
                        value,
                        displayValue:
                            value,
                    }),
                ),
            );
        }
    }

    return result;
}

function variantMatchesSelection(
    variant: ProductVariant,
    selection: Record<
        string,
        string
    >,
): boolean {
    for (const [
        key,
        selectedValue,
    ] of Object.entries(
        selection,
    )) {
        const attribute =
            variant.attributes?.find(
                item =>
                    (
                        item.attributeCode ||
                        item.attributeName
                    ) === key,
            );

        if (attribute) {
            if (
                attribute.value !==
                selectedValue
            ) {
                return false;
            }

            continue;
        }

        if (
            key.toLowerCase() ===
            'color'
        ) {
            if (
                variant.color?.trim() !==
                selectedValue
            ) {
                return false;
            }

            continue;
        }

        if (
            key.toLowerCase() ===
            'size'
        ) {
            if (
                variant.size?.trim() !==
                selectedValue
            ) {
                return false;
            }
        }
    }

    return true;
}

export default function ProductVariantPickerDialog({
    open,
    product,
    loading = false,
    onClose,
    onConfirm,
}: ProductVariantPickerDialogProps) {
    const [
        selection,
        setSelection,
    ] = useState<
        Record<string, string>
    >({});

    useEffect(() => {
        if (!open) {
            setSelection({});
        }
    }, [open, product?.id]);

    const attributeOptions =
        useMemo(
            () =>
                product
                    ? getVariantAttributeOptions(
                        product,
                    )
                    : new Map<
                        string,
                        AttributeOption[]
                    >(),
            [product],
        );

    const selectedVariant =
        useMemo(() => {
            if (!product) {
                return null;
            }

            const requiredKeys =
                Array.from(
                    attributeOptions.keys(),
                );

            if (
                requiredKeys.length ===
                0
            ) {
                return (
                    product.variants.find(
                        variant =>
                            variant.isActive &&
                            variant.stockQuantity >
                            0,
                    ) ?? null
                );
            }

            if (
                requiredKeys.some(
                    key =>
                        !selection[key],
                )
            ) {
                return null;
            }

            return (
                product.variants.find(
                    variant =>
                        variant.isActive &&
                        variant.stockQuantity >
                        0 &&
                        variantMatchesSelection(
                            variant,
                            selection,
                        ),
                ) ?? null
            );
        }, [
            attributeOptions,
            product,
            selection,
        ]);

    const canConfirm =
        Boolean(selectedVariant);

    const getOptionAvailable = (
        attributeKey: string,
        value: string,
    ) => {
        if (!product) {
            return false;
        }

        const nextSelection = {
            ...selection,
            [attributeKey]:
                value,
        };

        return product.variants.some(
            variant =>
                variant.isActive &&
                variant.stockQuantity >
                0 &&
                variantMatchesSelection(
                    variant,
                    nextSelection,
                ),
        );
    };

    const handleClose = () => {
        setSelection({});
        onClose();
    };

    const handleConfirm = () => {
        if (!selectedVariant) {
            return;
        }

        onConfirm(
            selectedVariant,
        );
    };

    if (!product) {
        return null;
    }

    return (
        <Dialog
            open={open}
            onClose={
                loading
                    ? undefined
                    : handleClose
            }
            fullWidth
            maxWidth="sm"
            dir="rtl"
        >
            <DialogTitle
                sx={{
                    fontWeight: 800,
                    textAlign: 'right',
                }}
            >
                انتخاب ویژگی محصول
            </DialogTitle>

            <DialogContent
                dividers
            >
                <Stack
                    spacing={2.5}
                    sx={{
                        direction:
                            'rtl',
                    }}
                >
                    <Typography
                        variant="h6"
                        sx={{
                            fontWeight:
                                800,
                            textAlign:
                                'right',
                        }}
                    >
                        {
                            product.name
                        }
                    </Typography>

                    {Array.from(
                        attributeOptions.entries(),
                    ).map(
                        ([
                            attributeKey,
                            options,
                        ]) => (
                            <Stack
                                key={
                                    attributeKey
                                }
                                spacing={1}
                            >
                                <Typography
                                    variant="subtitle2"
                                    sx={{
                                        fontWeight:
                                            800,
                                        textAlign:
                                            'right',
                                    }}
                                >
                                    {
                                        attributeKey
                                    }
                                </Typography>

                                <Stack
                                    direction="row"
                                    sx={{
                                        flexWrap:
                                            'wrap',
                                        gap:
                                            1,
                                        direction:
                                            'rtl',
                                    }}
                                >
                                    {options.map(
                                        option => {
                                            const available =
                                                getOptionAvailable(
                                                    attributeKey,
                                                    option.value,
                                                );

                                            const selected =
                                                selection[
                                                    attributeKey
                                                ] ===
                                                option.value;

                                            return (
                                                <Button
                                                    key={`${ attributeKey } -${ option.value } `}
                                                    type="button"
                                                    variant={
                                                        selected
                                                            ? 'contained'
                                                            : 'outlined'
                                                    }
                                                    disabled={
                                                        !available ||
                                                        loading
                                                    }
                                                    onClick={() =>
                                                        setSelection(
                                                            current => ({
                                                                ...current,
                                                                [attributeKey]:
                                                                    option.value,
                                                            }),
                                                        )
                                                    }
                                                    sx={{
                                                        minWidth:
                                                            72,
                                                        minHeight:
                                                            42,
                                                        borderRadius:
                                                            2,
                                                        fontWeight:
                                                            700,
                                                    }}
                                                >
                                                    {option.colorHex && (
                                                        <span
                                                            style={{
                                                                width: 16,
                                                                height: 16,
                                                                borderRadius:
                                                                    '50%',
                                                                background:
                                                                    option.colorHex,
                                                                border: '1px solid rgba(0,0,0,.2)',
                                                                display:
                                                                    'inline-block',
                                                                marginLeft: 8,
                                                            }}
                                                        />
                                                    )}

                                                    {
                                                        option.displayValue
                                                    }
                                                </Button>
                                            );
                                        },
                                    )}
                                </Stack>
                            </Stack>
                        ),
                    )}

                    <Divider />

                    {selectedVariant ? (
                        <Stack
                            direction="row"
                            sx={{
                                direction:
                                    'rtl',
                                alignItems:
                                    'center',
                                justifyContent:
                                    'space-between',
                                gap: 1,
                            }}
                        >
                            <Stack
                                direction="row"
                                sx={{
                                    direction:
                                        'rtl',
                                    alignItems:
                                        'center',
                                    gap: 1,
                                    flexWrap:
                                        'wrap',
                                }}
                            >
                                <Chip
                                    icon={
                                        <CheckCircle />
                                    }
                                    label={`SKU: ${ selectedVariant.sku } `}
                                    color="success"
                                    variant="outlined"
                                />

                                <Chip
                                    label={`موجودی: ${ selectedVariant.stockQuantity } `}
                                    color="success"
                                />
                            </Stack>

                            <Typography
                                variant="body2"
                                sx={{
                                    fontWeight:
                                        700,
                                    whiteSpace:
                                        'nowrap',
                                }}
                            >
                                {selectedVariant.priceOverride !=
                                    null
                                    ? new Intl.NumberFormat(
                                        'fa-IR',
                                    ).format(
                                        selectedVariant.priceOverride,
                                    )
                                    : new Intl.NumberFormat(
                                        'fa-IR',
                                    ).format(
                                        product.finalPrice,
                                    )}{' '}
                                {
                                    product.currency
                                }
                            </Typography>
                        </Stack>
                    ) : (
                        <Typography
                            variant="body2"
                            color="text.secondary"
                            sx={{
                                textAlign:
                                    'right',
                                lineHeight:
                                    1.8,
                            }}
                        >
                            لطفاً ویژگی‌های موردنظر
                            محصول را انتخاب کنید.
                        </Typography>
                    )}
                </Stack>
            </DialogContent>

            <DialogActions
                sx={{
                    p: 2,
                    direction:
                        'rtl',
                    gap: 1,
                }}
            >
                <Button
                    type="button"
                    variant="outlined"
                    onClick={
                        handleClose
                    }
                    disabled={loading}
                >
                    انصراف
                </Button>

                <Button
                    type="button"
                    variant="contained"
                    startIcon={
                        <ShoppingCartOutlined />
                    }
                    onClick={
                        handleConfirm
                    }
                    disabled={
                        !canConfirm ||
                        loading
                    }
                    sx={{
                        minWidth:
                            170,
                        fontWeight:
                            800,
                    }}
                >
                    {loading
                        ? 'در حال افزودن...'
                        : 'افزودن به سبد'}
                </Button>
            </DialogActions>
        </Dialog>
    );
}
