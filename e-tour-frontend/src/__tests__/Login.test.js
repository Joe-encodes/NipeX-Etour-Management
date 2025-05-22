import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { render } from '../test-utils';
import Login from '../components/Login';

describe('Login', () => {
  let mockLogin;

  beforeEach(() => {
    mockLogin = jest.fn();
  });

  const renderLogin = (authContextValue = {}) => {
    return render(
      <Login />, 
      {
        route: '/login',
        initialState: {
          user: authContextValue.user || null,
          login: mockLogin
        }
      }
    );
  };

  it('renders login form', () => {
    renderLogin();
    expect(screen.getByRole('heading', { name: /login/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
  });

  it('renders login form with correct structure', () => {
    renderLogin();
    // Check for main container
    expect(screen.getByRole('heading', { name: 'Login' })).toBeInTheDocument();
    
    // Check for form elements
    const usernameInput = screen.getByLabelText('Username:');
    const passwordInput = screen.getByLabelText('Password:');
    const submitButton = screen.getByRole('button', { name: 'Login' });
    
    expect(usernameInput).toBeInTheDocument();
    expect(passwordInput).toBeInTheDocument();
    expect(submitButton).toBeInTheDocument();
    
    // Check for register link
    expect(screen.getByText('Register here')).toBeInTheDocument();
  });

  it.skip('handles successful login', async () => {
    const mockUser = {
      id: 1,
      username: 'testuser',
      email: 'test@example.com',
      role: 'User'
    };
    mockLogin.mockResolvedValueOnce(mockUser);
    renderLogin({ login: mockLogin });
    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password123' } });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));
    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith('testuser', 'password123');
    });
  });

  it('handles login failure', async () => {
    const errorMessage = 'Invalid credentials';
    mockLogin.mockRejectedValueOnce(new Error(errorMessage));
    renderLogin({ login: mockLogin });
    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrongpassword' } });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));
    await waitFor(() => {
      expect(screen.getByText('Login failed. Please try again.')).toBeInTheDocument();
    });
  });

  it('validates required fields', async () => {
    renderLogin();
    fireEvent.click(screen.getByRole('button', { name: /login/i }));
    await waitFor(() => {
      expect(screen.getByText('Username is required')).toBeInTheDocument();
      expect(screen.getByText('Password is required')).toBeInTheDocument();
    });
  });
});
