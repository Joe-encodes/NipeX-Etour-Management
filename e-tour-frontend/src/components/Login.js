import React, { useState } from 'react';
import axios from 'axios';
import { Link } from 'react-router-dom'; // Import Link from react-router-dom
import './Login.css';
const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');

    const handleLogin = async (e) => {
        e.preventDefault();
        setMessage('');

        try {
            console.log('Sending login request to backend...');
            const response = await axios.post('http://localhost:5237/api/auth/login', {
                username,
                password
            });
            console.log('Login response:', response.data);
            localStorage.setItem('token', response.data.token);
            console.log('User role:', response.data.role);

            if (response.data.role === 'User') {
                console.log('Navigating to user dashboard...');
                window.location.href = '/user-dashboard';
            } else if (response.data.role === 'Approver') {
                console.log('Navigating to approver dashboard...');
                window.location.href = '/approver-dashboard';
            } else if (response.data.role === 'Admin') {
                console.log('Navigating to admin dashboard...');
                window.location.href = '/admin-dashboard'; // Add this route if you have an admin dashboard
            }
        } catch (error) {
            console.error('Login error:', error);
            setMessage(`Login failed: ${error.response?.data || error.message}`);
        }
    };

    return (
        <div>
            <h2>Login</h2>
            <form onSubmit={handleLogin}>
                <div>
                    <label>Username:</label>
                    <input
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        required
                    />
                </div>
                <div>
                    <label>Password:</label>
                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />
                </div>
                <button type="submit">Login</button>
            </form>
            {message && <p>{message}</p>}
            <p>Don't have an account? <Link to="/register">Register here</Link></p> {/* Add the link to the register page */}
        </div>
    );
};

export default Login;