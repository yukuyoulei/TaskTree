// Placeholder for main JavaScript logic
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM fully loaded and parsed');

    // Initialize UI elements and event listeners
    if (typeof initializeUI === 'function') {
        initializeUI();
    }

    // Check login status on page load
    if (typeof checkLoginStatus === 'function') {
        checkLoginStatus();
    }

    // Load specific page data if applicable
    const page = document.body.id || window.location.pathname.split('/').pop().split('.')[0];
    switch (page) {
        case 'task-list':
            if (typeof loadTasks === 'function') loadTasks();
            if (typeof populateFilterOptions === 'function') populateFilterOptions();
            break;
        case 'task-detail':
            if (typeof loadTaskDetails === 'function') loadTaskDetails();
            break;
        case 'user-management':
            if (typeof loadUsers === 'function') loadUsers();
            break;
        default:
            // For index.html or other pages
            break;
    }
});

function checkLoginStatus() {
    const token = localStorage.getItem('authToken');
    const userInfo = document.getElementById('user-info');
    const logoutButton = document.getElementById('logout-button');
    const loginSection = document.getElementById('login-section');
    const welcomeSection = document.getElementById('welcome-section');
    const welcomeUsername = document.getElementById('welcome-username');

    if (token) {
        // Ideally, you would verify the token with the backend
        // For now, assume token presence means logged in
        const username = localStorage.getItem('username') || 'User'; // Get username stored at login
        if (userInfo) userInfo.textContent = `Logged in as: ${username}`;
        if (logoutButton) logoutButton.style.display = 'inline';
        if (loginSection) loginSection.style.display = 'none';
        if (welcomeSection) welcomeSection.style.display = 'block';
        if (welcomeUsername) welcomeUsername.textContent = username;

        // Show user-specific elements in nav for all pages
        document.querySelectorAll('nav #user-info').forEach(el => el.textContent = `欢迎, ${username}`);
        document.querySelectorAll('nav #logout-button').forEach(el => el.style.display = 'inline');

    } else {
        if (userInfo) userInfo.textContent = '';
        if (logoutButton) logoutButton.style.display = 'none';
        // For index page, show login form if not logged in
        if (loginSection && window.location.pathname.endsWith('index.html') || window.location.pathname === '/') {
            loginSection.style.display = 'block';
        }
        if (welcomeSection) welcomeSection.style.display = 'none';

        // Hide user-specific elements in nav for all pages
        document.querySelectorAll('nav #user-info').forEach(el => el.textContent = '');
        document.querySelectorAll('nav #logout-button').forEach(el => el.style.display = 'none');

        // If not on login page and not logged in, redirect to login page
        const nonAuthPages = ['index.html', '']; // Allow access to index.html (login page)
        const currentPage = window.location.pathname.split('/').pop();
        if (!nonAuthPages.includes(currentPage) && !token) {
            // window.location.href = 'index.html';
            console.warn('Not logged in, access to this page might be restricted by backend.');
        }
    }
}

// Placeholder for logout function
function logout() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('username');
    localStorage.removeItem('userRole');
    // Redirect to login page or update UI
    checkLoginStatus(); // Update UI elements
    if (window.location.pathname.split('/').pop() !== 'index.html') {
        window.location.href = 'index.html';
    }
}

// Add event listener for logout button if it exists
document.addEventListener('DOMContentLoaded', () => {
    const logoutButton = document.getElementById('logout-button');
    if (logoutButton) {
        logoutButton.addEventListener('click', logout);
    }
});
