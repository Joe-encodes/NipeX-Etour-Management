import { render } from '@testing-library/react';
import { AuthContext } from '../contexts/AuthContext';
import { MemoryRouter } from 'react-router-dom';

export const mockUser = {
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  role: 'User'
};

export const mockDocument = {
  id: 1,
  title: 'Test Document',
  status: 'Signed',
  createdAt: '2024-03-20T00:00:00.000Z',
  updatedAt: '2024-03-20T00:00:00.000Z'
};

export const renderWithProviders = (ui, { user = mockUser, route = '/' } = {}) => {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <AuthContext.Provider value={{ user }}>
        {ui}
      </AuthContext.Provider>
    </MemoryRouter>
  );
}; 