import React, { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../utils/axiosConfig';

const AuthContext = createContext(null);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (token) {
            fetchUserProfile(token);
        } else {
            setLoading(false);
        }
    }, []);

    const fetchUserProfile = async (token) => {
        try {
            const response = await axiosInstance.get('/api/auth/profile');
            setUser(response.data.data);
            setLoading(false);
        } catch (err) {
            console.error('Error fetching user profile:', err);
            localStorage.removeItem('token');
            setUser(null);
            setLoading(false);
        }
    };

    const login = async (username, password) => {
        try {
            setError(null);
            const response = await axiosInstance.post('/api/auth/login', {
                username,
                password
            });

            const { token, user: userData } = response.data.data;
            localStorage.setItem('token', token);
            setUser(userData);

            // Redirect based on role
            const dashboardPath = getDashboardPath(userData.role);
            navigate(dashboardPath);
            return { success: true };
        } catch (err) {
            const errorMessage = err.response?.data?.message || 'Login failed';
            setError(errorMessage);
            return { success: false, error: errorMessage };
        }
    };

    const register = async (userData) => {
        try {
            setError(null);
            const response = await axiosInstance.post('/api/auth/register', userData);
            return { success: true, data: response.data.data };
        } catch (err) {
            const errorMessage = err.response?.data?.message || 'Registration failed';
            setError(errorMessage);
            return { success: false, error: errorMessage };
        }
    };

    const verifyEmail = async (token) => {
        try {
            setError(null);
            const response = await axiosInstance.post('/api/auth/verify-email', { token });
            
            // If verification is successful, automatically log in the user
            if (response.data.data.token) {
                localStorage.setItem('token', response.data.data.token);
                setUser(response.data.data.user);
                
                // Redirect to appropriate dashboard
                const dashboardPath = getDashboardPath(response.data.data.user.role);
                navigate(dashboardPath);
            }
            
            return { success: true, data: response.data.data };
        } catch (err) {
            const errorMessage = err.response?.data?.message || 'Email verification failed';
            setError(errorMessage);
            return { success: false, error: errorMessage };
        }
    };

    const logout = () => {
        localStorage.removeItem('token');
        setUser(null);
        navigate('/');
    };

    const isAuthenticated = () => {
        return !!user;
    };

    const hasRole = (role) => {
        return user?.role === role;
    };

    const getDashboardPath = (role) => {
        switch (role) {
            case 'User':
                return '/user-dashboard';
            case 'Approver':
                return '/approver-dashboard';
            case 'Admin':
                return '/admin-dashboard';
            default:
                return '/';
        }
    };

    const value = {
        user,
        loading,
        error,
        login,
        register,
        verifyEmail,
        logout,
        isAuthenticated,
        hasRole
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}; 