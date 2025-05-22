import React from 'react';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext } from './contexts/AuthContext';

// Mock window.location for tests
const mockLocation = {
  href: 'http://localhost',
  origin: 'http://localhost',
  pathname: '/',
  search: '',
  hash: '',
  assign: jest.fn(),
  replace: jest.fn(),
  reload: jest.fn(),
  toString: () => 'http://localhost'
};

Object.defineProperty(window, 'location', {
  value: mockLocation,
  writable: true
});

// Create a wrapper component that provides both router and auth context
const AllTheProviders = ({ children, route = '/', initialState = {} }) => {
  const contextValue = {
    user: initialState.user || null,
    login: jest.fn(),
    logout: jest.fn(),
    register: jest.fn(),
    isAuthenticated: () => !!initialState.user,
    hasRole: (role) => initialState.user?.role === role
  };

  // Update window.location for each test
  window.location.pathname = route;

  return (
    <MemoryRouter initialEntries={[route]}>
      <AuthContext.Provider value={contextValue}>
        {children}
      </AuthContext.Provider>
    </MemoryRouter>
  );
};

// Custom render function that uses our wrapper
const customRender = (ui, { route = '/', initialState = {}, ...renderOptions } = {}) => {
  return render(ui, {
    wrapper: ({ children }) => (
      <AllTheProviders route={route} initialState={initialState}>
        {children}
      </AllTheProviders>
    ),
    ...renderOptions
  });
};

// Re-export everything
export * from '@testing-library/react';

// Override render method
export { customRender as render }; 