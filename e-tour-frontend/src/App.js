import React, { useEffect, useState } from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import authService from './services/authService';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import Messages from './components/Messages';
import Profile from './components/Profile';
import UploadDocument from './components/UploadDocument';
import DocumentApproval from './components/DocumentApproval';
import ErrorDisplay from './components/ErrorDisplay';
import sessionService from './services/sessionService';
import './styles/sessionWarning.css';

const PrivateRoute = ({ children, roles = [] }) => {
    const [isAuthenticated, setIsAuthenticated] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const location = useLocation();

    useEffect(() => {
        const checkAuth = async () => {
            try {
                const isAuth = await authService.isAuthenticated();
                setIsAuthenticated(isAuth);
            } catch (error) {
                setIsAuthenticated(false);
            } finally {
                setIsLoading(false);
            }
        };

        checkAuth();
    }, []);

    if (isLoading) {
        return (
            <div className="min-h-screen bg-gray-100 flex items-center justify-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
            </div>
        );
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    const user = authService.getCurrentUser();
    if (roles.length > 0 && !roles.includes(user?.role)) {
        return <Navigate to="/dashboard" replace />;
    }

    return children;
};

const NotFound = () => (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="text-center">
            <h1 className="text-6xl font-bold text-gray-800 mb-4">404</h1>
            <p className="text-xl text-gray-600 mb-8">Page not found</p>
            <a
                href="/dashboard"
                className="text-primary-600 hover:text-primary-700 font-medium"
            >
                Return to Dashboard
            </a>
        </div>
    </div>
);

const App = () => {
    useEffect(() => {
        // Initialize session service
        sessionService.startSession();

        // Cleanup on unmount
        return () => {
            sessionService.cleanup();
        };
    }, []);

    return (
        <div className="min-h-screen bg-gray-100">
            <Routes>
                <Route path="/login" element={<Login />} />
                
                <Route
                    path="/dashboard"
                    element={
                        <PrivateRoute>
                            <Dashboard />
                        </PrivateRoute>
                    }
                />

                <Route
                    path="/messages"
                    element={
                        <PrivateRoute>
                            <Messages />
                        </PrivateRoute>
                    }
                />

                <Route
                    path="/profile"
                    element={
                        <PrivateRoute>
                            <Profile />
                        </PrivateRoute>
                    }
                />

                <Route
                    path="/upload-document"
                    element={
                        <PrivateRoute>
                            <UploadDocument />
                        </PrivateRoute>
                    }
                />

                <Route
                    path="/approve-document/:documentId"
                    element={
                        <PrivateRoute roles={['Approver']}>
                            <DocumentApproval />
                        </PrivateRoute>
                    }
                />

                <Route path="/" element={<Navigate to="/dashboard" />} />
                <Route path="*" element={<NotFound />} />
            </Routes>
        </div>
    );
};

export default App;