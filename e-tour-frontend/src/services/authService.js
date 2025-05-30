import api from './api';

const AUTH_CONFIG = {
    tokenKey: 'auth_token',
    refreshTokenKey: 'refresh_token',
    tokenExpiryKey: 'token_expiry',
    sessionTimeout: 30 * 60 * 1000, // 30 minutes
    retryAttempts: 3,
    retryDelay: 1000
};

const handleApiError = (error) => {
    if (error.response) {
        // The request was made and the server responded with a status code
        // that falls out of the range of 2xx
        const { status, data } = error.response;
        
        switch (status) {
            case 400:
                return {
                    success: false,
                    message: data.message || 'Invalid request. Please check your input.'
                };
            case 401:
                return {
                    success: false,
                    message: data.message || 'Invalid credentials. Please try again.'
                };
            case 403:
                return {
                    success: false,
                    message: data.message || 'Access denied. Please check your permissions.'
                };
            case 404:
                return {
                    success: false,
                    message: data.message || 'Resource not found.'
                };
            case 429:
                return {
                    success: false,
                    message: 'Too many requests. Please try again later.'
                };
            case 500:
                return {
                    success: false,
                    message: 'Server error. Please try again later.'
                };
            default:
                return {
                    success: false,
                    message: data.message || 'An error occurred. Please try again.'
                };
        }
    } else if (error.request) {
        // The request was made but no response was received
        return {
            success: false,
            message: 'No response from server. Please check your internet connection.'
        };
    } else {
        // Something happened in setting up the request that triggered an Error
        return {
            success: false,
            message: 'An unexpected error occurred. Please try again.'
        };
    }
};

const authService = {
    // Login with retry logic
    login: async (username, password) => {
        if (!username || !password) {
            return {
                success: false,
                message: 'Username and password are required'
            };
        }

        let attempts = 0;
        const maxAttempts = AUTH_CONFIG.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.post('/auth/login', {
                    username,
                    password
                });

                if (!response.data) {
                    return {
                        success: false,
                        message: 'Invalid response from server'
                    };
                }

                const { success, data, message } = response.data;

                if (!success || !data) {
                    return {
                        success: false,
                        message: message || 'Login failed'
                    };
                }

                const { token, refreshToken, user } = data;
                
                if (!token || !refreshToken || !user) {
                    return {
                        success: false,
                        message: 'Invalid authentication data received'
                    };
                }

                // Store tokens
                localStorage.setItem(AUTH_CONFIG.tokenKey, token);
                localStorage.setItem(AUTH_CONFIG.refreshTokenKey, refreshToken);
                localStorage.setItem(AUTH_CONFIG.tokenExpiryKey, new Date(Date.now() + AUTH_CONFIG.sessionTimeout).toISOString());

                return {
                    success: true,
                    data: user
                };
            } catch (error) {
                attempts++;
                
                // Don't retry if it's an authentication error
                if (error.response && error.response.status === 401) {
                    return handleApiError(error);
                }

                // Don't retry if it's not a network error or timeout
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    return handleApiError(error);
                }

                // If we've exhausted all attempts, return the error
                if (attempts === maxAttempts) {
                    return handleApiError(error);
                }

                // Wait before retrying
                await new Promise(resolve => setTimeout(resolve, AUTH_CONFIG.retryDelay * attempts));
            }
        }
    },

    // Register with retry logic
    register: async (userData) => {
        let attempts = 0;
        const maxAttempts = AUTH_CONFIG.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.post('/auth/register', userData);
                return response.data;
            } catch (error) {
                attempts++;
                
                // Don't retry if it's a validation error
                if (error.response && error.response.status === 400) {
                    return handleApiError(error);
                }

                // Don't retry if it's not a network error or timeout
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    return handleApiError(error);
                }

                // If we've exhausted all attempts, return the error
                if (attempts === maxAttempts) {
                    return handleApiError(error);
                }

                // Wait before retrying
                await new Promise(resolve => setTimeout(resolve, AUTH_CONFIG.retryDelay * attempts));
            }
        }
    },

    // Logout
    logout: () => {
        try {
            localStorage.removeItem(AUTH_CONFIG.tokenKey);
            localStorage.removeItem(AUTH_CONFIG.refreshTokenKey);
            localStorage.removeItem(AUTH_CONFIG.tokenExpiryKey);
            window.location.href = '/login';
        } catch (error) {
            console.error('Logout error:', error);
            // Force redirect even if there's an error
            window.location.href = '/login';
        }
    },

    // Check if user is authenticated
    isAuthenticated: () => {
        try {
            const token = localStorage.getItem(AUTH_CONFIG.tokenKey);
            const tokenExpiry = localStorage.getItem(AUTH_CONFIG.tokenExpiryKey);
            
            if (!token || !tokenExpiry) {
                return false;
            }

            // Check if token is expired
            if (new Date(tokenExpiry) <= new Date()) {
                authService.logout();
                return false;
            }

            return true;
        } catch (error) {
            console.error('Authentication check error:', error);
            return false;
        }
    },

    // Get current user
    getCurrentUser: () => {
        try {
            const token = localStorage.getItem(AUTH_CONFIG.tokenKey);
            if (!token) return null;

            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));

            return JSON.parse(jsonPayload);
        } catch (error) {
            console.error('Error parsing JWT token:', error);
            return null;
        }
    },

    // Refresh token with retry logic
    refreshToken: async () => {
        let attempts = 0;
        const maxAttempts = AUTH_CONFIG.retryAttempts;
        const refreshToken = localStorage.getItem(AUTH_CONFIG.refreshTokenKey);

        if (!refreshToken) {
            return {
                success: false,
                message: 'No refresh token available'
            };
        }

        while (attempts < maxAttempts) {
            try {
                const response = await api.post('/auth/refresh', {
                    refreshToken
                });

                const { token, newRefreshToken } = response.data;
                localStorage.setItem(AUTH_CONFIG.tokenKey, token);
                localStorage.setItem(AUTH_CONFIG.refreshTokenKey, newRefreshToken);
                localStorage.setItem(AUTH_CONFIG.tokenExpiryKey, new Date(Date.now() + AUTH_CONFIG.sessionTimeout).toISOString());

                return {
                    success: true,
                    data: token
                };
            } catch (error) {
                attempts++;
                
                // Don't retry if it's an authentication error
                if (error.response && error.response.status === 401) {
                    authService.logout();
                    return handleApiError(error);
                }

                // Don't retry if it's not a network error or timeout
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    return handleApiError(error);
                }

                // If we've exhausted all attempts, return the error
                if (attempts === maxAttempts) {
                    return handleApiError(error);
                }

                // Wait before retrying
                await new Promise(resolve => setTimeout(resolve, AUTH_CONFIG.retryDelay * attempts));
            }
        }
    },

    // Update user profile with retry logic
    updateProfile: async (profileData) => {
        let attempts = 0;
        const maxAttempts = AUTH_CONFIG.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.put('/users/profile', profileData, {
                    timeout: AUTH_CONFIG.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                // Don't retry if it's a validation error
                if (error.response && error.response.status === 400) {
                    throw new Error(error.response.data.message || 'Profile update failed');
                }

                // Don't retry if it's not a network error or timeout
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    throw error;
                }

                // If we've exhausted all attempts, throw the error
                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Profile update request timed out. Please try again.');
                    }
                    throw error;
                }

                // Wait before retrying
                await new Promise(resolve => setTimeout(resolve, AUTH_CONFIG.retryDelay * attempts));
            }
        }
    },

    // Resend verification email
    resendVerificationEmail: async () => {
        try {
            const response = await api.post('/auth/resend-verification');
            return {
                success: true,
                message: 'Verification email sent successfully'
            };
        } catch (error) {
            return handleApiError(error);
        }
    }
};

export default authService; 