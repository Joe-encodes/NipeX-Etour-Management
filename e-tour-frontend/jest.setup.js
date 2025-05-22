// Mock window.location for React Router
const mockLocation = new URL('http://localhost/');
mockLocation.assign = jest.fn();
mockLocation.replace = jest.fn();
mockLocation.reload = jest.fn();

// Ensure window.location is properly mocked before any tests run
beforeAll(() => {
  delete window.location;
  window.location = mockLocation;
});

// Clean up after all tests
afterAll(() => {
  window.location = location;
}); 