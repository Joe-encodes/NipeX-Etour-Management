import { screen, fireEvent, waitFor } from '@testing-library/react';
import axios from 'axios';
import Login from '../components/Login';
import { renderWithProviders } from './testUtils';

jest.mock('axios');

describe('Login', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('handles successful login', async () => {
    axios.post.mockResolvedValueOnce({ data: { token: 'test-token' } });
    
    renderWithProviders(<Login />);
    
    const usernameInput = screen.getByLabelText(/username/i);
    const passwordInput = screen.getByLabelText(/password/i);
    const loginButton = screen.getByRole('button', { name: /login/i });
    
    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(loginButton);
    
    await waitFor(() => {
      expect(axios.post).toHaveBeenCalledWith(
        '/api/auth/login',
        {
          username: 'testuser',
          password: 'password123'
        }
      );
    });
  });

  test('handles login failure', async () => {
    const errorMessage = 'Invalid credentials';
    axios.post.mockRejectedValueOnce({ response: { data: { message: errorMessage } } });
    
    renderWithProviders(<Login />);
    
    const usernameInput = screen.getByLabelText(/username/i);
    const passwordInput = screen.getByLabelText(/password/i);
    const loginButton = screen.getByRole('button', { name: /login/i });
    
    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(passwordInput, { target: { value: 'wrongpassword' } });
    fireEvent.click(loginButton);
    
    await waitFor(() => {
      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });
  });
}); 