import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import authService from '../services/authService';

const EmailVerification = () => {
    const { token } = useParams();
    const navigate = useNavigate();
    const [status, setStatus] = useState('verifying');
    const [error, setError] = useState(null);

    useEffect(() => {
        verifyEmail();
    }, [token]);

    const verifyEmail = async () => {
        try {
            setStatus('verifying');
            const response = await authService.verifyEmail(token);
            if (response.success) {
                setStatus('success');
                setTimeout(() => {
                    navigate('/login');
                }, 3000);
            }
        } catch (err) {
            setStatus('error');
            setError(err.message || 'Failed to verify email');
        }
    };

    const renderContent = () => {
        switch (status) {
            case 'verifying':
                return (
                    <div className="text-center">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto mb-4"></div>
                        <h2 className="text-xl font-semibold text-gray-800 mb-2">Verifying your email...</h2>
                        <p className="text-gray-600">Please wait while we verify your email address.</p>
                    </div>
                );
            case 'success':
                return (
                    <div className="text-center">
                        <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100 mb-4">
                            <svg
                                className="h-6 w-6 text-green-600"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                            >
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth="2"
                                    d="M5 13l4 4L19 7"
                                />
                            </svg>
                        </div>
                        <h2 className="text-xl font-semibold text-gray-800 mb-2">Email Verified!</h2>
                        <p className="text-gray-600 mb-4">Your email has been successfully verified.</p>
                        <p className="text-sm text-gray-500">Redirecting to login page...</p>
                    </div>
                );
            case 'error':
                return (
                    <div className="text-center">
                        <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100 mb-4">
                            <svg
                                className="h-6 w-6 text-red-600"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                            >
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth="2"
                                    d="M6 18L18 6M6 6l12 12"
                                />
                            </svg>
                        </div>
                        <h2 className="text-xl font-semibold text-gray-800 mb-2">Verification Failed</h2>
                        <p className="text-gray-600 mb-4">{error}</p>
                        <button
                            onClick={() => navigate('/login')}
                            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
                        >
                            Return to Login
                        </button>
                    </div>
                );
            default:
                return null;
        }
    };

    return (
        <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
            <div className="sm:mx-auto sm:w-full sm:max-w-md">
                <h1 className="text-center text-3xl font-extrabold text-gray-900 mb-8">
                    Email Verification
                </h1>
                <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
                    {renderContent()}
                </div>
            </div>
        </div>
    );
};

export default EmailVerification; 