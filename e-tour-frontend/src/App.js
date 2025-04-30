import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Login from './components/Login';
import Register from './components/Register';
import UserDashboard from './components/UserDashboard';
import ApproverDashboard from './components/ApproverDashboard';
import AdminDashboard from './components/AdminDashboard';

function App() {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/user-dashboard" element={<UserDashboard />} />
                <Route path="/approver-dashboard" element={<ApproverDashboard />} />
                <Route path="/admin-dashboard" element={<AdminDashboard />} />
            </Routes>
        </Router>
    );
}

export default App;