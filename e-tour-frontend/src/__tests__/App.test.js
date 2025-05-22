import React from 'react';
import { render, screen } from '../test-utils';
import App from '../App';

// Create a version of App without the Router for testing
const AppWithoutRouter = () => {
  const { Router, ...rest } = App;
  return <App {...rest} />;
};

describe('App Component', () => {
  beforeEach(() => {
    // Clear all mocks before each test
    jest.clearAllMocks();
  });

  it.skip('renders login button on root route', () => {
    render(<AppWithoutRouter />, { route: '/' });
    expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
  });

  it.skip('renders correct content for authenticated user', () => {
    const mockUser = {
      id: 1,
      username: 'testuser',
      role: 'User'
    };

    render(<AppWithoutRouter />, {
      route: '/user-dashboard',
      initialState: { user: mockUser }
    });

    expect(screen.getByText(/welcome/i)).toBeInTheDocument();
  });
});
