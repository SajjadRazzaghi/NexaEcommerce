import api from './api';
export const productApi = {
    getAll: (params) => api.get('/products', { params }),
    getById: (id) => api.get(`/products/${id}`),
    getByCategory: (categoryId) => api.get(`/products/category/${categoryId}`),
    search: (query) => api.get(`/products/search?q=${encodeURIComponent(query)}`),
    getFeatured: (count = 8) => api.get(`/products/featured?count=${count}`),
    create: (data) => api.post('/products', data),
    update: (id, data) => api.put(`/products/${id}`, data),
    updateStock: (id, quantity) => api.patch(`/products/${id}/stock`, { quantity }),
    delete: (id) => api.delete(`/products/${id}`),
};
