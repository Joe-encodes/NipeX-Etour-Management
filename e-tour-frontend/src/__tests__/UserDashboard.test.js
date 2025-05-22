import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import axios from 'axios';
import UserDashboard from '../components/UserDashboard';
import { renderWithProviders } from './testUtils';

jest.mock('axios');

// Add debug logging
const debug = {
  log: (message, data = null) => {
    console.log(`[UserDashboard Test Debug] ${message}`, data ? data : '');
  }
};

describe('UserDashboard', () => {
  jest.setTimeout(15000); // Set timeout for all tests in this suite

  const mockUser = {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    role: 'User'
  };

  const mockDocument = {
    id: 1,
    title: 'Test Document',
    status: 'Signed',
    createdAt: '2024-03-20T10:00:00Z'
  };

  beforeEach(() => {
    debug.log('Clearing all mocks');
    jest.clearAllMocks();
    // Mock successful document fetch by default
    debug.log('Setting up default successful document fetch mock');
    axios.get.mockResolvedValueOnce({ data: [mockDocument] });
  });

  const renderDashboard = () => {
    debug.log('Rendering dashboard with providers', {
      user: mockUser,
      route: '/user-dashboard'
    });
    return renderWithProviders(
      <UserDashboard />,
      {
        user: mockUser,
        route: '/user-dashboard',
        authContextValue: {
          user: mockUser,
          isAuthenticated: true
        }
      }
    );
  };

  it('renders user dashboard with documents', async () => {
    debug.log('Starting dashboard render test');
    renderDashboard();

    // Check if user info is displayed
    debug.log('Checking user info display');
    expect(screen.getByText(`Welcome, ${mockUser.username}`)).toBeInTheDocument();
    
    // Check if document is displayed
    debug.log('Waiting for document display');
    await waitFor(() => {
      const elements = {
        title: screen.getByText(mockDocument.title),
        status: screen.getByText(mockDocument.status)
      };
      debug.log('Found document elements', elements);
      expect(elements.title).toBeInTheDocument();
      expect(elements.status).toBeInTheDocument();
    });
  });

  it('handles document upload', async () => {
    debug.log('Starting document upload test');
    const file = new File(['test'], 'test.pdf', { type: 'application/pdf' });
    debug.log('Created test file', { name: file.name, type: file.type });
    
    axios.post.mockResolvedValueOnce({ data: { message: 'Document uploaded successfully' } });
    debug.log('Mocked successful upload response');
    
    renderDashboard();
    
    const fileInput = screen.getByLabelText('Upload document');
    debug.log('Found file input element');
    
    fireEvent.change(fileInput, { target: { files: [file] } });
    debug.log('Triggered file change event');

    await waitFor(() => {
      debug.log('Verifying upload API call');
      expect(axios.post).toHaveBeenCalledWith(
        '/api/documents',
        expect.any(FormData),
        expect.any(Object)
      );
    });
  });

  it('handles document download', async () => {
    debug.log('Starting document download test');
    const mockBlob = new Blob(['test'], { type: 'application/pdf' });
    debug.log('Created mock blob', { type: mockBlob.type });
    
    axios.get.mockResolvedValueOnce({ data: [mockDocument] }); // Initial fetch
    axios.get.mockResolvedValueOnce({ data: mockBlob }); // Download request
    debug.log('Mocked API responses for fetch and download');
    
    renderDashboard();

    // Wait for the document to be displayed
    debug.log('Waiting for document display');
    await waitFor(() => {
      const titleElement = screen.getByText(mockDocument.title);
      debug.log('Found document title element');
      expect(titleElement).toBeInTheDocument();
    });

    // Find and click the download button
    const downloadButton = screen.getByRole('button', { name: /download/i });
    debug.log('Found download button');
    fireEvent.click(downloadButton);
    debug.log('Clicked download button');

    expect(axios.get).toHaveBeenCalledWith(
      `/api/documents/${mockDocument.id}/download`,
      expect.any(Object)
    );
    debug.log('Verified download API call');
  });

  it('handles document upload error', async () => {
    debug.log('Starting document upload error test');
    const file = new File(['test'], 'test.pdf', { type: 'application/pdf' });
    const errorMessage = 'Failed to upload document';
    debug.log('Created test file and error message', { errorMessage });
    
    axios.post.mockRejectedValueOnce({
      response: {
        data: {
          message: errorMessage
        }
      }
    });
    debug.log('Mocked upload error response');
    
    renderDashboard();
    
    const fileInput = screen.getByLabelText('Upload document');
    debug.log('Found file input element');
    fireEvent.change(fileInput, { target: { files: [file] } });
    debug.log('Triggered file change event');

    await waitFor(() => {
      const errorElement = screen.getByText(`Failed to upload document: ${errorMessage}`);
      debug.log('Found error message element', { text: errorElement.textContent });
      expect(errorElement).toBeInTheDocument();
    });
  });
});
