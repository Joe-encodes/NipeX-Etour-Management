import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext } from '../contexts/AuthContext';

// Mock user for testing
export const mockUser = {
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  role: 'user'
};

// Helper to render components with providers
export function renderWithProviders(ui, { user = mockUser, route = '/', authContextValue } = {}) {
  // Clean up any previous history state
  window.history.replaceState({}, 'Test page', route);

  const mockLogin = jest.fn();
  const mockLogout = jest.fn();
  const mockRegister = jest.fn();

  const contextValue = authContextValue || {
    user,
    login: mockLogin,
    logout: mockLogout,
    register: mockRegister,
    isAuthenticated: !!user
  };

  return render(
    <MemoryRouter initialEntries={[route]}>
      <AuthContext.Provider value={contextValue}>
        {ui}
      </AuthContext.Provider>
    </MemoryRouter>
  );
}

// Helper to mock API responses
export function mockApiResponse(data, status = 200) {
  return Promise.resolve({
    data,
    status,
    statusText: status === 200 ? 'OK' : 'Error'
  });
}

// Helper to mock API errors
export function mockApiError(message = 'API Error', status = 500) {
  return Promise.reject({
    response: {
      data: { message },
      status,
      statusText: 'Error'
    }
  });
}

// Helper to mock localStorage
export function mockLocalStorage() {
  const store = {};
  return {
    getItem: jest.fn(key => store[key]),
    setItem: jest.fn((key, value) => {
      store[key] = value;
    }),
    removeItem: jest.fn(key => {
      delete store[key];
    }),
    clear: jest.fn(() => {
      Object.keys(store).forEach(key => {
        delete store[key];
      });
    })
  };
}

// Helper to wait for state updates
export async function waitForStateUpdate() {
  await new Promise(resolve => setTimeout(resolve, 0));
}

describe('renderWithProviders', () => {
  it('renders component with default providers', () => {
    const TestComponent = () => <div>Test</div>;
    const { getByText } = renderWithProviders(<TestComponent />);
    expect(getByText('Test')).toBeInTheDocument();
  });
});
