import React, { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';
import config from '../config';

export const AuthContext = createContext(null);

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
  const [backendAvailable, setBackendAvailable] = useState(true);

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
      const response = await axios.get(`${config.api.baseUrl}/api/auth/profile`, {
        headers: { Authorization: `Bearer ${token}` },
        timeout: config.api.timeout
      });
      setUser(response.data);
      setBackendAvailable(true);
    } catch (error) {
      console.error('Failed to fetch user profile:', error);
      if (error.code === 'ECONNABORTED' || error.message.includes('Network Error')) {
        setBackendAvailable(false);
      }
      logout();
    } finally {
      setLoading(false);
    }
  };

  const login = async (username, password) => {
    try {
      setError(null);
      const response = await axios.post(`${config.api.baseUrl}/api/auth/login`, {
        username,
        password
      }, {
        timeout: config.api.timeout
      });
      
      const { token, ...userData } = response.data;
      if (!token) {
        setError('Invalid response from server');
        return null;
      }

      localStorage.setItem('token', token);
      setUser(userData);
      setBackendAvailable(true);
      return userData;
    } catch (error) {
      if (error.code === 'ECONNABORTED' || error.message.includes('Network Error')) {
        setBackendAvailable(false);
        setError('Backend service is currently unavailable');
      } else {
        const errorMessage = error.response?.data?.message || 'Login failed';
        setError(errorMessage);
      }
      return null;
    }
  };

  const register = async (userData) => {
    try {
      setError(null);
      const response = await axios.post(`${config.api.baseUrl}/api/auth/register`, userData, {
        timeout: config.api.timeout
      });
      
      if (response.data.success) {
        setBackendAvailable(true);
        return { success: true, message: 'Registration successful' };
      } else {
        setError(response.data.message || 'Registration failed');
        return { success: false, message: response.data.message };
      }
    } catch (error) {
      if (error.code === 'ECONNABORTED' || error.message.includes('Network Error')) {
        setBackendAvailable(false);
        setError('Backend service is currently unavailable');
        return { success: false, message: 'Backend service is currently unavailable' };
      } else {
        const errorMessage = error.response?.data?.message || 'Registration failed';
        setError(errorMessage);
        return { success: false, message: errorMessage };
      }
    }
  };

  const logout = () => {
    localStorage.removeItem('token');
    setUser(null);
    setError(null);
  };

  const isAuthenticated = () => !!user;
  
  const hasRole = (role) => {
    return user?.role === role;
  };

  const value = {
    user,
    loading,
    error,
    backendAvailable,
    login,
    register,
    logout,
    isAuthenticated,
    hasRole
  };

  return (
    <AuthContext.Provider value={value}>
      {!loading && children}
    </AuthContext.Provider>
  );
}; 