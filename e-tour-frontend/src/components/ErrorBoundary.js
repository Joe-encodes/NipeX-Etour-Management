import React from 'react';
import ErrorDisplay from './ErrorDisplay';

class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }
  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }
  componentDidCatch(error, errorInfo) {
    // Optionally log error to a service
    if (process.env.NODE_ENV !== 'production') {
      console.error(error, errorInfo);
    }
  }
  render() {
    if (this.state.hasError) {
      return <ErrorDisplay message={this.state.error?.message} statusCode={500} />;
    }
    return this.props.children;
  }
}
export default ErrorBoundary; 