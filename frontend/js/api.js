// Placeholder for API interaction logic
const API_BASE_URL = "http://localhost:5038/api";

async function request(endpoint, method = "GET", data = null, token = null) {
    const config = {
        method: method,
        headers: {
            "Content-Type": "application/json",
        },
    };

    if (token) {
        config.headers["Authorization"] = `Bearer ${token}`;
    }

    if (data && (method === "POST" || method === "PUT")) {
        config.body = JSON.stringify(data);
    }

    let url = `${API_BASE_URL}${endpoint}`;
    if (data && method === "GET" && Object.keys(data).length > 0) {
        url += `?${new URLSearchParams(data).toString()}`;
    }

    try {
        const response = await fetch(url, config);
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: response.statusText }));
            console.error("API Error:", response.status, errorData);
            throw new Error(errorData.message || `Request failed with status ${response.status}`);
        }
        if (response.status === 204) { // No Content
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error("Fetch API Error:", error);
        if (typeof showError === "function") {
            showError(error.message || "An unexpected error occurred.");
        }
        throw error;
    }
}

// --- Auth API Calls ---
async function loginUser(username, password) {
    return request("/auth/login", "POST", { username, password });
}

// For initial admin registration (if no users exist)
async function registerInitialAdmin(userData) {
    return request("/auth/register", "POST", userData);
}

// For admin to register other users
async function registerUserByAdmin(userData) {
    const token = localStorage.getItem("authToken");
    return request("/users/register-by-admin", "POST", userData, token);
}


// --- User Management API Calls (Admin) ---
async function getUsers(params = {}) {
    const token = localStorage.getItem("authToken");
    return request("/users", "GET", params, token);
}

async function getUserDetails(userId) {
    const token = localStorage.getItem("authToken");
    return request(`/users/${userId}`, "GET", null, token);
}

async function updateUser(userId, userData) {
    const token = localStorage.getItem("authToken");
    return request(`/users/${userId}`, "PUT", userData, token);
}

async function deleteUser(userId) {
    const token = localStorage.getItem("authToken");
    return request(`/users/${userId}`, "DELETE", null, token);
}

// --- Task API Calls ---
async function createTask(taskData) {
    const token = localStorage.getItem("authToken");
    return request("/tasks", "POST", taskData, token);
}

async function getTasks(params = {}) { 
    const token = localStorage.getItem("authToken");
    return request("/tasks", "GET", params, token);
}

async function getTaskDetails(taskId) {
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}`, "GET", null, token);
}

async function updateTask(taskId, taskData) {
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}`, "PUT", taskData, token);
}

async function deleteTask(taskId) {
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}`, "DELETE", null, token);
}

// --- Task Relationship API Calls ---
// addTaskRelationship(currentTaskId, { relatedTaskId: relatedTaskIdValue, relationshipType: typeForAPIFromFrontend });
async function addTaskRelationship(taskId, relationshipData) { // relationshipData = { relatedTaskId, relationshipType }
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}/relationships`, "POST", relationshipData, token);
}

async function getTaskRelationships(taskId) {
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}/relationships`, "GET", null, token);
}

async function getTaskTree(taskId) {
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}/relationships/tree`, "GET", null, token); // Corrected endpoint for tree
}

// Corrected deleteTaskRelationship to include taskId in the path
async function deleteTaskRelationship(taskId, relationshipId) {
    const token = localStorage.getItem("authToken");
    return request(`/tasks/${taskId}/relationships/${relationshipId}`, "DELETE", null, token);
}

// --- Metadata API Calls ---
async function getTaskStatuses() {
    const token = localStorage.getItem("authToken");
    return request("/metadata/task-statuses", "GET", null, token);
}

async function getTaskPriorities() {
    const token = localStorage.getItem("authToken");
    return request("/metadata/task-priorities", "GET", null, token);
}

async function getCombinedMetadata() {
    const token = localStorage.getItem("authToken");
    return request("/metadata", "GET", null, token);
}

async function getAllUsersForAssigneeSelection() {
    const token = localStorage.getItem("authToken");
    return request("/users", "GET", { page: 1, pageSize: 1000 }, token); 
}

