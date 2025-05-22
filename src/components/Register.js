import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

const Register = () => {
  const [formData, setFormData] = useState({
    username: '',
    password: '',
    confirmPassword: '',
    email: '',
    role: 'User'
  });
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    try {
      const { confirmPassword, ...registerData } = formData;
      await axios.post('/api/auth/register', {
        username: formData.username,
        password: formData.password,
        email: formData.email,
        role: formData.role
      });
      navigate('/');
    } catch (err) {
      setError(err.response?.data?.message || 'Registration failed. Please try again.');
    }
  };

  return (
    <div className="register-container">
      <h2>Register</h2>
      <form onSubmit={handleSubmit}>
        <div className="form-row">
          <label className="form-label" htmlFor="username">
            Username:
          </label>
          <input
            className="form-input"
            id="username"
            name="username"
            type="text"
            required
            value={formData.username}
            onChange={handleChange}
          />
        </div>
        <div className="form-row">
          <label className="form-label" htmlFor="password">
            Password:
          </label>
          <input
            className="form-input"
            id="password"
            name="password"
            type="password"
            required
            value={formData.password}
            onChange={handleChange}
          />
        </div>
        <div className="form-row">
          <label className="form-label" htmlFor="confirmPassword">
            Confirm Password:
          </label>
          <input
            className="form-input"
            id="confirmPassword"
            name="confirmPassword"
            type="password"
            required
            value={formData.confirmPassword}
            onChange={handleChange}
          />
        </div>
        <div className="form-row">
          <label className="form-label" htmlFor="email">
            Email:
          </label>
          <input
            className="form-input"
            id="email"
            name="email"
            type="email"
            required
            value={formData.email}
            onChange={handleChange}
          />
        </div>
        <div className="form-row">
          <label className="form-label" htmlFor="role">
            Role:
          </label>
          <select
            className="form-input"
            id="role"
            name="role"
            value={formData.role}
            onChange={handleChange}
          >
            <option value="User">User</option>
            <option value="Approver">Approver</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
        <button type="submit">Register</button>
      </form>
      {error && <p className="error-message">{error}</p>}
      <p>
        Already have an account? <a href="/">Login here</a>
      </p>
    </div>
  );
};

export default Register; 