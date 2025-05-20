import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import ApproverDashboard from '../components/ApproverDashboard';

describe('ApproverDashboard', () => {
  test('renders ApproverDashboard component', () => {
    render(
      <MemoryRouter>
        <ApproverDashboard />
      </MemoryRouter>
    );
    const heading = screen.getByText(/approver dashboard/i);
    expect(heading).toBeInTheDocument();
  });
});
