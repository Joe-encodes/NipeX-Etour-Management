import React, { useState } from 'react';
import axios from 'axios';
import { Link } from 'react-router-dom';
import './Register.css';
import config from '../config'; // config file for API base URL

const Register = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [email, setEmail] = useState('');
    const [role, setRole] = useState('User');
    const [message, setMessage] = useState('');

    const handleRegister = async (e) => {
        e.preventDefault();
        setMessage('');

        try {
            await axios.post(`${config.api.baseUrl}/api/auth/register`, {
                username,
                password,
                email,
                role
            });
            setMessage('Registration successful! You can now log in.');
        } catch (error) {
            setMessage(`Registration failed: ${error.response?.data || error.message}`);
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
            {message && <p className="error-message">{message}</p>}
            <p>Already have an account? <Link to="/">Login here</Link></p>
        </div>
    );
};

export default Register;