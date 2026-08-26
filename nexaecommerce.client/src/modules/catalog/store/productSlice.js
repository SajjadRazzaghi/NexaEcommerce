import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import { productApi } from '@/services/endpoints';
const initialState = {
    products: [],
    featuredProducts: [],
    selectedProduct: null,
    loading: false,
    error: null,
    total: 0,
    page: 1,
    pageSize: 20,
};
export const fetchProducts = createAsyncThunk('products/fetchAll', async (params = {}, { rejectWithValue }) => {
    try {
        const response = await productApi.getAll({
            page: params.page ?? 1,
            pageSize: 20,
            sortBy: params.sort ?? 'newest',
            search: params.search ?? '',
        });
        return response.data;
    }
    catch (error) {
        const axiosError = error;
        return rejectWithValue(axiosError.response?.data?.error ??
            axiosError.message ??
            'خطا در دریافت محصولات');
    }
});
export const fetchFeaturedProducts = createAsyncThunk('products/fetchFeatured', async (count = 8, { rejectWithValue }) => {
    try {
        const response = await productApi.getFeatured(count);
        return response.data;
    }
    catch (error) {
        const axiosError = error;
        return rejectWithValue(axiosError.response?.data?.error ??
            axiosError.message ??
            'خطا در دریافت محصولات ویژه');
    }
});
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
export const { clearSelectedProduct, clearError, } = productSlice.actions;
export default productSlice.reducer;
