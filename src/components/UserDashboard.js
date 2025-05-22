import React, { useState, useEffect, useContext } from 'react';
import axios from 'axios';
import { AuthContext } from '../contexts/AuthContext';

const UserDashboard = () => {
  const { user } = useContext(AuthContext);
  const [documents, setDocuments] = useState([]);
  const [error, setError] = useState(null);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    const fetchDocuments = async () => {
      try {
        const response = await axios.get('/api/documents');
        setDocuments(response.data);
      } catch (err) {
        setError('Failed to fetch documents: ' + err.message);
      }
    };
    fetchDocuments();
  }, []);

  const handleFileUpload = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    setUploading(true);
    setError(null);

    const formData = new FormData();
    formData.append('file', file);

    try {
      await axios.post('/api/documents', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      // Refresh documents after upload
      const response = await axios.get('/api/documents');
      setDocuments(response.data);
    } catch (err) {
      setError('Failed to upload document: ' + err.message);
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (documentId) => {
    try {
      const response = await axios.get(`/api/documents/${documentId}/download`, {
        responseType: 'blob'
      });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'document.pdf');
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      setError('Failed to download document: ' + err.message);
    }
  };

  const renderDocuments = () => {
    if (!documents || documents.length === 0) {
      return <p>No documents found.</p>;
    }
    
    return documents.map(doc => (
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
    ));
  };

  return (
    <div className="dashboard-container">
      <h1>Welcome, {user?.username || 'User'}</h1>
      <div className="upload-section">
        <h2>Upload Document</h2>
        <input
          type="file"
          aria-label="Upload document"
          onChange={handleFileUpload}
          disabled={uploading}
        />
        {uploading && <p>Uploading...</p>}
      </div>
      {error && <div className="error-message">{error}</div>}
      <div className="documents-section">
        <h2>Your Documents</h2>
        {renderDocuments()}
      </div>
    </div>
  );
};

export default UserDashboard; 