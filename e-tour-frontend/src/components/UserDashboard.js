import React, { useState, useEffect, useContext } from 'react';
import axios from 'axios';
import { AuthContext } from '../contexts/AuthContext';
import './UserDashboard.css';

const UserDashboard = () => {
  const [documents, setDocuments] = useState([]);
  const [error, setError] = useState('');
  const [uploading, setUploading] = useState(false);
  const { user } = useContext(AuthContext);

  useEffect(() => {
    fetchDocuments();
  }, []);

  const fetchDocuments = async () => {
    try {
      const response = await axios.get('/api/documents');
      setDocuments(Array.isArray(response.data) ? response.data : []);
      setError('');
    } catch (error) {
      setError(`Failed to fetch documents: ${error.response?.data?.message || error.message}`);
      setDocuments([]);
    }
  };

  const handleFileChange = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    setUploading(true);
    setError('');

    const formData = new FormData();
    formData.append('file', file);

    try {
      await axios.post('/api/documents', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      await fetchDocuments();
    } catch (error) {
      setError(`Failed to upload document: ${error.response?.data?.message || error.message}`);
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (documentId) => {
    try {
      const response = await axios.get(`/api/documents/${documentId}/download`, {
        responseType: 'blob',
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `document-${documentId}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      setError(`Failed to download document: ${error.response?.data?.message || error.message}`);
    }
  };

  return (
    <div className="dashboard-container">
      <h1>Welcome, {user?.username || 'User'}</h1>
      
      <div className="upload-section">
        <h2>Upload Document</h2>
        <input
          type="file"
          onChange={handleFileChange}
          disabled={uploading}
          aria-label="Upload document"
        />
        {uploading && <p>Uploading...</p>}
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="documents-section">
        <h2>Your Documents</h2>
        {documents.length === 0 ? (
          <p>No documents found.</p>
        ) : (
          <div className="documents-list">
            {documents.map(doc => (
              <div key={doc.id} className="document-item">
                <span>{doc.title}</span>
                <span className="document-status">{doc.status}</span>
                <button
                  onClick={() => handleDownload(doc.id)}
                  disabled={doc.status !== 'Signed'}
                >
                  Download
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default UserDashboard;