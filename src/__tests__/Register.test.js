import { screen, fireEvent, waitFor } from '@testing-library/react';
import axios from 'axios';
import Register from '../components/Register';
import { renderWithProviders } from './testUtils';

jest.mock('axios');

describe('Register', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('handles successful registration', async () => {
    axios.post.mockResolvedValueOnce({ data: { message: 'Registration successful' } });
    
    renderWithProviders(<Register />);
    
    const usernameInput = screen.getByLabelText(/username/i);
    const passwordInput = screen.getByLabelText(/password/i);
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i);
    const emailInput = screen.getByLabelText(/email/i);
    const roleSelect = screen.getByLabelText(/role/i);
    const registerButton = screen.getByRole('button', { name: /register/i });
    
    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.change(confirmPasswordInput, { target: { value: 'password123' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(roleSelect, { target: { value: 'User' } });
    fireEvent.click(registerButton);
    
    await waitFor(() => {
      expect(axios.post).toHaveBeenCalledWith(
        '/api/auth/register',
        {
          username: 'testuser',
          password: 'password123',
          email: 'test@example.com',
          role: 'User'
        }
      );
    });
  });

  test('handles registration failure', async () => {
    const errorMessage = 'Username already exists';
    axios.post.mockRejectedValueOnce({ response: { data: { message: errorMessage } } });
    
    renderWithProviders(<Register />);
    
    const usernameInput = screen.getByLabelText(/username/i);
    const passwordInput = screen.getByLabelText(/password/i);
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i);
    const emailInput = screen.getByLabelText(/email/i);
    const roleSelect = screen.getByLabelText(/role/i);
    const registerButton = screen.getByRole('button', { name: /register/i });
    
    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.change(confirmPasswordInput, { target: { value: 'password123' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(roleSelect, { target: { value: 'User' } });
    fireEvent.click(registerButton);
    
    await waitFor(() => {
      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });
  });
}); 