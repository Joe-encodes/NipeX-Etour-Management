// Environment variables with fallbacks
const config = {
    // API Configuration
    API_URL: process.env.REACT_APP_API_URL || 'https://localhost:7078/api',
    
    // Authentication
    JWT_STORAGE_KEY: process.env.REACT_APP_JWT_STORAGE_KEY || 'etour_token',
    REFRESH_TOKEN_KEY: process.env.REACT_APP_REFRESH_TOKEN_KEY || 'etour_refresh_token',
    
    // Feature Flags
    ENABLE_ANALYTICS: process.env.REACT_APP_ENABLE_ANALYTICS === 'true',
    ENABLE_NOTIFICATIONS: process.env.REACT_APP_ENABLE_NOTIFICATIONS === 'true',
    
    // File Upload
    MAX_FILE_SIZE: parseInt(process.env.REACT_APP_MAX_FILE_SIZE || '10485760'), // 10MB default
    ALLOWED_FILE_TYPES: (process.env.REACT_APP_ALLOWED_FILE_TYPES || 'application/pdf').split(','),
    
    // UI Configuration
    ITEMS_PER_PAGE: parseInt(process.env.REACT_APP_ITEMS_PER_PAGE || '10'),
    TRUNCATE_LENGTH: parseInt(process.env.REACT_APP_TRUNCATE_LENGTH || '15'),
    
    // Session
    SESSION_TIMEOUT: parseInt(process.env.REACT_APP_SESSION_TIMEOUT || '30'), // minutes
    
    // Environment
    NODE_ENV: process.env.NODE_ENV || 'development',
    IS_PRODUCTION: process.env.NODE_ENV === 'production',
    
    // Version
    APP_VERSION: process.env.REACT_APP_VERSION || '1.0.0'
};

// Validate required environment variables
const requiredEnvVars = ['REACT_APP_API_URL'];
const missingEnvVars = requiredEnvVars.filter(envVar => !process.env[envVar]);

if (missingEnvVars.length > 0 && config.NODE_ENV === 'production') {
    console.error('Missing required environment variables:', missingEnvVars);
    throw new Error('Missing required environment variables');
}

export default config; 