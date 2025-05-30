import api from './api';
import config from '../config';

const documentService = {
    // Get all documents
    getAllDocuments: async () => {
        try {
            const response = await api.get('/documents');
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get document by ID
    getDocumentById: async (id) => {
        try {
            const response = await api.get(`/documents/${id}`);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Upload new document with progress tracking and retry logic
    uploadDocument: async (formData, onProgress) => {
        // Validate file size before upload
        const file = formData.get('file');
        if (file && file.size > config.upload.maxFileSize) {
            throw new Error(`File size exceeds the maximum limit of ${config.upload.maxFileSize / (1024 * 1024)}MB`);
        }

        // Validate file type
        const fileType = file?.type || '';
        const allowedTypes = config.upload.allowedFileTypes.map(type => `application/${type}`);
        if (!allowedTypes.includes(fileType)) {
            throw new Error(`Invalid file type. Allowed types: ${config.upload.allowedFileTypes.join(', ')}`);
        }

        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.post('/documents/upload', formData, {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                    onUploadProgress: (progressEvent) => {
                        if (onProgress) {
                            const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                            onProgress(percentCompleted);
                        }
                    },
                    timeout: config.api.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                // Handle specific error cases
                if (error.response) {
                    switch (error.response.status) {
                        case 413:
                            throw new Error('File size too large');
                        case 415:
                            throw new Error('Unsupported file type');
                        case 400:
                            throw new Error(error.response.data?.message || 'Invalid file');
                        default:
                            if (error.response.status !== 408 && error.response.status !== 500) {
                                throw error;
                            }
                    }
                }

                // If we've exhausted all attempts, throw the error
                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Upload timed out. Please try again.');
                    }
                    throw error;
                }

                // Wait before retrying with exponential backoff
                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * Math.pow(2, attempts - 1)));
            }
        }
    },

    // Update document status
    updateDocumentStatus: async (id, status) => {
        try {
            const response = await api.put(`/documents/${id}/status`, { status });
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Delete document
    deleteDocument: async (id) => {
        try {
            const response = await api.delete(`/documents/${id}`);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get user's documents
    getUserDocuments: async () => {
        try {
            const response = await api.get('/documents/user');
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get pending approvals
    getPendingApprovals: async () => {
        try {
            const response = await api.get('/documents/pending');
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Download document with retry logic
    downloadDocument: async (id) => {
        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.get(`/documents/${id}/download`, {
                    responseType: 'blob',
                    timeout: config.api.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                // Don't retry if it's not a network error or timeout
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    throw error;
                }

                // If we've exhausted all attempts, throw the error
                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Download timed out. Please try again.');
                    }
                    throw error;
                }

                // Wait before retrying
                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * attempts));
            }
        }
    }
};

export default documentService; 