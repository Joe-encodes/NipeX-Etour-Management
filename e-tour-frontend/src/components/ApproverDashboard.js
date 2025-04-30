import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import './Dashboard.css';

const ApproverDashboard = () => {
    const [documents, setDocuments] = useState([]);
    const [signature, setSignature] = useState('');
    const [message, setMessage] = useState('');
    const [loading, setLoading] = useState(false); // Added for loading state
    const navigate = useNavigate();

    useEffect(() => {
        fetchDocuments();
    }, []);

    const fetchDocuments = async () => {
        try {
            setLoading(true);
            const token = localStorage.getItem('token');
            const response = await axios.get('http://localhost:5237/api/documents/pending', {
                headers: { Authorization: `Bearer ${token}` }
            });
            setDocuments(response.data);
            setMessage(''); // Clear any previous messages
        } catch (err) {
            setMessage('Failed to fetch documents: ' + (err.response?.data || err.message));
        } finally {
            setLoading(false);
        }
    };

    const handleApprove = async (documentId) => {
        if (!signature.trim()) {
            setMessage('Please enter a valid signature.');
            return;
        }
        try {
            setLoading(true);
            setMessage('Approving document...');
            const token = localStorage.getItem('token');
            const response = await axios.post(
                `http://localhost:5237/api/documents/approve/${documentId}`,
                { signature },
                { headers: { Authorization: `Bearer ${token}` } }
            );
            setMessage(response.data.message || 'Document approved successfully');
            fetchDocuments(); // Refresh list
        } catch (err) {
            const errorMessage = err.response?.data || err.message;
            setMessage('Failed to approve document: ' + errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        navigate('/');
    };

    return (
        <div className="dashboard-container">
            <h2>Approver Dashboard</h2>
            <button onClick={handleLogout} style={{ backgroundColor: '#dc3545' }} disabled={loading}>
                Logout
            </button>
            {message && <p style={{ color: message.includes('Failed') ? 'red' : 'green' }}>{message}</p>}
            <div>
                <label>Signature:</label>
                <input
                    type="text"
                    value={signature}
                    onChange={(e) => setSignature(e.target.value)}
                    placeholder="Enter your signature"
                    required
                    disabled={loading}
                />
            </div>
            <h3>Pending Documents</h3>
            {loading ? (
                <p>Loading...</p>
            ) : documents.length === 0 ? (
                <p>No pending documents.</p>
            ) : (
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>File Name</th>
                            <th>Status</th>
                            <th>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {documents.map(doc => (
                            <tr key={doc.id}>
                                <td>{doc.id}</td>
                                <td>{doc.fileName}</td>
                                <td>{doc.status}</td>
                                <td>
                                    <button
                                        onClick={() => handleApprove(doc.id)}
                                        disabled={loading}
                                    >
                                        Approve
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );
};

export default ApproverDashboard;