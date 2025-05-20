import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import UserDashboard from '../components/UserDashboard';

describe('UserDashboard', () => {
  test('renders UserDashboard component', () => {
    render(
      <MemoryRouter>
        <UserDashboard />
      </MemoryRouter>
    );
    const heading = screen.getByText(/user dashboard/i);
    expect(heading).toBeInTheDocument();
  });
});
