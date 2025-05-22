import React from 'react';
import { render, screen } from '@testing-library/react';
import App from '../App';

beforeAll(() => {
  delete window.location;
  window.location = { href: 'http://localhost/', origin: 'http://localhost' };
});

describe('App (Isolated)', () => {
  test('renders login button', () => {
    render(<App />);
    const loginButton = screen.getByRole('button', { name: /login/i });
    expect(loginButton).toBeInTheDocument();
  });
}); 