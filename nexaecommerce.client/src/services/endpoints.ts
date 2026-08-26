import { productsApi } from '@/modules/catalog/api/products';

// Legacy compatibility facade.
// New Catalog pages should import productsApi directly.
export const productApi = productsApi;
