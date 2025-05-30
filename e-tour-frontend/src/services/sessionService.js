import config from '../config';

class SessionService {
    constructor() {
        this.timeoutId = null;
        this.warningId = null;
        this.lastActivity = Date.now();
        this.setupActivityListeners();
    }

    setupActivityListeners() {
        // Reset timer on user activity
        const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'];
        events.forEach(event => {
            document.addEventListener(event, () => this.resetTimer());
        });
    }

    startSession() {
        this.resetTimer();
    }

    resetTimer() {
        this.lastActivity = Date.now();
        this.clearTimers();
        this.setupTimers();
    }

    setupTimers() {
        // Set warning timer
        this.warningId = setTimeout(() => {
            this.showWarning();
        }, config.session.timeout - config.session.warningTime);

        // Set timeout timer
        this.timeoutId = setTimeout(() => {
            this.handleTimeout();
        }, config.session.timeout);
    }

    clearTimers() {
        if (this.warningId) {
            clearTimeout(this.warningId);
        }
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
        }
    }

    showWarning() {
        // Create warning modal
        const warningModal = document.createElement('div');
        warningModal.className = 'session-warning-modal';
        warningModal.innerHTML = `
            <div class="session-warning-content">
                <h3>Session Timeout Warning</h3>
                <p>Your session will expire in ${config.session.warningTime / 60000} minutes due to inactivity.</p>
                <p>Would you like to continue your session?</p>
                <div class="session-warning-buttons">
                    <button id="extend-session">Extend Session</button>
                    <button id="logout-now">Logout Now</button>
                </div>
            </div>
        `;

        document.body.appendChild(warningModal);

        // Add event listeners
        document.getElementById('extend-session').addEventListener('click', () => {
            this.resetTimer();
            document.body.removeChild(warningModal);
        });

        document.getElementById('logout-now').addEventListener('click', () => {
            this.handleTimeout();
        });
    }

    handleTimeout() {
        this.clearTimers();
        localStorage.removeItem(config.auth.tokenKey);
        localStorage.removeItem(config.auth.refreshTokenKey);
        localStorage.removeItem(config.auth.tokenExpiryKey);
        window.location.href = '/login';
    }

    // Clean up when component unmounts
    cleanup() {
        this.clearTimers();
        const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'];
        events.forEach(event => {
            document.removeEventListener(event, () => this.resetTimer());
        });
    }
}

// Create a singleton instance
const sessionService = new SessionService();
export default sessionService; 