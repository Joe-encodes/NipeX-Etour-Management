import React, { useState } from 'react';
import axios from 'axios';
import { Link } from 'react-router-dom';
import './Login.css';
import config from '../config'; // config file for API base URL


const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');

    const handleLogin = async (e) => {
        e.preventDefault();
        setMessage('');

        try {
            const response = await axios.post(`${config.api.baseUrl}/api/auth/login`, {
                username,
                password
            });
            localStorage.setItem('token', response.data.token);

            if (response.data.role === 'User') {
                window.location.href = '/user-dashboard';
            } else if (response.data.role === 'Approver') {
                window.location.href = '/approver-dashboard';
            } else if (response.data.role === 'Admin') {
                window.location.href = '/admin-dashboard';
            }
        } catch (error) {
            setMessage(`Login failed: ${error.response?.data || error.message}`);
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
                </div>
                <button type="submit">Login</button>
            </form>
            {message && <p className="error-message">{message}</p>}
            <p>Don't have an account? <Link to="/register">Register here</Link></p>
        </div>
    );
};

export default Login;
