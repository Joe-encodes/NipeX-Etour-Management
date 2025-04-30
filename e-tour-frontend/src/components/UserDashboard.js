import React, { useState, useEffect } from 'react';
import axios from 'axios';
import './Dashboard.css';

const UserDashboard = () => {
    const [file, setFile] = useState(null);
    const [documents, setDocuments] = useState([]);
    const [message, setMessage] = useState('');

    useEffect(() => {
        fetchDocuments();
    }, []);

    const fetchDocuments = async () => {
        try {
            const token = localStorage.getItem('token');
            const response = await axios.get('http://localhost:5237/api/documents', {
                headers: { Authorization: `Bearer ${token}` }
            });
            setDocuments(response.data);
        } catch (error) {
            setMessage(`Failed to fetch documents: ${error.response?.data || error.message}`);
        }
    };

    const handleFileChange = (e) => {
        setFile(e.target.files[0]);
    };

    const handleUpload = async (e) => {
        e.preventDefault();
        setMessage('');
        if (!file) {
            setMessage('Please select a file to upload.');
            return;
        }

        const formData = new FormData();
        formData.append('file', file);
        formData.append('title', file.name);

        try {
            const token = localStorage.getItem('token');
            await axios.post('http://localhost:5237/api/documents/upload', formData, {
                headers: {
                    Authorization: `Bearer ${token}`,
                    'Content-Type': 'multipart/form-data'
                }
            });
            setMessage('Document uploaded successfully!');
            fetchDocuments();
        } catch (error) {
            setMessage(`Failed to upload: ${error.response?.data || error.message}`);
        }
    };

    const handleDownload = async (id, fileName) => {
        try {
            const token = localStorage.getItem('token');
            const response = await axios.get(`http://localhost:5237/api/documents/download/${id}`, {
                headers: { Authorization: `Bearer ${token}` },
                responseType: 'blob'
            });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', fileName);
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } catch (error) {
            setMessage(`Failed to download: ${error.response?.data || error.message}`);
        }
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        window.location.href = '/';
    };

    return (
        <div className="dashboard-container">
            <h2>User Dashboard</h2>
            <button onClick={handleLogout} style={{ backgroundColor: '#dc3545' }}>
                Logout
            </button>
            <div>
                <input type="file" onChange={handleFileChange} />
                <button onClick={handleUpload}>Upload Document</button>
            </div>
            {message && <p>{message}</p>}
            <h3>Your Documents</h3>
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
                                {doc.status === 'Signed' && (
                                    <button onClick={() => handleDownload(doc.id, doc.fileName)}>
                                        Download
                                    </button>
                                )}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
};

export default UserDashboard;