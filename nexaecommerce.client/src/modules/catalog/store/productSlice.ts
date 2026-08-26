import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import { productApi } from '@/services/endpoints';
import type { Product } from '../types/product.types';
import type { ProductListResponse } from '../api/products';
import type { AxiosError } from 'axios';

type ProductSort =
    | 'newest'
    | 'price_asc'
    | 'price_desc'
    | 'name'
    | 'popular';

const validSorts: ProductSort[] = [
    'newest',
    'price_asc',
    'price_desc',
    'name',
    'popular',
];

interface ProductState {
    products: ProductListResponse['items'];
    featuredProducts: Product[];
    selectedProduct: Product | null;
    loading: boolean;
    error: string | null;
    total: number;
    page: number;
    pageSize: number;
}

const initialState: ProductState = {
    products: [],
    featuredProducts: [],
    selectedProduct: null,
    loading: false,
    error: null,
    total: 0,
    page: 1,
    pageSize: 20,
};

interface FetchProductsParams {
    page?: number;
    sort?: string;
    search?: string;
}

export const fetchProducts = createAsyncThunk<
    ProductListResponse,
    FetchProductsParams | undefined,
    { rejectValue: string }
>(
    'products/fetchAll',
    async (params = {}, { rejectWithValue }) => {
        try {
            const response = await productApi.getAll({
                page: params.page ?? 1,
                pageSize: 20,
                sortBy: validSorts.includes(params.sort as ProductSort)
                    ? (params.sort as ProductSort)
                    : 'newest',
                search: params.search ?? '',
            });

            return response;
        } catch (error) {
            const axiosError = error as AxiosError<{ error?: string }>;

            return rejectWithValue(
                axiosError.response?.data?.error ??
                axiosError.message ??
                'خطا در دریافت محصولات'
            );
        }
    }
);

export const fetchFeaturedProducts = createAsyncThunk<
    Product[],
    number | undefined,
    { rejectValue: string }
>(
    'products/fetchFeatured',
    async (count = 8, { rejectWithValue }) => {
        try {
            const response = await productApi.getFeatured(count);

            return response;
        } catch (error) {
            const axiosError = error as AxiosError<{ error?: string }>;

            return rejectWithValue(
                axiosError.response?.data?.error ??
                axiosError.message ??
                'خطا در دریافت محصولات ویژه'
            );
        }
    }
);

const productSlice = createSlice({
    name: 'products',
    initialState,

    reducers: {
        clearSelectedProduct: (state) => {
            state.selectedProduct = null;
        },

        clearError: (state) => {
            state.error = null;
        },
    },

    extraReducers: (builder) => {
        builder
            .addCase(fetchProducts.pending, (state) => {
                state.loading = true;
                state.error = null;
            })

            .addCase(fetchProducts.fulfilled, (state, action) => {
                state.loading = false;
                state.products = action.payload.items;
                state.total = action.payload.total;
                state.page = action.payload.page;
                state.pageSize = action.payload.pageSize;
            })

            .addCase(fetchProducts.rejected, (state, action) => {
                state.loading = false;
                state.error =
                    action.payload ?? 'خطا در دریافت محصولات';
            })

            .addCase(fetchFeaturedProducts.pending, (state) => {
                state.loading = true;
                state.error = null;
            })

            .addCase(fetchFeaturedProducts.fulfilled, (state, action) => {
                state.loading = false;
                state.featuredProducts = action.payload;
            })

            .addCase(fetchFeaturedProducts.rejected, (state, action) => {
                state.loading = false;
                state.error =
                    action.payload ?? 'خطا در دریافت محصولات ویژه';
            });
    },
});

export const {
    clearSelectedProduct,
    clearError,
} = productSlice.actions;

export default productSlice.reducer;