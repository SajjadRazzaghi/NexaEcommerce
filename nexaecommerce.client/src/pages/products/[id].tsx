import { Box, CircularProgress } from '@mui/material';
import { Navigate, useParams } from 'react-router-dom';

import { useProduct } from '@/modules/catalog/hooks/useProducts';

export default function ProductPage() {
    const { id } = useParams<{
        id: string;
    }>();

    const {
        data: product,
        isLoading,
        error,
    } = useProduct(id);

    if (isLoading) {
        return (
            <Box
                sx={{
                    minHeight: '60vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    if (error || !product) {
        return (
            <Box
                sx={{
                    minHeight: '60vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    px: 2,
                    textAlign: 'center',
                }}
            >
                محصول موردنظر پیدا نشد.
            </Box>
        );
    }

    return (
        <Navigate
            to={`/products/${encodeURIComponent(product.slug)}`}
            replace
        />
    );
}