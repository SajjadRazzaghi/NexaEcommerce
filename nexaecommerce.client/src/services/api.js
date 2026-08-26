// NexaEcommerce.Client/src/services/api.ts
import axios from 'axios';
// ✅ استفاده از URL نسبی (از طریق Proxy Vite)
const API_BASE_URL = '/api';
const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000,
});
api.interceptors.request.use((config) => {
    const token = localStorage.getItem('accessToken');
    if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
}, (error) => Promise.reject(error));
api.interceptors.response.use((response) => response, (error) => {
    if (error.response?.status === 401) {
        window.location.href = '/login';
    }
    return Promise.reject(error);
});
export default api;
