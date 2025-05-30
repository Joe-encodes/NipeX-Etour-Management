import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import authService from '../services/authService';
import documentService from '../services/documentService';
import messageService from '../services/messageService';
import userService from '../services/userService';

const Dashboard = () => {
    const navigate = useNavigate();
    const [user, setUser] = useState(null);
    const [documents, setDocuments] = useState([]);
    const [messages, setMessages] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [activeTab, setActiveTab] = useState('documents');

    useEffect(() => {
        const loadDashboard = async () => {
            try {
                const currentUser = authService.getCurrentUser();
                if (!currentUser) {
                    navigate('/login');
                    return;
                }

                setUser(currentUser);

                // Load documents based on user role
                let docs;
                if (currentUser.role === 'Admin') {
                    docs = await documentService.getAllDocuments();
                } else if (currentUser.role === 'Approver') {
                    docs = await documentService.getPendingDocuments();
                } else {
                    docs = await documentService.getUserDocuments();
                }
                setDocuments(docs.data || []);

                // Load messages
                const msgs = await messageService.getMessages();
                setMessages(msgs.data || []);

                setLoading(false);
            } catch (err) {
                setError(err.message);
                setLoading(false);
            }
        };

        loadDashboard();
    }, [navigate]);

    const handleLogout = () => {
        authService.logout();
        navigate('/login');
    };

    if (loading) {
        return (
            <div className="min-h-screen bg-gray-100 flex items-center justify-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-100">
            {/* Navigation */}
            <nav className="bg-white shadow-lg">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between h-16">
                        <div className="flex">
                            <div className="flex-shrink-0 flex items-center">
                                <h1 className="text-xl font-bold text-indigo-600">E-Tour Management</h1>
                            </div>
                            <div className="hidden sm:ml-6 sm:flex sm:space-x-8">
                                <button
                                    onClick={() => setActiveTab('documents')}
                                    className={`${
                                        activeTab === 'documents'
                                            ? 'border-indigo-500 text-gray-900'
                                            : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
                                    } inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium`}
                                >
                                    Documents
                                </button>
                                <button
                                    onClick={() => setActiveTab('messages')}
                                    className={`${
                                        activeTab === 'messages'
                                            ? 'border-indigo-500 text-gray-900'
                                            : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
                                    } inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium`}
                                >
                                    Messages
                                </button>
                                {user?.role === 'Admin' && (
                                    <button
                                        onClick={() => setActiveTab('users')}
                                        className={`${
                                            activeTab === 'users'
                                                ? 'border-indigo-500 text-gray-900'
                                                : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
                                        } inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium`}
                                    >
                                        Users
                                    </button>
                                )}
                            </div>
                        </div>
                        <div className="flex items-center">
                            <div className="ml-3 relative">
                                <div className="flex items-center space-x-4">
                                    <div className="text-right">
                                        <p className="text-sm font-medium text-gray-900 truncate max-w-[150px]">
                                            Welcome, {user?.username}
                                        </p>
                                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800">
                                            {user?.role}
                                        </span>
                                    </div>
                                    <button
                                        onClick={handleLogout}
                                        className="inline-flex items-center px-3 py-1.5 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                    >
                                        Logout
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </nav>

            {/* Main Content */}
            <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
                {error && (
                    <div className="mb-4 bg-red-50 border-l-4 border-red-400 p-4">
                        <div className="flex">
                            <div className="flex-shrink-0">
                                <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                                </svg>
                            </div>
                            <div className="ml-3">
                                <p className="text-sm text-red-700">{error}</p>
                            </div>
                        </div>
                    </div>
                )}

                {/* Documents Tab */}
                {activeTab === 'documents' && (
                    <div className="bg-white shadow overflow-hidden sm:rounded-lg">
                        <div className="px-4 py-5 sm:px-6 flex justify-between items-center">
                            <div>
                                <h3 className="text-lg leading-6 font-medium text-gray-900">Documents</h3>
                                <p className="mt-1 max-w-2xl text-sm text-gray-500">
                                    {user?.role === 'Admin'
                                        ? 'All documents in the system'
                                        : user?.role === 'Approver'
                                        ? 'Documents pending your approval'
                                        : 'Your uploaded documents'}
                                </p>
                            </div>
                            {user?.role === 'User' && (
                                <button
                                    onClick={() => navigate('/upload')}
                                    className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                >
                                    Upload Document
                                </button>
                            )}
                        </div>
                        <div className="border-t border-gray-200">
                            <div className="overflow-x-auto">
                                <div className="inline-block min-w-full align-middle">
                                    <div className="overflow-hidden">
                                        <ul className="divide-y divide-gray-200">
                                            {documents.map((doc) => (
                                                <li key={doc.id} className="px-4 py-4 sm:px-6">
                                                    <div className="flex items-center justify-between space-x-4">
                                                        <div className="flex-1 min-w-0">
                                                            <div className="flex items-center space-x-3">
                                                                <div className="flex-shrink-0">
                                                                    <svg className="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                                                                    </svg>
                                                                </div>
                                                                <div className="flex-1 min-w-0">
                                                                    <p className="text-sm font-medium text-gray-900 truncate">
                                                                        {doc.title.length > 15 ? `${doc.title.substring(0, 15)}...` : doc.title}
                                                                    </p>
                                                                    <p className="text-sm text-gray-500 truncate">
                                                                        {doc.user?.username || 'Unknown'}
                                                                    </p>
                                                                </div>
                                                            </div>
                                                        </div>
                                                        <div className="flex items-center space-x-2">
                                                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                                                doc.status === 'Pending' ? 'bg-yellow-100 text-yellow-800' :
                                                                doc.status === 'Approved' ? 'bg-green-100 text-green-800' :
                                                                'bg-gray-100 text-gray-800'
                                                            }`}>
                                                                {doc.status}
                                                            </span>
                                                            <button
                                                                onClick={() => navigate(`/documents/${doc.id}`)}
                                                                className="inline-flex items-center p-2 border border-transparent rounded-md text-indigo-700 bg-indigo-100 hover:bg-indigo-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                                            >
                                                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                                                </svg>
                                                            </button>
                                                            <button
                                                                onClick={() => documentService.downloadDocument(doc.id)}
                                                                className="inline-flex items-center p-2 border border-transparent rounded-md text-green-700 bg-green-100 hover:bg-green-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500"
                                                            >
                                                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                                                                </svg>
                                                            </button>
                                                        </div>
                                                    </div>
                                                </li>
                                            ))}
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {/* Messages Tab */}
                {activeTab === 'messages' && (
                    <div className="bg-white shadow overflow-hidden sm:rounded-lg">
                        <div className="px-4 py-5 sm:px-6">
                            <h3 className="text-lg leading-6 font-medium text-gray-900">Messages</h3>
                            <p className="mt-1 max-w-2xl text-sm text-gray-500">Your inbox messages</p>
                        </div>
                        <div className="border-t border-gray-200">
                            <ul className="divide-y divide-gray-200">
                                {messages.map((message) => (
                                    <li key={message.id} className="px-4 py-4 sm:px-6">
                                        <div className="flex items-center justify-between">
                                            <div className="flex items-center">
                                                <div className="flex-shrink-0">
                                                    <svg className={`h-6 w-6 ${message.isRead ? 'text-gray-400' : 'text-indigo-400'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                                                    </svg>
                                                </div>
                                                <div className="ml-4">
                                                    <h4 className="text-sm font-medium text-gray-900">{message.subject}</h4>
                                                    <p className="text-sm text-gray-500">From: {message.senderName}</p>
                                                </div>
                                            </div>
                                            <div className="flex space-x-2">
                                                <button
                                                    onClick={() => navigate(`/messages/${message.id}`)}
                                                    className="inline-flex items-center px-3 py-1 border border-transparent text-sm font-medium rounded-md text-indigo-700 bg-indigo-100 hover:bg-indigo-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                                >
                                                    View
                                                </button>
                                                <button
                                                    onClick={() => messageService.deleteMessage(message.id)}
                                                    className="inline-flex items-center px-3 py-1 border border-transparent text-sm font-medium rounded-md text-red-700 bg-red-100 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                                                >
                                                    Delete
                                                </button>
                                            </div>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    </div>
                )}

                {/* Users Tab (Admin only) */}
                {activeTab === 'users' && user?.role === 'Admin' && (
                    <div className="bg-white shadow overflow-hidden sm:rounded-lg">
                        <div className="px-4 py-5 sm:px-6">
                            <h3 className="text-lg leading-6 font-medium text-gray-900">Users</h3>
                            <p className="mt-1 max-w-2xl text-sm text-gray-500">Manage system users</p>
                        </div>
                        <div className="border-t border-gray-200">
                            {/* User management content will be added here */}
                        </div>
                    </div>
                )}
            </main>
            <footer className="bg-white border-t border-gray-200 mt-auto">
                <div className="max-w-7xl mx-auto py-4 px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between items-center">
                        <p className="text-sm text-gray-500">
                            © {new Date().getFullYear()} E-Tour Management System
                        </p>
                        <div className="flex space-x-6">
                            <button
                                onClick={() => navigate('/terms')}
                                className="text-sm text-gray-500 hover:text-gray-900"
                            >
                                Terms
                            </button>
                            <button
                                onClick={() => navigate('/privacy')}
                                className="text-sm text-gray-500 hover:text-gray-900"
                            >
                                Privacy
                            </button>
                        </div>
                    </div>
                </div>
            </footer>
        </div>
    );
};

export default Dashboard; 