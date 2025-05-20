// Placeholder for main JavaScript logic
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM fully loaded and parsed');

    // 首先填充筛选选项
    if (typeof populateFilterOptions === 'function') populateFilterOptions();

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
            // 确保先填充筛选选项再加载任务
            if (typeof loadTasks === 'function') loadTasks();
            
            // 添加搜索按钮事件监听
            const searchButton = document.getElementById('search-button');
            if (searchButton) {
                searchButton.addEventListener('click', () => {
                    const searchInput = document.getElementById('search-input');
                    if (searchInput && searchInput.value) {
                        currentSearchParams.searchText = searchInput.value;
                        loadTasks();
                    }
                });
            }
            
            // 添加应用筛选按钮事件监听
            const applyFiltersButton = document.getElementById('apply-filters');
            if (applyFiltersButton) {
                applyFiltersButton.addEventListener('click', () => {
                    // 重新填充筛选选项以确保数据最新
                    if (typeof populateFilterOptions === 'function') populateFilterOptions();
                    
                    const statusFilter = document.getElementById('filter-status');
                    const priorityFilter = document.getElementById('filter-priority');
                    const sortBy = document.getElementById('sort-by');
                    const sortDirection = document.getElementById('sort-direction');
                    
                    if (statusFilter) currentSearchParams.status = statusFilter.value;
                    if (priorityFilter) currentSearchParams.priority = priorityFilter.value;
                    if (sortBy) currentSearchParams.sortBy = sortBy.value;
                    if (sortDirection) currentSearchParams.ascending = sortDirection.value === 'true';
                    
                    loadTasks();
                });
            }
            
            // 添加重置按钮事件监听
            const resetFiltersButton = document.getElementById('reset-filters');
            if (resetFiltersButton) {
                resetFiltersButton.addEventListener('click', () => {
                    currentSearchParams = {
                        searchText: '',
                        status: '',
                        priority: '',
                        sortBy: '',
                        ascending: false,
                        page: 1,
                        pageSize: 10
                    };
                    
                    const searchInput = document.getElementById('search-input');
                    if (searchInput) searchInput.value = '';
                    
                    const statusFilter = document.getElementById('filter-status');
                    if (statusFilter) statusFilter.value = '';
                    
                    const priorityFilter = document.getElementById('filter-priority');
                    if (priorityFilter) priorityFilter.value = '';
                    
                    const sortBy = document.getElementById('sort-by');
                    if (sortBy) sortBy.value = '';
                    
                    const sortDirection = document.getElementById('sort-direction');
                    if (sortDirection) sortDirection.value = 'true';
                    
                    loadTasks();
                });
            }
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
