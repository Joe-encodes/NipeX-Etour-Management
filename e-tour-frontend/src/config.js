const config = {
    api: {
        baseURL: process.env.REACT_APP_API_URL || 'https://localhost:7078/api',
        timeout: parseInt(process.env.API_TIMEOUT || '30') * 1000, // Convert to milliseconds
        retryAttempts: 3,
        retryDelay: 1000, // 1 second
    },
    features: {
        requireBackend: process.env.REACT_APP_ENV === 'production'
    },
    auth: {
        tokenKey: 'auth_token',
        refreshTokenKey: 'refresh_token',
        tokenExpiryKey: 'token_expiry',
    },
    app: {
        name: process.env.REACT_APP_NAME || 'E-Tour Management',
        version: process.env.REACT_APP_VERSION || '1.0.0',
        description: process.env.REACT_APP_DESCRIPTION || 'Electronic Tour Management System'
    },
    upload: {
        maxFileSize: parseInt(process.env.REACT_APP_MAX_FILE_SIZE || '5242880'), // 5MB default
        allowedFileTypes: (process.env.REACT_APP_ALLOWED_FILE_TYPES || 'jpg,jpeg,png,pdf,doc,docx').split(',').map(type => {
            switch(type.trim()) {
                case 'jpg':
                case 'jpeg':
                    return 'image/jpeg';
                case 'png':
                    return 'image/png';
                case 'pdf':
                    return 'application/pdf';
                case 'doc':
                    return 'application/msword';
                case 'docx':
                    return 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
                default:
                    return type.trim();
            }
        }),
        maxFilesPerUpload: 5,
    },
    session: {
        timeout: parseInt(process.env.REACT_APP_SESSION_TIMEOUT || '30') * 60 * 1000, // Convert to milliseconds
        warningTime: 5 * 60 * 1000, // 5 minutes
    }
};

export default config;