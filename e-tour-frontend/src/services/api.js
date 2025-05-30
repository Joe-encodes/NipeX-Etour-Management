import axios from 'axios';

// Default configuration
const defaultConfig = {
    baseURL: process.env.REACT_APP_API_URL || 'https://localhost:7078/api',
    timeout: parseInt(process.env.API_TIMEOUT || '30') * 1000,
    headers: {
        'Content-Type': 'application/json'
    }
};

// Create axios instance with default config
const api = axios.create(defaultConfig);

// Request interceptor
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('auth_token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        console.error('Request interceptor error:', error);
        return Promise.reject(error);
    }
);

// Response interceptor
api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        // Handle network errors
        if (!error.response) {
            if (error.code === 'ECONNABORTED') {
                throw new Error('Request timed out. Please check your connection and try again.');
            }
            throw new Error('Network error. Please check your connection and try again.');
        }

        // Handle token expiration
        if (error.response.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                const refreshToken = localStorage.getItem('refresh_token');
                if (!refreshToken) {
                    throw new Error('No refresh token available');
                }

                const response = await axios.post(`${defaultConfig.baseURL}/auth/refresh-token`, {
                    refreshToken
                });

                const { token, refreshToken: newRefreshToken } = response.data.data;

                localStorage.setItem('auth_token', token);
                localStorage.setItem('refresh_token', newRefreshToken);
                localStorage.setItem('token_expiry', new Date(Date.now() + 30 * 60 * 1000).toISOString());

                originalRequest.headers.Authorization = `Bearer ${token}`;
                return api(originalRequest);
            } catch (refreshError) {
                // Clear auth data and redirect to login
                localStorage.removeItem('auth_token');
                localStorage.removeItem('refresh_token');
                localStorage.removeItem('token_expiry');
                window.location.href = '/login';
                throw new Error('Session expired. Please login again.');
            }
        }

        // Handle other errors
        const errorMessage = error.response.data?.message || error.message || 'An error occurred';
        throw new Error(errorMessage);
    }
);

export default api; 