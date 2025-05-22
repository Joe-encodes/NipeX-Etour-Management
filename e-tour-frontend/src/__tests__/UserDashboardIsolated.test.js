import React from 'react';
import { screen, waitFor } from '@testing-library/react';
import axios from 'axios';
import UserDashboard from '../components/UserDashboard';
import { renderWithProviders } from './testUtils';

jest.mock('axios');

describe('UserDashboard (Isolated)', () => {
  const mockUser = {
    id: 1,
    username: 'testuser',
    email: 'test@example.com',
    role: 'User'
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('handles document fetch error', async () => {
    // Reset the default mock so our error mock is used
    axios.get.mockReset();

    const errorMessage = 'Failed to fetch documents';
    const mockError = {
      response: {
        data: {
          message: errorMessage
        }
      }
    };

    // Mock axios.get to reject with error
    axios.get.mockRejectedValueOnce(mockError);

    renderWithProviders(<UserDashboard />, {
      user: mockUser,
      route: '/user-dashboard',
      authContextValue: {
        user: mockUser,
        isAuthenticated: true
      }
    });

    const errorElement = await waitFor(
      () => screen.getByText(`Failed to fetch documents: ${errorMessage}`),
      { timeout: 10000 }
    );

    expect(errorElement).toBeInTheDocument();
  });
}); 