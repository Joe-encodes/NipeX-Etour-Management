import React, { useState } from 'react';
import documentService from '../services/documentService';
import config from '../config';

const UploadDocument = () => {
    const [file, setFile] = useState(null);
    const [description, setDescription] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);

    const handleFileChange = (e) => {
        const selectedFile = e.target.files[0];
        if (selectedFile) {
            // Check file size
            if (selectedFile.size > config.upload.maxFileSize) {
                setError(`File size must be less than ${config.upload.maxFileSize / (1024 * 1024)}MB`);
                return;
            }
            // Check file type
            const fileExtension = selectedFile.name.split('.').pop().toLowerCase();
            if (!config.upload.allowedFileTypes.includes(fileExtension)) {
                setError(`Invalid file type. Please upload ${config.upload.allowedFileTypes.join(', ').toUpperCase()} files only.`);
                return;
            }
            setFile(selectedFile);
            setError(null);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!file) {
            setError('Please select a file to upload');
            return;
        }

        setLoading(true);
        setError(null);
        setSuccess(false);
        setUploadProgress(0);

        try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('description', description);

            const response = await documentService.uploadDocument(formData, (progress) => {
                setUploadProgress(Math.round((progress.loaded * 100) / progress.total));
            });

            setSuccess(true);
            setFile(null);
            setDescription('');
            setUploadProgress(0);
        } catch (err) {
            if (err.code === 'ECONNABORTED') {
                setError('Upload timed out. Please try again.');
            } else if (err.response?.status === 413) {
                setError('File is too large. Please choose a smaller file.');
            } else {
                setError(err.response?.data?.message || 'Failed to upload document');
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="max-w-2xl mx-auto p-6">
            <h2 className="text-2xl font-bold mb-6 text-gray-800">Upload Document</h2>
            
            <form onSubmit={handleSubmit} className="space-y-6">
                <div className="space-y-2">
                    <label className="block text-sm font-medium text-gray-700">
                        Select Document
                    </label>
                    <div className="mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-gray-300 border-dashed rounded-lg hover:border-primary-500 transition-colors duration-200">
                        <div className="space-y-1 text-center">
                            <svg
                                className="mx-auto h-12 w-12 text-gray-400"
                                stroke="currentColor"
                                fill="none"
                                viewBox="0 0 48 48"
                                aria-hidden="true"
                            >
                                <path
                                    d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
                                    strokeWidth={2}
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                />
                            </svg>
                            <div className="flex text-sm text-gray-600">
                                <label
                                    htmlFor="file-upload"
                                    className="relative cursor-pointer bg-white rounded-md font-medium text-primary-600 hover:text-primary-500 focus-within:outline-none focus-within:ring-2 focus-within:ring-offset-2 focus-within:ring-primary-500"
                                >
                                    <span>Upload a file</span>
                                    <input
                                        id="file-upload"
                                        name="file-upload"
                                        type="file"
                                        className="sr-only"
                                        onChange={handleFileChange}
                                        accept={config.upload.allowedFileTypes.map(ext => `.${ext}`).join(',')}
                                    />
                                </label>
                                <p className="pl-1">or drag and drop</p>
                            </div>
                            <p className="text-xs text-gray-500">
                                {config.upload.allowedFileTypes.join(', ').toUpperCase()} up to {config.upload.maxFileSize / (1024 * 1024)}MB
                            </p>
                        </div>
                    </div>
                    {file && (
                        <p className="mt-2 text-sm text-gray-600">
                            Selected file: {file.name}
                        </p>
                    )}
                </div>

                <div className="space-y-2">
                    <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                        Description
                    </label>
                    <textarea
                        id="description"
                        name="description"
                        rows={4}
                        className="shadow-sm focus:ring-primary-500 focus:border-primary-500 block w-full sm:text-sm border-gray-300 rounded-md"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Enter document description..."
                    />
                </div>

                {error && (
                    <div className="rounded-md bg-red-50 p-4">
                        <div className="flex">
                            <div className="ml-3">
                                <h3 className="text-sm font-medium text-red-800">
                                    {error}
                                </h3>
                            </div>
                        </div>
                    </div>
                )}

                {success && (
                    <div className="rounded-md bg-green-50 p-4">
                        <div className="flex">
                            <div className="ml-3">
                                <h3 className="text-sm font-medium text-green-800">
                                    Document uploaded successfully!
                                </h3>
                            </div>
                        </div>
                    </div>
                )}

                {loading && (
                    <div className="w-full bg-gray-200 rounded-full h-2.5">
                        <div 
                            className="bg-primary-600 h-2.5 rounded-full transition-all duration-300"
                            style={{ width: `${uploadProgress}%` }}
                        ></div>
                        <p className="text-sm text-gray-600 mt-2 text-center">
                            Uploading... {uploadProgress}%
                        </p>
                    </div>
                )}

                <div className="flex justify-end">
                    <button
                        type="submit"
                        disabled={loading}
                        className={`inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white 
                            ${loading 
                                ? 'bg-primary-400 cursor-not-allowed' 
                                : 'bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500'
                            }`}
                    >
                        {loading ? 'Uploading...' : 'Upload Document'}
                    </button>
                </div>
            </form>
        </div>
    );
};

export default UploadDocument; 