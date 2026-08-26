// NexaEcommerce.Client/src/store/index.ts
import { configureStore } from '@reduxjs/toolkit';
import productReducer from '@/modules/catalog/store/productSlice';
export const store = configureStore({
    reducer: {
        products: productReducer,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware({
        serializableCheck: false,
    }),
});
