import api from './api';

const userService = {
    // Get current user profile
    getCurrentUser: async () => {
        try {
            const response = await api.get('/users/me');
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Update user profile
    updateProfile: async (userData) => {
        try {
            const response = await api.put('/users/profile', userData);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Change password
    changePassword: async (passwordData) => {
        try {
            const response = await api.put('/users/change-password', passwordData);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get user by ID
    getUserById: async (id) => {
        try {
            const response = await api.get(`/users/${id}`);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get all users (admin only)
    getAllUsers: async () => {
        try {
            const response = await api.get('/users');
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Update user role (admin only)
    updateUserRole: async (userId, role) => {
        try {
            const response = await api.put(`/users/${userId}/role`, { role });
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Delete user (admin only)
    deleteUser: async (userId) => {
        try {
            const response = await api.delete(`/users/${userId}`);
            return response.data;
        } catch (error) {
            throw error;
        }
    },

    // Get user statistics
    getUserStats: async () => {
        try {
            const response = await api.get('/users/stats');
            return response.data;
        } catch (error) {
            throw error;
        }
    }
};

export default userService; 