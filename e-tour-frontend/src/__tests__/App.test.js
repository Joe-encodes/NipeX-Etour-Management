import React from 'react';
import { render, screen } from '@testing-library/react';
import App from '../App';

test('renders login link', () => {
  render(<App />);
  const loginLink = screen.getByText(/login/i);
  expect(loginLink).toBeInTheDocument();
});
