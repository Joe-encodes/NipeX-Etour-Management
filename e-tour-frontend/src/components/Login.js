import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import authService from '../services/authService';

const Login = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [formData, setFormData] = useState({
        username: '',
        password: ''
    });
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [mounted, setMounted] = useState(true);
    const [verificationRequired, setVerificationRequired] = useState(false);
    const [loginAttempts, setLoginAttempts] = useState(0);
    const MAX_LOGIN_ATTEMPTS = 3;

    useEffect(() => {
        // Check if user is already logged in
        if (authService.isAuthenticated()) {
            const user = authService.getCurrentUser();
            if (user) {
                handleUserNavigation(user);
            }
        }
        return () => {
            setMounted(false);
        };
    }, [navigate]);

    const handleUserNavigation = useCallback((user) => {
        if (!user.isEmailVerified) {
            setVerificationRequired(true);
            setError('Please verify your email before proceeding. Check your inbox for the verification link.');
            return;
        }

        const from = location.state?.from?.pathname || '/dashboard';
        navigate(from, { replace: true });
    }, [navigate, location]);

    const handleChange = useCallback((e) => {
        setFormData(prev => ({
            ...prev,
            [e.target.name]: e.target.value
        }));
        // Clear error when user starts typing
        if (error) {
            setError('');
        }
        if (verificationRequired) {
            setVerificationRequired(false);
        }
    }, [error, verificationRequired]);

    const handleSubmit = useCallback(async (e) => {
        e.preventDefault();
        if (isLoading) return; // Prevent multiple submissions
        
        // Validate form data
        if (!formData.username.trim() || !formData.password.trim()) {
            setError('Please enter both username and password');
            return;
        }

        setError('');
        setIsLoading(true);
        setVerificationRequired(false);

        try {
            const response = await authService.login(formData.username, formData.password);
            
            if (!mounted) return;

            if (response.success) {
                setLoginAttempts(0); // Reset login attempts on success
                handleUserNavigation(response.data);
            } else {
                setLoginAttempts(prev => prev + 1);
                if (loginAttempts >= MAX_LOGIN_ATTEMPTS - 1) {
                    setError('Too many failed attempts. Please try again later or reset your password.');
                    // Disable login for 5 minutes
                    setTimeout(() => {
                        setLoginAttempts(0);
                    }, 5 * 60 * 1000);
                } else {
                    setError(response.message || 'Login failed. Please check your credentials.');
                }
            }
        } catch (err) {
            if (mounted) {
                setError('An unexpected error occurred. Please try again later.');
                console.error('Login error:', err);
            }
        } finally {
            if (mounted) {
                setIsLoading(false);
            }
        }
    }, [formData, isLoading, mounted, handleUserNavigation, loginAttempts]);

    const handleResendVerification = useCallback(async () => {
        try {
            setIsLoading(true);
            const response = await authService.resendVerificationEmail();
            if (response.success) {
                setError('Verification email has been resent. Please check your inbox.');
            } else {
                setError(response.message || 'Failed to resend verification email.');
            }
        } catch (err) {
            setError('Failed to resend verification email. Please try again later.');
            console.error('Resend verification error:', err);
        } finally {
            setIsLoading(false);
        }
    }, []);

    const handleForgotPassword = useCallback(() => {
        navigate('/forgot-password');
    }, [navigate]);

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8">
                <div>
                    <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
                        Sign in to your account
                    </h2>
                </div>
                <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
                    <div className="rounded-md shadow-sm -space-y-px">
                        <div>
                            <label htmlFor="username" className="sr-only">
                                Username
                            </label>
                            <input
                                id="username"
                                name="username"
                                type="text"
                                required
                                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                                placeholder="Username"
                                value={formData.username}
                                onChange={handleChange}
                                disabled={isLoading || loginAttempts >= MAX_LOGIN_ATTEMPTS}
                            />
                        </div>
                        <div>
                            <label htmlFor="password" className="sr-only">
                                Password
                            </label>
                            <input
                                id="password"
                                name="password"
                                type="password"
                                required
                                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                                placeholder="Password"
                                value={formData.password}
                                onChange={handleChange}
                                disabled={isLoading || loginAttempts >= MAX_LOGIN_ATTEMPTS}
                            />
                        </div>
                    </div>

                    {error && (
                        <div className={`rounded-md p-4 ${verificationRequired ? 'bg-yellow-50' : 'bg-red-50'}`}>
                            <div className="flex">
                                <div className="flex-shrink-0">
                                    {verificationRequired ? (
                                        <svg className="h-5 w-5 text-yellow-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                            <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                                        </svg>
                                    ) : (
                                        <svg className="h-5 w-5 text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                                        </svg>
                                    )}
                                </div>
                                <div className="ml-3">
                                    <h3 className={`text-sm font-medium ${verificationRequired ? 'text-yellow-800' : 'text-red-800'}`}>
                                        {error}
                                    </h3>
                                    {verificationRequired && (
                                        <div className="mt-2">
                                            <button
                                                type="button"
                                                onClick={handleResendVerification}
                                                disabled={isLoading}
                                                className="text-sm font-medium text-indigo-600 hover:text-indigo-500 focus:outline-none focus:underline transition ease-in-out duration-150"
                                            >
                                                Resend verification email
                                            </button>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>
                    )}

                    <div className="flex items-center justify-between">
                        <div className="text-sm">
                            <button
                                type="button"
                                onClick={handleForgotPassword}
                                className="font-medium text-indigo-600 hover:text-indigo-500 focus:outline-none focus:underline transition ease-in-out duration-150"
                            >
                                Forgot your password?
                            </button>
                        </div>
                    </div>

                    <div>
                        <button
                            type="submit"
                            disabled={isLoading || loginAttempts >= MAX_LOGIN_ATTEMPTS}
                            className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {isLoading ? (
                                <>
                                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Signing in...
                                </>
                            ) : (
                                'Sign in'
                            )}
                        </button>
                    </div>

                    <div className="text-sm text-center">
                        <a href="/register" className="font-medium text-indigo-600 hover:text-indigo-500">
                            Don't have an account? Register
                        </a>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default Login;
