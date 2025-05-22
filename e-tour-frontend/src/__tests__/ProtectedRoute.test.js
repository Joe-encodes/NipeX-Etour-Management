import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProtectedRoute from '../components/ProtectedRoute';
import { AuthContext } from '../contexts/AuthContext';

// Test components
const ProtectedContent = () => <div>Protected Content</div>;
const LoginPage = () => <div>Login Page</div>;
const UnauthorizedPage = () => <div>Unauthorized Access</div>;

describe('ProtectedRoute', () => {
  const renderProtectedRoute = (authValue, initialRoute = '/protected') => {
    return render(
      <MemoryRouter initialEntries={[initialRoute]}>
        <AuthContext.Provider value={authValue}>
          <Routes>
            <Route
              path="/protected"
              element={
                <ProtectedRoute>
                  <ProtectedContent />
                </ProtectedRoute>
              }
            />
            <Route path="/" element={<LoginPage />} />
            <Route path="/unauthorized" element={<UnauthorizedPage />} />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>
    );
  };

  test('shows loading state', () => {
    renderProtectedRoute({ loading: true });
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  test('redirects to login when not authenticated', () => {
    renderProtectedRoute({
      loading: false,
      isAuthenticated: () => false
    });

    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  test('renders protected content when authenticated', () => {
    renderProtectedRoute({
      loading: false,
      isAuthenticated: () => true
    });

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  test('redirects to unauthorized page when role is not allowed', () => {
    renderProtectedRoute({
      loading: false,
      isAuthenticated: () => true,
      hasRole: (role) => role === 'User'
    }, '/admin-protected');

    render(
      <MemoryRouter initialEntries={['/admin-protected']}>
        <AuthContext.Provider
          value={{
            loading: false,
            isAuthenticated: () => true,
            hasRole: (role) => role === 'User'
          }}
        >
          <Routes>
            <Route
              path="/admin-protected"
              element={
                <ProtectedRoute requiredRole="Admin">
                  <ProtectedContent />
                </ProtectedRoute>
              }
            />
            <Route path="/unauthorized" element={<UnauthorizedPage />} />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>
    );

    expect(screen.getByText('Unauthorized Access')).toBeInTheDocument();
  });

  test('allows access when role is correct', () => {
    render(
      <MemoryRouter initialEntries={['/admin-protected']}>
        <AuthContext.Provider
          value={{
            loading: false,
            isAuthenticated: () => true,
            hasRole: (role) => role === 'Admin'
          }}
        >
          <Routes>
            <Route
              path="/admin-protected"
              element={
                <ProtectedRoute requiredRole="Admin">
                  <ProtectedContent />
                </ProtectedRoute>
              }
            />
            <Route path="/unauthorized" element={<UnauthorizedPage />} />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  test('preserves location state when redirecting to login', () => {
    const { container } = renderProtectedRoute({
      loading: false,
      isAuthenticated: () => false
    });

    expect(container.innerHTML).toContain('Login Page');
    // Note: We can't directly test the location state as it's internal to react-router
    // But we can verify the redirect happened
  });
}); 