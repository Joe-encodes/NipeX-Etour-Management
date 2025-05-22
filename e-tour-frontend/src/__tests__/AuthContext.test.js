import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { AuthProvider, useAuth } from '../contexts/AuthContext';
import axios from 'axios';

jest.mock('axios');

// Test component that uses the auth context
const TestComponent = () => {
  const auth = useAuth();
  return (
    <div>
      <div data-testid="auth-status">
        {auth.user ? `Logged in as ${auth.user.username}` : 'Not logged in'}
      </div>
      <div data-testid="loading-status">
        {auth.loading ? 'Loading...' : 'Not loading'}
      </div>
      <div data-testid="error-message">
        {auth.error || 'No error'}
      </div>
      <button onClick={() => auth.login('testuser', 'password')}>Login</button>
      <button onClick={() => auth.register({ username: 'testuser', password: 'password', email: 'test@example.com' })}>Register</button>
      <button onClick={auth.logout}>Logout</button>
    </div>
  );
};

describe('AuthContext', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  test('provides initial authentication state', () => {
    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    expect(screen.getByTestId('auth-status')).toHaveTextContent('Not logged in');
    expect(screen.getByTestId('loading-status')).toHaveTextContent('Not loading');
    expect(screen.getByTestId('error-message')).toHaveTextContent('No error');
  });

  test('handles successful login', async () => {
    const mockResponse = {
      data: {
        token: 'mock-token',
        id: 1,
        username: 'testuser',
        role: 'User'
      }
    };
    axios.post.mockResolvedValueOnce(mockResponse);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    fireEvent.click(screen.getByText('Login'));

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('Logged in as testuser');
    });
    expect(localStorage.getItem('token')).toBe('mock-token');
  });

  test('handles login failure', async () => {
    const mockError = {
      response: {
        data: { message: 'Invalid credentials' }
      }
    };
    axios.post.mockRejectedValueOnce(mockError);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    fireEvent.click(screen.getByText('Login'));

    await waitFor(() => {
      expect(screen.getByTestId('error-message')).toHaveTextContent('Invalid credentials');
    });
    expect(localStorage.getItem('token')).toBeNull();
  });

  test('handles successful registration', async () => {
    const mockResponse = {
      data: {
        message: 'Registration successful'
      }
    };
    axios.post.mockResolvedValueOnce(mockResponse);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    fireEvent.click(screen.getByText('Register'));

    await waitFor(() => {
      expect(axios.post).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/register'),
        {
          username: 'testuser',
          password: 'password',
          email: 'test@example.com'
        }
      );
    });
  });

  test('handles registration failure', async () => {
    const mockError = {
      response: {
        data: { message: 'Username already exists' }
      }
    };
    axios.post.mockRejectedValueOnce(mockError);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    fireEvent.click(screen.getByText('Register'));

    await waitFor(() => {
      expect(screen.getByTestId('error-message')).toHaveTextContent('Username already exists');
    });
  });

  test('handles logout', async () => {
    // First login
    const mockResponse = {
      data: {
        token: 'mock-token',
        id: 1,
        username: 'testuser',
        role: 'User'
      }
    };
    axios.post.mockResolvedValueOnce(mockResponse);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    fireEvent.click(screen.getByText('Login'));

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('Logged in as testuser');
    });

    // Then logout
    fireEvent.click(screen.getByText('Logout'));

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('Not logged in');
    });
    expect(localStorage.getItem('token')).toBeNull();
  });

  test('checks user role correctly', async () => {
    const mockResponse = {
      data: {
        token: 'mock-token',
        id: 1,
        username: 'testuser',
        role: 'Admin'
      }
    };
    axios.post.mockResolvedValueOnce(mockResponse);

    const RoleTestComponent = () => {
      const auth = useAuth();
      return (
        <div>
          <div data-testid="is-admin">{auth.hasRole('Admin') ? 'Is Admin' : 'Not Admin'}</div>
          <div data-testid="is-user">{auth.hasRole('User') ? 'Is User' : 'Not User'}</div>
          <button onClick={() => auth.login('testuser', 'password')}>Login</button>
        </div>
      );
    };

    render(
      <AuthProvider>
        <RoleTestComponent />
      </AuthProvider>
    );

    fireEvent.click(screen.getByText('Login'));

    await waitFor(() => {
      expect(screen.getByTestId('is-admin')).toHaveTextContent('Is Admin');
      expect(screen.getByTestId('is-user')).toHaveTextContent('Not User');
    });
  });

  test('handles token persistence on page reload', async () => {
    localStorage.setItem('token', 'mock-token');
    
    const mockProfileResponse = {
      data: {
        id: 1,
        username: 'testuser',
        role: 'User'
      }
    };
    axios.get.mockResolvedValueOnce(mockProfileResponse);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('Logged in as testuser');
    });
  });
}); 