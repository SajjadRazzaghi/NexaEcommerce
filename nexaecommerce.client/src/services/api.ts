import axios, {
    type AxiosError,
    type AxiosResponse,
    type InternalAxiosRequestConfig,
} from 'axios';

const API_BASE_URL = '/api';

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000,
    withCredentials: true,
});

api.interceptors.request.use(
    (
        config: InternalAxiosRequestConfig,
    ): InternalAxiosRequestConfig => {
        /*
         * Authentication is cookie-based in the application.
         *
         * Never read access tokens from localStorage.
         */
        return config;
    },
    (
        error: AxiosError,
    ): Promise<AxiosError> =>
        Promise.reject(error),
);

api.interceptors.response.use(
    (
        response: AxiosResponse,
    ): AxiosResponse =>
        response,
    (
        error: AxiosError,
    ): Promise<AxiosError> => {
        if (
            error.response?.status === 401
        ) {
            if (
                !window.location.pathname.includes(
                    '/login',
                )
            ) {
                window.location.href =
                    '/login';
            }
        }

        return Promise.reject(
            error,
        );
    },
);

export default api;