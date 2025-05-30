import React, { useState, useEffect } from 'react';
import documentService from '../services/documentService';

const DocumentApproval = () => {
    const [documents, setDocuments] = useState([]);
    const [selectedDocument, setSelectedDocument] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionLoading, setActionLoading] = useState(false);

    useEffect(() => {
        loadDocuments();
    }, []);

    const loadDocuments = async () => {
        try {
            setLoading(true);
            const data = await documentService.getPendingDocuments();
            setDocuments(data);
            setError(null);
        } catch (err) {
            setError('Failed to load documents');
            console.error('Error loading documents:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleDocumentSelect = (document) => {
        setSelectedDocument(document);
    };

    const handleApprove = async (documentId) => {
        try {
            setActionLoading(true);
            await documentService.approveDocument(documentId);
            setDocuments(documents.filter(doc => doc.id !== documentId));
            if (selectedDocument?.id === documentId) {
                setSelectedDocument(null);
            }
        } catch (err) {
            setError('Failed to approve document');
            console.error('Error approving document:', err);
        } finally {
            setActionLoading(false);
        }
    };

    const handleReject = async (documentId) => {
        try {
            setActionLoading(true);
            await documentService.rejectDocument(documentId);
            setDocuments(documents.filter(doc => doc.id !== documentId));
            if (selectedDocument?.id === documentId) {
                setSelectedDocument(null);
            }
        } catch (err) {
            setError('Failed to reject document');
            console.error('Error rejecting document:', err);
        } finally {
            setActionLoading(false);
        }
    };

    if (loading) {
        return <div className="flex items-center justify-center h-screen text-gray-600">Loading documents...</div>;
    }

    if (error) {
        return <div className="flex items-center justify-center h-screen text-red-500">{error}</div>;
    }

    return (
        <div className="flex gap-8 p-4 h-[calc(100vh-100px)]">
            <div className="flex-1 border-r border-gray-200 pr-4 overflow-y-auto">
                <h2 className="text-2xl font-bold mb-4 text-gray-800">Pending Documents</h2>
                {documents.length === 0 ? (
                    <p className="text-center text-gray-600 py-8">No pending documents</p>
                ) : (
                    documents.map(doc => (
                        <div
                            key={doc.id}
                            className={`relative p-4 border rounded-lg mb-4 cursor-pointer transition-all duration-200 hover:bg-gray-50
                                ${selectedDocument?.id === doc.id ? 'bg-primary-50 border-primary-300' : 'border-gray-200'}`}
                            onClick={() => handleDocumentSelect(doc)}
                        >
                            <div className="flex justify-between mb-2">
                                <span className="font-semibold text-gray-800">{doc.title}</span>
                                <span className="text-sm text-gray-500">
                                    {new Date(doc.uploadedAt).toLocaleDateString()}
                                </span>
                            </div>
                            <div className="text-gray-700">{doc.description}</div>
                            <div className="mt-2 text-sm text-gray-500">
                                Uploaded by: {doc.uploadedBy}
                            </div>
                        </div>
                    ))
                )}
            </div>

            {selectedDocument && (
                <div className="flex-2 p-4 bg-white rounded-lg shadow-sm">
                    <div className="border-b border-gray-200 pb-4 mb-4">
                        <h3 className="text-xl font-semibold text-gray-800">{selectedDocument.title}</h3>
                        <div className="mt-2 text-sm text-gray-500">
                            Uploaded on: {new Date(selectedDocument.uploadedAt).toLocaleString()}
                        </div>
                    </div>

                    <div className="space-y-4">
                        <div>
                            <h4 className="text-sm font-medium text-gray-700">Description</h4>
                            <p className="mt-1 text-gray-600">{selectedDocument.description}</p>
                        </div>

                        <div>
                            <h4 className="text-sm font-medium text-gray-700">Uploaded By</h4>
                            <p className="mt-1 text-gray-600">{selectedDocument.uploadedBy}</p>
                        </div>

                        <div>
                            <h4 className="text-sm font-medium text-gray-700">File Type</h4>
                            <p className="mt-1 text-gray-600">{selectedDocument.fileType}</p>
                        </div>

                        <div className="pt-4 border-t border-gray-200">
                            <div className="flex justify-end space-x-4">
                                <button
                                    onClick={() => handleReject(selectedDocument.id)}
                                    disabled={actionLoading}
                                    className={`px-4 py-2 border border-red-300 text-red-700 rounded-md hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500
                                        ${actionLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
                                >
                                    {actionLoading ? 'Rejecting...' : 'Reject'}
                                </button>
                                <button
                                    onClick={() => handleApprove(selectedDocument.id)}
                                    disabled={actionLoading}
                                    className={`px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500
                                        ${actionLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
                                >
                                    {actionLoading ? 'Approving...' : 'Approve'}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default DocumentApproval; 