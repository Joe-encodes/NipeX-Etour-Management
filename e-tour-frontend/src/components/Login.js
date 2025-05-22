import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './Login.css';

const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [validationErrors, setValidationErrors] = useState({});
    const { login } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const validateForm = () => {
        const errors = {};
        
        if (!username.trim()) {
            errors.username = 'Username is required';
        }
        
        if (!password) {
            errors.password = 'Password is required';
        }

        setValidationErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleLogin = async (e) => {
        e.preventDefault();
        setError('');
        setValidationErrors({});

        if (!validateForm()) {
            return;
        }

        try {
            const userData = await login(username, password);
            const from = location.state?.from?.pathname || getDashboardPath(userData.role);
            navigate(from, { replace: true });
        } catch (error) {
            setError(error.response?.data?.message || 'Login failed. Please try again.');
        }
    };

    const getDashboardPath = (role) => {
        switch (role) {
            case 'User':
                return '/user-dashboard';
            case 'Approver':
                return '/approver-dashboard';
            case 'Admin':
                return '/admin-dashboard';
            default:
                return '/';
        }
    };

    return (
        <div className="login-container">
            <h2>Login</h2>
            <form onSubmit={handleLogin}>
                <div className="form-row">
                    <label htmlFor="username" className="form-label">Username:</label>
                    <input
                        id="username"
                        className="form-input"
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        required
                    />
                    {validationErrors.username && (
                        <span className="validation-error">{validationErrors.username}</span>
                    )}
                </div>
                <div className="form-row">
                    <label htmlFor="password" className="form-label">Password:</label>
                    <input
                        id="password"
                        className="form-input"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />
                    {validationErrors.password && (
                        <span className="validation-error">{validationErrors.password}</span>
                    )}
                </div>
                <button type="submit">Login</button>
            </form>
            {error && <p className="error-message">{error}</p>}
            <p>Don't have an account? <Link to="/register">Register here</Link></p>
        </div>
    );
};

export default Login;
