// Placeholder for UI manipulation logic

function initializeUI() {
    // Login form submission
    const loginForm = document.getElementById("login-form");
    if (loginForm) {
        loginForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const username = event.target.username.value;
            const password = event.target.password.value;
            try {
                const data = await loginUser(username, password);
                if (data.token) {
                    localStorage.setItem("authToken", data.token);
                    localStorage.setItem("username", data.user.username); // Store username
                    localStorage.setItem("userRole", data.user.role); // Store user role
                    checkLoginStatus(); // Update UI
                    // Redirect to task list or dashboard after login
                    if (window.location.pathname.endsWith("index.html") || window.location.pathname === "/") {
                         window.location.href = "task-list.html";
                    }
                } else {
                    showError("Login failed: No token received.");
                }
            } catch (error) {
                showError(error.message || "Login failed.");
            }
        });
    }

    // Create Task Button on task-list.html
    const createTaskButton = document.getElementById("create-task-button");
    if (createTaskButton) {
        createTaskButton.addEventListener("click", () => openTaskModal());
    }

    // Task Form submission (for create/edit)
    const taskForm = document.getElementById("task-form");
    if (taskForm) {
        taskForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const formData = new FormData(event.target);
            const taskId = formData.get("taskId");
            const assigneeSelect = document.getElementById("task-assignees") || document.getElementById("assignee-select");
            const taskData = {
                title: formData.get("title"),
                content: formData.get("content"),
                assigneeIds: assigneeSelect ? Array.from(assigneeSelect.selectedOptions).map(opt => opt.value) : [],
                status: formData.get("status"),
                priority: formData.get("priority"),
                dueDate: formData.get("dueDate") ? new Date(formData.get("dueDate")).toISOString() : null,
            };

            try {
                if (taskId) {
                    await updateTask(taskId, taskData);
                    //showSuccess("Task updated successfully!");
                } else {
                    await createTask(taskData);
                    //showSuccess("Task created successfully!");
                }
                closeTaskModal();
                loadTasks(); // Refresh task list
            } catch (error) {
                showError(error.message || "Failed to save task.");
            }
        });
    }

    // Modal close buttons
    document.querySelectorAll(".modal .close-button").forEach(button => {
        button.addEventListener("click", () => {
            button.closest(".modal").style.display = "none";
        });
    });

    // User Management: Register User Button
    const registerUserButton = document.getElementById("register-user-button");
    if (registerUserButton) {
        registerUserButton.addEventListener("click", () => openUserModal());
    }

    // User Form submission (for create/edit by admin)
    const userForm = document.getElementById("user-form");
    if (userForm) {
        userForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const formData = new FormData(event.target);
            const userId = formData.get("userId");
            const userData = {
                username: formData.get("username"),
                password: formData.get("password"), // Password should only be sent for new users or if being changed
                email: formData.get("email"),
                realName: formData.get("realName"),
                role: formData.get("role"),
            };
            // For updates, password might not be sent or handled differently
            if (userId && !userData.password) delete userData.password; 

            try {
                if (userId) {
                    await updateUser(userId, userData);
                    showSuccess("User updated successfully!");
                } else {
                    await registerUserByAdmin(userData); // Uses admin registration endpoint
                    showSuccess("User registered successfully!");
                }
                closeUserModal();
                loadUsers(); // Refresh user list
            } catch (error) {
                showError(error.message || "Failed to save user.");
            }
        });
    }
    
    // Task Detail Page Buttons
    const editTaskBtn = document.getElementById("edit-task-button");
    if(editTaskBtn) editTaskBtn.addEventListener("click", () => {
        const taskId = document.getElementById("detail-task-id").textContent;
        if(taskId) openTaskModal(taskId); // Reuse task modal for editing
    });

    const deleteTaskBtn = document.getElementById("delete-task-button");
    if(deleteTaskBtn) deleteTaskBtn.addEventListener("click", async () => {
        const taskId = document.getElementById("detail-task-id").textContent;
        if(taskId && confirm("Are you sure you want to delete this task?")){
            try {
                await deleteTask(taskId);
                showSuccess("Task deleted successfully!");
                window.location.href = "task-list.html";
            } catch (error) {
                showError(error.message || "Failed to delete task.");
            }
        }
    });

    const manageRelationsBtn = document.getElementById("manage-relations-button");
    if(manageRelationsBtn) manageRelationsBtn.addEventListener("click", () => {
        const taskId = document.getElementById("detail-task-id").textContent;
        openRelationsModal(taskId);
    });

    const relationsForm = document.getElementById("relations-form");
    if(relationsForm) relationsForm.addEventListener("submit", handleRelationsFormSubmit);
}
// --- Task List Page Functions ---
// 全局变量，用于存储当前的搜索和筛选条件
let currentSearchParams = {
    searchText: '',
    status: '',
    priority: '',
    sortBy: '',
    ascending: false,
    page: 1,
    pageSize: 10
};

// 加载任务列表
async function loadTasks(page = 1, pageSize = 10) {
    try {
        // 使用当前的搜索参数
        const params = {
            ...currentSearchParams,
            page: page,
            pageSize: pageSize
        };
        
        const response = await getTasks(params);
        const taskListBody = document.getElementById('task-list-body');
        if (!taskListBody) return;
        
        taskListBody.innerHTML = '';
        
        if (response.data.length === 0) {
            const row = document.createElement('tr');
            row.innerHTML = `<td colspan="6" style="text-align: center;">没有找到任务</td>`;
            taskListBody.appendChild(row);
            return;
        }
        
        response.data.forEach(task => {
            const row = document.createElement('tr');
            
            // 格式化截止日期
            let dueDateDisplay = '无截止日期';
            if (task.dueDate) {
                const dueDate = new Date(task.dueDate);
                dueDateDisplay = dueDate.toLocaleDateString();
            }
            
            // 获取负责人名称列表
            const assigneeNames = task.assignees ? task.assignees.map(a => a.username).join(', ') : '无负责人';
            
            row.innerHTML = `
                <td><a href="task-detail.html?id=${task.id}">${task.title}</a></td>
                <td>${assigneeNames}</td>
                <td>${task.status}</td>
                <td>${task.priority}</td>
                <td>${dueDateDisplay}</td>
                <td>
                    <button class="edit-task" data-id="${task.id}">编辑</button>
                    <button class="delete-task" data-id="${task.id}">删除</button>
                </td>
            `;
            
            taskListBody.appendChild(row);
        });
        
        // 添加编辑和删除按钮的事件监听器
        document.querySelectorAll('.edit-task').forEach(button => {
            button.addEventListener('click', function() {
                const taskId = this.getAttribute('data-id');
                openTaskModal(taskId);
            });
        });
        
        document.querySelectorAll('.delete-task').forEach(button => {
            button.addEventListener('click', function() {
                const taskId = this.getAttribute('data-id');
                confirmDeleteTask(taskId);
            });
        });
        
        // 更新分页控件
        updatePagination(response.totalCount, page, pageSize);
        
    } catch (error) {
        console.error('加载任务失败:', error);
        showError('加载任务失败，请检查网络连接或刷新页面重试。');
    }
}

// 更新分页控件
function updatePagination(totalCount, currentPage, pageSize) {
    const paginationDiv = document.getElementById('pagination');
    if (!paginationDiv) return;
    
    const totalPages = Math.ceil(totalCount / pageSize);
    paginationDiv.innerHTML = '';
    
    if (totalPages <= 1) return;
    
    // 添加上一页按钮
    const prevButton = document.createElement('button');
    prevButton.textContent = '上一页';
    prevButton.disabled = currentPage === 1;
    prevButton.addEventListener('click', () => loadTasks(currentPage - 1, pageSize));
    paginationDiv.appendChild(prevButton);
    
    // 添加页码按钮
    const maxPageButtons = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxPageButtons / 2));
    let endPage = Math.min(totalPages, startPage + maxPageButtons - 1);
    
    if (endPage - startPage + 1 < maxPageButtons) {
        startPage = Math.max(1, endPage - maxPageButtons + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
        const pageButton = document.createElement('button');
        pageButton.textContent = i;
        pageButton.classList.toggle('active', i === currentPage);
        pageButton.addEventListener('click', () => loadTasks(i, pageSize));
        paginationDiv.appendChild(pageButton);
    }
    
    // 添加下一页按钮
    const nextButton = document.createElement('button');
    nextButton.textContent = '下一页';
    nextButton.disabled = currentPage === totalPages;
    nextButton.addEventListener('click', () => loadTasks(currentPage + 1, pageSize));
    paginationDiv.appendChild(nextButton);
}

// 初始化搜索和筛选功能
function initializeSearchAndFilters() {
    // 填充筛选选项
    populateFilterOptions();
    
    // 搜索按钮点击事件
    const searchButton = document.getElementById('search-button');
    if (searchButton) {
        searchButton.addEventListener('click', applySearchAndFilters);
    }
    
    // 搜索框回车事件
    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                applySearchAndFilters();
            }
        });
    }
    
    // 应用筛选按钮点击事件
    const applyFiltersButton = document.getElementById('apply-filters');
    if (applyFiltersButton) {
        applyFiltersButton.addEventListener('click', applySearchAndFilters);
    }
    
    // 重置筛选按钮点击事件
    const resetFiltersButton = document.getElementById('reset-filters');
    if (resetFiltersButton) {
        resetFiltersButton.addEventListener('click', resetSearchAndFilters);
    }
}

// 应用搜索和筛选条件
function applySearchAndFilters() {
    const searchInput = document.getElementById('search-input');
    const statusFilter = document.getElementById('filter-status');
    const priorityFilter = document.getElementById('filter-priority');
    const sortBySelect = document.getElementById('sort-by');
    const sortDirectionSelect = document.getElementById('sort-direction');
    
    // 更新全局搜索参数
    currentSearchParams = {
        searchText: searchInput ? searchInput.value : '',
        status: statusFilter ? statusFilter.value : '',
        priority: priorityFilter ? priorityFilter.value : '',
        sortBy: sortBySelect ? sortBySelect.value : '',
        ascending: sortDirectionSelect ? sortDirectionSelect.value === 'true' : false,
        page: 1, // 重置到第一页
        pageSize: currentSearchParams.pageSize
    };
    
    // 重新加载任务列表
    loadTasks(1, currentSearchParams.pageSize);
}

// 重置搜索和筛选条件
function resetSearchAndFilters() {
    // 重置输入框和下拉选择框
    const searchInput = document.getElementById('search-input');
    if (searchInput) searchInput.value = '';
    
    const statusFilter = document.getElementById('filter-status');
    if (statusFilter) statusFilter.value = '';
    
    const priorityFilter = document.getElementById('filter-priority');
    if (priorityFilter) priorityFilter.value = '';
    
    const sortBySelect = document.getElementById('sort-by');
    if (sortBySelect) sortBySelect.value = '';
    
    const sortDirectionSelect = document.getElementById('sort-direction');
    if (sortDirectionSelect) sortDirectionSelect.value = 'false';
    
    // 重置全局搜索参数
    currentSearchParams = {
        searchText: '',
        status: '',
        priority: '',
        sortBy: '',
        ascending: false,
        page: 1,
        pageSize: 10
    };
    
    // 重新加载任务列表
    loadTasks(1, 10);
}

async function populateFilterOptions() {
    // Populate status and priority dropdowns in task modal and filter sections
    const taskStatusSelect = document.getElementById("task-status");
    const filterStatusSelect = document.getElementById("filter-status");
    const taskPrioritySelect = document.getElementById("task-priority");
    const filterPrioritySelect = document.getElementById("filter-priority");

    try {
        const statuses = await getTaskStatuses() || ["待办", "进行中", "已完成", "已取消"];
        const priorities = await getTaskPriorities() || ["高", "中", "低"];

        // 填充任务表单的状态下拉
        if (taskStatusSelect && taskStatusSelect.options.length <= 1) {
            taskStatusSelect.innerHTML = '';
            statuses.forEach(status => {
                const option = document.createElement("option");
                option.value = status;
                option.textContent = status;
                taskStatusSelect.options.add(option);
            });
        }
        
        // 填充筛选区域的状态下拉
        if (filterStatusSelect && filterStatusSelect.options.length <= 1) {
            filterStatusSelect.innerHTML = '';
            statuses.forEach(status => {
                const option = document.createElement("option");
                option.value = status;
                option.textContent = status;
                filterStatusSelect.options.add(option);
            });
        }

        // 填充任务表单的优先级下拉
        if (taskPrioritySelect && taskPrioritySelect.options.length <= 1) {
            taskPrioritySelect.innerHTML = '';
            priorities.forEach(priority => {
                const option = document.createElement("option");
                option.value = priority;
                option.textContent = priority;
                taskPrioritySelect.options.add(option);
            });
        }
        
        // 填充筛选区域的优先级下拉
        if (filterPrioritySelect && filterPrioritySelect.options.length <= 1) {
            filterPrioritySelect.innerHTML = '';
            priorities.forEach(priority => {
                const option = document.createElement("option");
                option.value = priority;
                option.textContent = priority;
                filterPrioritySelect.options.add(option);
            });
        }
        
        // Populate assignees dropdown
        await populateAssigneesDropdown();

    } catch (error) {
        console.error("Failed to load filter options:", error);
    }
}

async function populateAssigneesDropdown(selectedAssigneeIds = []) {
    // 查找负责人下拉列表，支持两种可能的ID
    const assigneesSelect = document.getElementById("task-assignees") || document.getElementById("assignee-select");
    if (!assigneesSelect) {
        console.error("无法找到负责人下拉列表元素");
        return;
    }
    assigneesSelect.innerHTML = "";
    try {
        const usersResponse = await getAllUsersForAssigneeSelection(); 
        const users = usersResponse.users || usersResponse; // Adapt based on actual API response structure
        if (users && users.length > 0) {
            users.forEach(user => {
                const option = document.createElement("option");
                option.value = user.userId;
                option.textContent = user.username;
                if (selectedAssigneeIds.includes(user.userId)) {
                    option.selected = true;
                }
                assigneesSelect.appendChild(option);
            });
        }
    } catch (error) {
        console.error("Failed to load users for assignee selection:", error);
    }
}

function confirmDeleteTask(taskId) {
    if (confirm("Are you sure you want to delete this task?")) {
        deleteTask(taskId)
            .then(() => {
                showSuccess("Task deleted successfully!");
                loadTasks(); // Refresh list
            })
            .catch(error => showError(error.message || "Failed to delete task."));
    }
}

// --- Task Modal Functions ---
async function openTaskModal(taskId = null) {
    const modal = document.getElementById("task-modal");
    const modalTitle = document.getElementById("modal-title");
    const taskForm = document.getElementById("task-form");
    const taskIdField = document.getElementById("task-id");
    
    // 设置模态框标题
    modalTitle.textContent = taskId ? "编辑任务" : "创建任务";
    
    // 只有在创建新任务时才重置表单
    if (!taskId) {
        taskForm.reset();
        
        // 如果有富文本编辑器，清空内容
        if (typeof Quill !== 'undefined') {
            const quill = Quill.find(document.querySelector('#editor-container'));
            if (quill) {
                quill.root.innerHTML = '';
            }
        }
    }
    
    // 清空下拉选项，准备重新填充
    const taskStatusSelect = document.getElementById("task-status");
    const taskPrioritySelect = document.getElementById("task-priority");
    if (taskStatusSelect) taskStatusSelect.innerHTML = "";
    if (taskPrioritySelect) taskPrioritySelect.innerHTML = "";
    
    // 填充状态和优先级下拉列表
    await populateFilterOptions();
    
    // If editing, load task data
    if (taskId) {
        taskIdField.value = taskId;
        try {
            const task = await getTaskDetails(taskId); // Changed from getTaskById
            document.getElementById("task-title").value = task.title;
            
            // 设置富文本编辑器内容
            if (typeof Quill !== 'undefined') {
                const quill = Quill.find(document.querySelector('#editor-container'));
                if (quill) {
                    quill.root.innerHTML = task.content || '';
                }
            }
            document.getElementById("task-content").value = task.content || '';
            
            // Set selected status
            const statusSelect = document.getElementById("task-status");
            if (statusSelect && task.status) {
                for (let i = 0; i < statusSelect.options.length; i++) {
                    if (statusSelect.options[i].value === task.status) {
                        statusSelect.options[i].selected = true;
                        break;
                    }
                }
            }
            
            // Set selected priority
            const prioritySelect = document.getElementById("task-priority");
            if (prioritySelect && task.priority) {
                for (let i = 0; i < prioritySelect.options.length; i++) {
                    if (prioritySelect.options[i].value === task.priority) {
                        prioritySelect.options[i].selected = true;
                        break;
                    }
                }
            }
            
            // Set due date if exists
            if (task.dueDate) {
                // Convert ISO date to local datetime-local format
                const dueDate = new Date(task.dueDate);
                const localDueDate = new Date(dueDate.getTime() - dueDate.getTimezoneOffset() * 60000)
                    .toISOString()
                    .slice(0, 10); // Format: YYYY-MM-DD
                document.getElementById("task-due-date").value = localDueDate;
            }
            
            // 获取任务负责人ID列表
            const assigneeIds = task.assignees ? task.assignees.map(a => a.userId) : [];
            // 先填充负责人下拉列表，并传入当前任务的负责人ID列表以便选中
            await populateAssigneesDropdown(assigneeIds);
            
        } catch (error) {
            showError("Failed to load task details: " + error.message);
        }
    } else {
        // 对于新任务，填充空的负责人下拉列表
        await populateAssigneesDropdown([]);
    }
    
    // Show modal
    modal.style.display = "block";
}

function closeTaskModal() {
    const modal = document.getElementById("task-modal");
    modal.style.display = "none";
}

// --- User Management Page Functions ---
async function loadUsers() {
    const userListBody = document.getElementById("user-list-body");
    if (!userListBody) return;
    userListBody.innerHTML = "<tr><td colspan=\"6\">Loading...</td></tr>";
    try {
        const response = await getUsers(); // Assuming getUsers returns an object like { users: [...] }
        const users = response.users || response; // Adapt if API returns array directly
        if (users && users.length > 0) {
            userListBody.innerHTML = "";
            users.forEach(user => {
                const row = userListBody.insertRow();
                row.innerHTML = `
                    <td>${user.username}</td>
                    <td>${user.email || "N/A"}</td>
                    <td>${user.realName || "N/A"}</td>
                    <td>${user.role}</td>
                    <td>${new Date(user.createdAt).toLocaleDateString()}</td>
                    <td>
                        <button onclick="openUserModal(\'${user.userId}\')">Edit</button>
                        <button onclick="confirmDeleteUser(\'${user.userId}\')">Delete</button>
                    </td>
                `;
            });
        } else {
            userListBody.innerHTML = "<tr><td colspan=\"6\">No users found.</td></tr>";
        }
    } catch (error) {
        userListBody.innerHTML = `<tr><td colspan=\"6\">Error loading users: ${error.message}</td></tr>`;
    }
}

async function openUserModal(userId = null) {
    const modal = document.getElementById("user-modal");
    const modalTitle = document.getElementById("user-modal-title");
    const userForm = document.getElementById("user-form");
    userForm.reset();
    document.getElementById("user-id").value = "";
    const passwordField = document.getElementById("reg-password");

    if (userId) {
        modalTitle.textContent = "Edit User";
        passwordField.required = false; // Password not required for edit unless changing
        passwordField.placeholder = "Leave blank to keep current password";
        try {
            const user = await getUserDetails(userId);
            document.getElementById("user-id").value = user.userId;
            document.getElementById("reg-username").value = user.username;
            document.getElementById("reg-username").readOnly = true; // Usually username is not editable
            document.getElementById("reg-email").value = user.email || "";
            document.getElementById("reg-realname").value = user.realName || "";
            document.getElementById("reg-role").value = user.role;
        } catch (error) {
            showError(error.message || "Failed to load user details for editing.");
            return;
        }
    } else {
        modalTitle.textContent = "代注册新用户";
        passwordField.required = true;
        passwordField.placeholder = "";
        document.getElementById("reg-username").readOnly = false;
    }
    modal.style.display = "block";
}

function closeUserModal() {
    const modal = document.getElementById("user-modal");
    modal.style.display = "none";
}

function confirmDeleteUser(userId) {
    if (confirm("Are you sure you want to delete this user? This action cannot be undone.")) {
        deleteUser(userId)
            .then(() => {
                showSuccess("User deleted successfully!");
                loadUsers(); // Refresh list
            })
            .catch(error => showError(error.message || "Failed to delete user."));
    }
}

// --- Task Detail Page Functions ---
async function loadTaskDetails() {
    const params = new URLSearchParams(window.location.search);
    const taskId = params.get("id");
    if (!taskId) {
        document.getElementById("task-details-content").innerHTML = "<p>No Task ID provided.</p>";
        return;
    }

    try {
        const task = await getTaskDetails(taskId);
        document.getElementById("detail-task-id").textContent = task.taskId;
        document.getElementById("task-detail-title").textContent = `任务详情: ${task.title}`;
        document.getElementById("detail-task-title").textContent = task.title;
        document.getElementById("detail-task-content").innerHTML = task.content || "N/A";
        document.getElementById("detail-task-assignees").textContent = task.assignees ? task.assignees.map(a => a.username).join(", ") : "N/A";
        document.getElementById("detail-task-status").textContent = task.status;
        document.getElementById("detail-task-priority").textContent = task.priority || "N/A";
        document.getElementById("detail-task-creator").textContent = task.creator ? task.creator.username : "N/A";
        document.getElementById("detail-task-created-at").textContent = new Date(task.createdAt).toLocaleString();
        document.getElementById("detail-task-updated-at").textContent = new Date(task.updatedAt).toLocaleString();
        document.getElementById("detail-task-due-date").textContent = task.dueDate ? new Date(task.dueDate).toLocaleString() : "N/A";
        document.getElementById("detail-task-completed-at").textContent = task.completedAt ? new Date(task.completedAt).toLocaleString() : "N/A";
        
        // Load and display relationships and tree
        await loadTaskRelationships(taskId);
        await loadTaskTree(taskId);

    } catch (error) {
        document.getElementById("task-details-content").innerHTML = `<p>Error loading task details: ${error.message}</p>`;
    }
}

async function loadTaskRelationships(taskId) {
    const relationshipsList = document.getElementById("current-relations-list");
    if (!relationshipsList) return;
    relationshipsList.innerHTML = "<li>Loading relationships...</li>";
    try {
        const relationships = await getTaskRelationships(taskId);
        relationshipsList.innerHTML = "";
        if (relations && relationships.length > 0) {
            relations.forEach(rel => {
                // Assuming rel has parentTask, childTask, and relationshipType properties
                // And parentTask/childTask are objects with title and taskId
                let text = ``;
                if (rel.parentTaskId === taskId) {
                    text = `Child: ${rel.childTask.title} (ID: ${rel.childTask.taskId}) - Type: ${rel.relationshipType}`;
                } else if (rel.childTaskId === taskId) {
                    text = `Parent: ${rel.parentTask.title} (ID: ${rel.parentTask.taskId}) - Type: ${rel.relationshipType}`;
                } else {
                    text = `Related: ${rel.otherTask.title} (ID: ${rel.otherTask.taskId}) - Type: ${rel.relationshipType}`; // Adjust if API is different
                }
                const li = document.createElement("li");
                li.textContent = text;
                // Add a delete button for the relationship
                const deleteBtn = document.createElement("button");
                deleteBtn.textContent = "Remove";
                deleteBtn.onclick = async () => {
                    if(confirm("Remove this relationship?")){
                        try {
                            await deleteTaskRelationship(rel.relationshipId);
                            showSuccess("Relationship removed.");
                            loadTaskRelationships(taskId);
                            loadTaskTree(taskId);
                        } catch (err) {
                            showError(err.message || "Failed to remove relationship");
                        }
                    }
                };
                li.appendChild(deleteBtn);
                relationshipsList.appendChild(li);
            });
        } else {
            relationshipsList.innerHTML = "<li>No relationships found.</li>";
        }
    } catch (error) {
        relationshipsList.innerHTML = `<li>Error loading relationships: ${error.message}</li>`;
    }
}

async function loadTaskTree(taskId) {
    const treeContainer = document.getElementById("task-tree-container");
    if (!treeContainer) return;
    treeContainer.innerHTML = "Loading tree...";
    try {
        const treeData = await getTaskTree(taskId);
        // Here you would use a library like D3.js, Vis.js, jsTree, or a custom renderer
        // For simplicity, just display the raw data for now
        // Example: treeContainer.innerHTML = `<pre>${JSON.stringify(treeData, null, 2)}</pre>`;
        
        // Basic textual tree rendering (placeholder)
        if (treeData) {
            treeContainer.innerHTML = ""; // Clear loading
            const rootUl = document.createElement("ul");
            renderTreeNode(treeData, rootUl, taskId);
            treeContainer.appendChild(rootUl);
        } else {
            treeContainer.innerHTML = "No tree data available.";
        }

    } catch (error) {
        treeContainer.innerHTML = `Error loading task tree: ${error.message}`;
    }
}

function renderTreeNode(node, parentElement, currentFocusTaskId, visited = new Set()) {
    if (visited.has(node.taskId)) return;
    visited.add(node.taskId);
    const li = document.createElement("li");
    li.className = "tree-node";
    let taskLink = document.createElement("a");
    
    if (relatedTasks.length > 0) {
        const ul = document.createElement("ul");
        ul.className = "tree-node-children";
        relatedTasks.forEach(relatedTask => {
            renderTreeNode(relatedTask, ul, currentFocusTaskId, new Set(visited));
        });
        li.appendChild(ul);
    }
    taskLink.href = `task-detail.html?id=${node.taskId}`;
    taskLink.textContent = `${node.title} (ID: ${node.taskId})`;
    if (node.taskId === currentFocusTaskId) {
        taskLink.style.fontWeight = "bold";
    }
    li.appendChild(taskLink);

    // 合并父任务和子任务的代码片段
    const relatedTasksMap = new Map();
    node.parents && node.parents.forEach(rel => !visited.has(rel.taskId) && relatedTasksMap.set(rel.taskId, rel));
    node.children && node.children.forEach(rel => !visited.has(rel.taskId) && relatedTasksMap.set(rel.taskId, rel));
    const relatedTasks = Array.from(relatedTasksMap.values());

    if (relatedTasks.length > 0) {
        const ul = document.createElement("ul");
        relatedTasks.forEach(relatedTask => {
            renderTreeNode(relatedTask, ul, currentFocusTaskId, new Set(visited));
        });
        li.appendChild(ul);
    }
    parentElement.appendChild(li);
}


function openRelationsModal(taskId) {
    const modal = document.getElementById("relations-modal");
    if (!modal) return;
    // 重置表单并添加一个隐藏字段存储当前任务ID
    const form = document.getElementById("relations-form");
    form.reset();
    
    // 检查是否已存在隐藏字段，如果不存在则创建
    let hiddenInput = document.getElementById("current-task-id-for-relations");
    if (!hiddenInput) {
        hiddenInput = document.createElement("input");
        hiddenInput.type = "hidden";
        hiddenInput.id = "current-task-id-for-relations";
        hiddenInput.name = "currentTaskId";
        form.appendChild(hiddenInput);
    }
    hiddenInput.value = taskId;
    
    loadTaskRelationships(taskId); // 加载当前关联到模态框列表
    modal.style.display = "block";
}

// --- Utility UI Functions ---
function showError(message) {
    // Simple alert for now, could be a dedicated error div
    console.error("UI Error:", message);
    alert(`Error: ${message}`);
}

function showSuccess(message) {
    // Simple alert for now
    console.log("UI Success:", message);
    alert(`Success: ${message}`);
}

// Ensure UI is initialized after DOM is loaded
// This is already handled by main.js, but if ui.js is loaded standalone or order changes:
// document.addEventListener("DOMContentLoaded", initializeUI);




// --- Task Detail Page: Relationship Management & Tree View ---

async function loadTaskRelationships(taskId) {
    const relationshipsListDiv = document.getElementById("current-relations-list");
    if (!relationshipsListDiv) {
        console.warn("Element with ID 'current-relations-list' not found.");
        return;
    }
    relationshipsListDiv.innerHTML = "<p>Loading relationships...</p>";
    try {
        const relationships = await getTaskRelationships(taskId);
        if (relationships && relationships.length > 0) {
            let html = "<h4>Task Relationships:</h4><ul>";
            relationships.forEach(rel => {
                let displayText = "";
                if (rel.parentTaskId == taskId) { 
                    displayText = `Is parent of <a href=\"task-detail.html?id=${rel.childTaskId}\">${rel.childTaskTitle || 'Task ' + rel.childTaskId}</a> (Type: ${rel.relationshipType})`;
                } else if (rel.childTaskId == taskId) { 
                    displayText = `Is child of <a href=\"task-detail.html?id=${rel.parentTaskId}\">${rel.parentTaskTitle || 'Task ' + rel.parentTaskId}</a> (Type: ${rel.relationshipType})`;
                } else { 
                    displayText = `Generic relation: ${rel.parentTaskTitle || 'Task ' + rel.parentTaskId} & ${rel.childTaskTitle || 'Task ' + rel.childTaskId} (Type: ${rel.relationshipType})`;
                }
                html += `<li>${displayText} <button class=\"delete-relation-btn\" data-task-id=\"${taskId}\" data-relation-id=\"${rel.relationshipId}\">Delete</button></li>`;
            });
            html += "</ul>";
            relationshipsListDiv.innerHTML = html;

            document.querySelectorAll('.delete-relation-btn').forEach(button => {
                button.addEventListener('click', function() {
                    const currentTaskId = this.getAttribute('data-task-id');
                    const relationshipId = this.getAttribute('data-relation-id');
                    confirmDeleteRelationship(currentTaskId, relationshipId);
                });
            });
        } else {
            relationshipsListDiv.innerHTML = "<p>No relationships found for this task.</p>";
        }
    } catch (error) {
        relationshipsListDiv.innerHTML = `<p>Error loading relationships: ${error.message}</p>`;
        console.error("Error in loadTaskRelationships:", error);
    }
}

function closeRelationsModal() {
    const modal = document.getElementById("relations-modal");
    if (modal) modal.style.display = "none";
}

async function confirmDeleteRelationship(taskId, relationshipId) {
    if (confirm("Are you sure you want to delete this relationship?")) {
        try {
            await deleteTaskRelationship(taskId, relationshipId);
            showSuccess("Relationship deleted successfully!");
            loadTaskRelationships(taskId);
            loadTaskTree(taskId);
        } catch (error) {
            showError(error.message || "Failed to delete relationship.");
            console.error("Error in confirmDeleteRelationship:", error);
        }
    }
}

async function loadTaskTree(taskId) {
    const treeViewDiv = document.getElementById("task-tree-container");
    if (!treeViewDiv) {
        console.warn("Element with ID 'task-tree-container' not found.");
        return;
    }
    treeViewDiv.innerHTML = "<p>Loading task tree...</p>";
    try {
        const treeData = await getTaskTree(taskId);
        if (treeData && treeData.taskId) {
            treeViewDiv.innerHTML = "";
            
            // 显示父任务
            if (treeData.parents && treeData.parents.length > 0) {
                // const parentHeader = document.createElement("h4");
                // parentHeader.textContent = "父任务:";
                // treeViewDiv.appendChild(parentHeader);
                
                const parentUl = document.createElement("ul");
                treeData.parents.forEach(parentNode => {
                    renderTaskTreeNode(parentNode, parentUl, 0, parseInt(taskId));
                });
                treeViewDiv.appendChild(parentUl);
            }
            
            // 显示子任务
            // const childrenHeader = document.createElement("h4");
            // childrenHeader.textContent = "子任务:";
            // treeViewDiv.appendChild(childrenHeader);
            
            const ul = document.createElement("ul");
            let level = 0;
            if (treeData.parents)
                level += treeData.parents.length;
            renderTaskTreeNode(treeData, ul, level, parseInt(taskId));
            treeViewDiv.appendChild(ul);
        } else {
            treeViewDiv.innerHTML = "<p>No tree data available or task not found.</p>";
        }
    } catch (error) {
        treeViewDiv.innerHTML = `<p>Error loading task tree: ${error.message}</p>`;
        console.error("Error in loadTaskTree:", error);
    }
}

function renderTaskTreeNode(node, parentElement, level, highlightTaskId) {
    const listItem = document.createElement("li");
    let taskDisplay = node.title ? node.title : `Task ${node.taskId}`;
    
    // 为不同状态设置不同的颜色
    let statusColor = "";
    let statusText = "";
    if (node.status) {
        switch(node.status) {
            case "待办":
                statusColor = "color: blue;";
                statusText = "待办";
                break;
            case "进行中":
                statusColor = "color: orange;";
                statusText = "进行中";
                break;
            case "已完成":
                statusColor = "color: green;";
                statusText = "已完成";
                break;
            case "已取消":
                statusColor = "color: red;";
                statusText = "已取消";
                break;
            default:
                statusColor = "";
                statusText = node.status;
        }
    }
    
    let statusDisplay = statusText ? ` [${statusText}]` : "";
    let taskLink = `<a href=\"task-detail.html?id=${node.taskId}\" style=\"${statusColor}\">${taskDisplay}</a>${statusDisplay}`;

    if (node.taskId === highlightTaskId) {
        taskLink = `<strong>${taskLink} (当前任务)</strong>`;
    }
    listItem.innerHTML = taskLink;
    listItem.style.marginLeft = `${level * 20}px`;

    if (node.children && node.children.length > 0) {
        const childrenUList = document.createElement("ul");
        node.children.forEach(childNode => {
            renderTaskTreeNode(childNode, childrenUList, level + 1, highlightTaskId);
        });
        listItem.appendChild(childrenUList);
    }
    parentElement.appendChild(listItem);
}

function initializeTaskDetailPage() {
    const urlParams = new URLSearchParams(window.location.search);
    const taskId = urlParams.get('id');

    if (taskId) {
        if (typeof loadTaskDetails === 'function') {
            loadTaskDetails(taskId).then(() => {
                loadTaskRelationships(taskId);
                loadTaskTree(taskId);
            }).catch(error => {
                showError("Failed to load task details: " + error.message);
                loadTaskRelationships(taskId);
                loadTaskTree(taskId);
            });
        } else {
            console.warn("loadTaskDetails function is not defined. Skipping initial detail load.");
            loadTaskRelationships(taskId);
            loadTaskTree(taskId);
        }

        const relationsForm = document.getElementById("relations-form");
        if (relationsForm) {
            relationsForm.removeEventListener('submit', handleRelationsFormSubmit);
            relationsForm.addEventListener('submit', handleRelationsFormSubmit);
        } else {
            console.warn("Element with ID 'relations-form' not found.");
        }

        const manageRelationsBtn = document.getElementById("manage-relations-button");
        if (manageRelationsBtn) {
            manageRelationsBtn.addEventListener("click", () => openRelationsModal(taskId));
        } else {
            console.warn("Element with ID 'manage-relations-button' not found.");
        }
        
        const closeRelationsModalButton = document.querySelector("#relations-modal .close-button");
        if (closeRelationsModalButton) {
            closeRelationsModalButton.addEventListener("click", closeRelationsModal);
        }

    } else {
        showError("No Task ID provided in URL.");
        if(document.body) document.body.innerHTML = "<h1>Error: Task ID is missing.</h1>";
    }
}

async function handleRelationsFormSubmit(event) {
    event.preventDefault();
    // 从隐藏字段获取当前任务ID
    const currentTaskId = document.getElementById("current-task-id-for-relations").value;
    const relatedTaskIdValue = document.getElementById("related-task-id").value;
    const relationshipTypeValue = document.getElementById("relationship-type").value;

    if (!currentTaskId || !relatedTaskIdValue || !relationshipTypeValue) {
        showError("请选择一个任务和关联类型。");
        return;
    }
    
    try {
        await addTaskRelationship(currentTaskId, { relatedTaskId: relatedTaskIdValue, relationshipType: relationshipTypeValue });
        showSuccess("关联关系添加成功！");
        closeRelationsModal();
        loadTaskRelationships(currentTaskId);
        loadTaskTree(currentTaskId);
        document.getElementById("related-task-id").value = "";
        document.getElementById("relationship-type").value = "related";
    } catch (error) {
        showError(error.message || "添加关联关系失败。");
        console.error("Error in handleRelationsFormSubmit:", error);
    }
}

if (window.location.pathname.includes("task-detail.html")) {
    document.addEventListener('DOMContentLoaded', initializeTaskDetailPage);
}

// Ensure showError and showSuccess are defined (they might be in the original ui.js or need to be added)
// function showError(message) { console.error(message); alert(message); }
// function showSuccess(message) { console.log(message); /* Optionally show a less intrusive notification */ }

