import api from './api';
import config from '../config';

const messageService = {
    // Get all messages
    getAllMessages: async () => {
        try {
            const response = await api.get('/messages');
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get message by ID
    getMessageById: async (id) => {
        try {
            const response = await api.get(`/messages/${id}`);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Send message with retry logic
    sendMessage: async (receiverId, content, subject) => {
        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.post('/messages', {
                    receiverId,
                    content,
                    subject
                }, {
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
                        throw new Error('Request timed out. Please try again.');
                    }
                    throw error;
                }

                // Wait before retrying
                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * attempts));
            }
        }
    },

    // Mark message as read with retry logic
    markAsRead: async (id) => {
        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.post(`/messages/${id}/read`, {}, {
                    timeout: config.api.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    throw error;
                }

                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Request timed out. Please try again.');
                    }
                    throw error;
                }

                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * attempts));
            }
        }
    },

    // Delete message with retry logic
    deleteMessage: async (id) => {
        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.delete(`/messages/${id}`, {
                    timeout: config.api.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    throw error;
                }

                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Request timed out. Please try again.');
                    }
                    throw error;
                }

                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * attempts));
            }
        }
    },

    // Get user messages with retry logic
    getUserMessages: async (includeRead = true) => {
        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.get(`/messages?includeRead=${includeRead}`, {
                    timeout: config.api.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    throw error;
                }

                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Request timed out. Please try again.');
                    }
                    throw error;
                }

                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * attempts));
            }
        }
    },

    // Get single message with retry logic
    getMessage: async (id) => {
        let attempts = 0;
        const maxAttempts = config.api.retryAttempts;

        while (attempts < maxAttempts) {
            try {
                const response = await api.get(`/messages/${id}`, {
                    timeout: config.api.timeout
                });
                return response.data;
            } catch (error) {
                attempts++;
                
                if (error.response && error.response.status !== 408 && error.response.status !== 500) {
                    throw error;
                }

                if (attempts === maxAttempts) {
                    if (error.code === 'ECONNABORTED') {
                        throw new Error('Request timed out. Please try again.');
                    }
                    throw error;
                }

                await new Promise(resolve => setTimeout(resolve, config.api.retryDelay * attempts));
            }
        }
    },

    // Get unread message count
    getUnreadCount: async () => {
        try {
            const response = await api.get('/messages/unread/count');
            return response.data;
        } catch (error) {
            throw error;
        }
    }
};

export default messageService; 