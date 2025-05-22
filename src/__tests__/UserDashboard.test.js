import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import axios from 'axios';
import UserDashboard from '../components/UserDashboard';
import { renderWithProviders } from './testUtils';
import { mockUser, mockDocument } from './testUtils';

jest.mock('axios');

describe('UserDashboard', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    
    // Mock successful API responses
    axios.get.mockImplementation((url) => {
      if (url === '/api/documents') {
        return Promise.resolve({ data: [mockDocument] });
      }
      if (url.includes('/download')) {
        return Promise.resolve({ data: new Blob(['test']) });
      }
      return Promise.reject(new Error('Not found'));
    });

    axios.post.mockImplementation((url) => {
      if (url === '/api/documents') {
        return Promise.resolve({ data: mockDocument });
      }
      return Promise.reject(new Error('Not found'));
    });
  });

  test('renders user dashboard with documents', async () => {
    renderWithProviders(<UserDashboard />);
    
    // Wait for documents to load
    await waitFor(() => {
      expect(screen.getByText(mockUser.username)).toBeInTheDocument();
    });
    
    // Check if document is displayed
    expect(screen.getByText(mockDocument.title)).toBeInTheDocument();
    expect(screen.getByText(mockDocument.status)).toBeInTheDocument();
  });

  test('handles document upload', async () => {
    renderWithProviders(<UserDashboard />);
    
    const file = new File(['test'], 'test.pdf', { type: 'application/pdf' });
    const fileInput = screen.getByLabelText(/upload document/i);
    
    fireEvent.change(fileInput, { target: { files: [file] } });
    
    // Wait for upload to complete
    await waitFor(() => {
      expect(axios.post).toHaveBeenCalledWith(
        '/api/documents',
        expect.any(FormData),
        expect.any(Object)
      );
    });

    // Verify documents are refreshed after upload
    expect(axios.get).toHaveBeenCalledWith('/api/documents');
  });

  test('handles document download', async () => {
    renderWithProviders(<UserDashboard />);
    
    // Wait for documents to load
    await waitFor(() => {
      expect(screen.getByText(mockDocument.title)).toBeInTheDocument();
    });
    
    const downloadButton = screen.getByText(/download/i);
    fireEvent.click(downloadButton);
    
    // Wait for download to complete
    await waitFor(() => {
      expect(axios.get).toHaveBeenCalledWith(
        `/api/documents/${mockDocument.id}/download`,
        expect.any(Object)
      );
    });
  });

  test('handles document upload error', async () => {
    axios.post.mockRejectedValueOnce(new Error('Upload failed'));
    
    renderWithProviders(<UserDashboard />);
    
    const file = new File(['test'], 'test.pdf', { type: 'application/pdf' });
    const fileInput = screen.getByLabelText(/upload document/i);
    
    fireEvent.change(fileInput, { target: { files: [file] } });
    
    await waitFor(() => {
      expect(screen.getByText(/Failed to upload document/)).toBeInTheDocument();
    });
  });

  test('handles document download error', async () => {
    axios.get.mockRejectedValueOnce(new Error('Download failed'));
    
    renderWithProviders(<UserDashboard />);
    
    await waitFor(() => {
      expect(screen.getByText(mockDocument.title)).toBeInTheDocument();
    });
    
    const downloadButton = screen.getByText(/download/i);
    fireEvent.click(downloadButton);
    
    await waitFor(() => {
      expect(screen.getByText(/Failed to download document/)).toBeInTheDocument();
    });
  });

  test('disables download button for unsigned documents', async () => {
    const unsignedDocument = { ...mockDocument, status: 'Pending' };
    axios.get.mockResolvedValueOnce({ data: [unsignedDocument] });
    
    renderWithProviders(<UserDashboard />);
    
    await waitFor(() => {
      const downloadButton = screen.getByText(/download/i);
      expect(downloadButton).toBeDisabled();
    });
  });
}); 