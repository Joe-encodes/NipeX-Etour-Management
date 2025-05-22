const config = {
    api: {
        baseUrl: process.env.REACT_APP_API_URL || 'http://localhost',
        timeout: 5000, // 5 second timeout
        retryAttempts: 3
    },
    features: {
        requireBackend: false // Set to false to allow frontend to work without backend
    }
};

export default config;