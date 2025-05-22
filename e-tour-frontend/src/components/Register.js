import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './Register.css';

const Register = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [email, setEmail] = useState('');
    const [role, setRole] = useState('User');
    const [error, setError] = useState('');
    const [validationErrors, setValidationErrors] = useState({});
    const { register } = useAuth();
    const navigate = useNavigate();

    const validateForm = () => {
        const errors = {};
        
        if (!username.trim()) {
            errors.username = 'Username is required';
        }
        
        if (!email.trim()) {
            errors.email = 'Email is required';
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            errors.email = 'Invalid email format';
        }
        
        if (!password) {
            errors.password = 'Password is required';
        } else if (password.length < 8) {
            errors.password = 'Password must be at least 8 characters';
        }

        if (!confirmPassword) {
            errors.confirmPassword = 'Confirm password is required';
        } else if (password !== confirmPassword) {
            errors.confirmPassword = 'Passwords do not match';
        }

        setValidationErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleRegister = async (e) => {
        e.preventDefault();
        setError('');
        setValidationErrors({});

        if (!validateForm()) {
            return;
        }

        try {
            await register({ username, password, email, role });
            navigate('/', { state: { message: 'Registration successful! Please log in.' } });
        } catch (error) {
            setError(error.response?.data?.message || 'Registration failed. Please try again.');
        }
    };

    return (
        <div className="register-container">
            <h2>Register</h2>
            <form onSubmit={handleRegister}>
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
                <div className="form-row">
                    <label htmlFor="confirmPassword" className="form-label">Confirm Password:</label>
                    <input
                        id="confirmPassword"
                        className="form-input"
                        type="password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                    />
                    {validationErrors.confirmPassword && (
                        <span className="validation-error">{validationErrors.confirmPassword}</span>
                    )}
                </div>
                <div className="form-row">
                    <label htmlFor="email" className="form-label">Email:</label>
                    <input
                        id="email"
                        className="form-input"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />
                    {validationErrors.email && (
                        <span className="validation-error">{validationErrors.email}</span>
                    )}
                </div>
                <div className="form-row">
                    <label htmlFor="role" className="form-label">Role:</label>
                    <select
                        id="role"
                        className="form-input"
                        value={role}
                        onChange={(e) => setRole(e.target.value)}
                    >
                        <option value="User">User</option>
                        <option value="Approver">Approver</option>
                        <option value="Admin">Admin</option>
                    </select>
                </div>
                <button type="submit">Register</button>
            </form>
            {error && <p className="error-message">{error}</p>}
            <p>Already have an account? <Link to="/">Login here</Link></p>
        </div>
    );
};

export default Register;