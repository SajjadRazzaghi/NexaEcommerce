import {
    useEffect,
    useState,
} from 'react';

import {
    Box,
    Checkbox,
    Divider,
    FormControlLabel,
    Slider,
    Stack,
    Typography,
} from '@mui/material';

interface ProductFiltersProps {
    minPrice: number;
    maxPrice: number;
    inStock: boolean;
    onPriceChange: (
        min: number,
        max: number,
    ) => void;
    onStockChange: (
        value: boolean,
    ) => void;
}

export default function ProductFilters({
    minPrice,
    maxPrice,
    inStock,
    onPriceChange,
    onStockChange,
}: ProductFiltersProps) {
    const [priceRange, setPriceRange] =
        useState<number[]>([
            minPrice,
            maxPrice,
        ]);

    useEffect(() => {
        setPriceRange([
            minPrice,
            maxPrice,
        ]);
    }, [
        minPrice,
        maxPrice,
    ]);

    return (
        <Box
            sx={{
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 3,
                p: 2.5,
                backgroundColor: 'background.paper',
            }}
        >
            <Typography
                variant="h6"
                sx={{
                    fontWeight: 800,
                }}
            >
                فیلترها
            </Typography>

            <Divider
                sx={{
                    my: 2,
                }}
            />

            <Stack spacing={3}>
                <Box>
                    <Typography
                        variant="subtitle2"
                        sx={{
                            fontWeight: 700,
                            mb: 1,
                        }}
                    >
                        محدوده قیمت
                    </Typography>

                    <Slider
                        value={priceRange}
                        min={0}
                        max={maxPrice}
                        step={10000}
                        valueLabelDisplay="auto"
                        onChange={(
                            _event,
                            value,
                        ) => {
                            if (
                                Array.isArray(
                                    value,
                                )
                            ) {
                                setPriceRange(
                                    value,
                                );
                            }
                        }}
                        onChangeCommitted={(
                            _event,
                            value,
                        ) => {
                            if (
                                Array.isArray(
                                    value,
                                )
                            ) {
                                onPriceChange(
                                    value[0],
                                    value[1],
                                );
                            }
                        }}
                    />

                    <Stack
                        direction="row"
                        sx={{
                            justifyContent: 'space-between',
                        }}
                    >
                        <Typography
                            variant="caption"
                        >
                            {priceRange[0].toLocaleString(
                                'fa-IR',
                            )}
                        </Typography>

                        <Typography
                            variant="caption"
                        >
                            {priceRange[1].toLocaleString(
                                'fa-IR',
                            )}
                        </Typography>
                    </Stack>
                </Box>

                <FormControlLabel
                    control={
                        <Checkbox
                            checked={inStock}
                            onChange={(
                                event,
                            ) =>
                                onStockChange(
                                    event.target
                                        .checked,
                                )
                            }
                        />
                    }
                    label="فقط کالاهای موجود"
                />
            </Stack>
        </Box>
    );
}