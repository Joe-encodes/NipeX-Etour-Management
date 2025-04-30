import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import './Dashboard.css';

const AdminDashboard = () => {
    const [documents, setDocuments] = useState([]);
    const [users, setUsers] = useState([]);
    const [message, setMessage] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        try {
            const token = localStorage.getItem('token');
            const [docsResponse, usersResponse] = await Promise.all([
                axios.get('http://localhost:5237/api/documents/all-documents', {
                    headers: { Authorization: `Bearer ${token}` }
                }),
                axios.get('http://localhost:5237/api/documents/users', {
                    headers: { Authorization: `Bearer ${token}` }
                })
            ]);
            setDocuments(docsResponse.data);
            setUsers(usersResponse.data);
        } catch (err) {
            setMessage('Error fetching data: ' + (err.response?.data || err.message));
        }
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        navigate('/');
    };

    return (
        <div className="dashboard-container">
            <h2>Admin Dashboard</h2>
            <button onClick={handleLogout} style={{ backgroundColor: '#dc3545' }}>
                Logout
            </button>
            {message && <p>{message}</p>}
            <h3>All Documents</h3>
            {documents.length === 0 ? (
                <p>No documents in the system.</p>
            ) : (
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>File Name</th>
                            <th>Status</th>
                            <th>User ID</th>
                        </tr>
                    </thead>
                    <tbody>
                        {documents.map(doc => (
                            <tr key={doc.id}>
                                <td>{doc.id}</td>
                                <td>{doc.fileName}</td>
                                <td>{doc.status}</td>
                                <td>{doc.userId}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
            <h3>All Users</h3>
            {users.length === 0 ? (
                <p>No users in the system.</p>
            ) : (
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Username</th>
                            <th>Role</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(user => (
                            <tr key={user.id}>
                                <td>{user.id}</td>
                                <td>{user.username}</td>
                                <td>{user.role}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );
};

export default AdminDashboard;