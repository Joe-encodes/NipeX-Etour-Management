import axios from 'axios';
import config from '../config';

// Create axios instance with default config
const axiosInstance = axios.create({
    baseURL: config.api.baseUrl,
    timeout: config.api.timeout,
    headers: {
        'Content-Type': 'application/json'
    }
});

// Request interceptor
axiosInstance.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor
axiosInstance.interceptors.response.use(
    (response) => {
        // If the backend uses the standard format, unwrap it
    if (
        response.data &&
        typeof response.data === 'object' &&
        'success' in response.data &&
        'data' in response.data
    ) {
        if (response.data.success) {
          // Return only the actual data
          return response.data.data;
        } else {
          // Throw an error with the backend's message and statusCode
          const error = new Error(response.data.message || 'Unknown error');
          error.statusCode = response.data.statusCode;
          error.data = response.data.data;
          throw error;
        }
      }
      // If not in standard format, just return as is
      return response.data;
    },
    (error) => {
        const originalRequest = error.config;

        // Handle token expiration
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
            localStorage.removeItem('token');
            window.location.href = '/login';
            return Promise.reject(error);
        }

        // Handle rate limiting
        if (error.response?.status === 429) {
            return Promise.reject({
                response: {
                    data: {
                        message: 'Too many requests. Please try again later.'
                    },
                    status: 429
                }
            });
        }

        // Handle other errors
        const errorMessage = error.response?.data?.message || 'An unexpected error occurred';
        return Promise.reject({
            response: {
                data: { message: errorMessage },
                status: error.response?.status || 500
            }
        });
    }
);

export default axiosInstance; 