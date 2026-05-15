// ==========================================
// GATEWAY CHECK (EXECUTES IMMEDIATELY)
// ==========================================
if (localStorage.getItem('aix_authenticated') !== 'true') {
    alert("Unauthorized Access: Please sign in to access system records.");
    window.location.href = 'login.html';
}

// Global logout hook to wire to your sidebar profile or exit button
function executeLogout() {
    localStorage.removeItem('aix_authenticated');
    localStorage.removeItem('aix_active_user');
    window.location.href = 'login.html';
}

// ==========================================
// 1. DATA LOADING (READ)
// ==========================================
function loadUsers() {
    const searchInput = document.getElementById('searchInput');
    const searchTerm = searchInput ? searchInput.value : "";

    fetch(`http://localhost:5291/api/user/search?term=${encodeURIComponent(searchTerm)}`)
        .then(response => response.json())
        .then(data => {
            const tbody = document.getElementById('userTableBody');
            if (!tbody) return;
            
            tbody.innerHTML = ""; 
            
            data.forEach(user => {
                const tr = document.createElement('tr');
                const statusText = user.is_active ? "Active" : "Inactive";
                
                // Clean HTML generation. The parameters are cleanly encoded into the button.
                tr.innerHTML = `
                    <td>${user.user_id}</td>
                    <td>${user.username}</td>
                    <td>${user.email}</td>
                    <td>${user.role}</td>
                    <td>${statusText}</td>
                    <td>
                        <button 
                            type="button"
                            style="cursor: pointer; color: #6a957a; background: none; border: none; font-weight: bold; text-decoration: underline; margin-right: 12px;"
                            data-id="${user.user_id}"
                            data-username="${user.username}"
                            data-email="${user.email}"
                            data-role="${user.role}"
                            data-active="${user.is_active}"
                            onclick="triggerEdit(this)">Edit</button>
                            
                        <button 
                            type="button"
                            style="cursor: pointer; color: #a84242; background: none; border: none; font-weight: bold; text-decoration: underline;"
                            onclick="requestDelete(${user.user_id}, '${user.username}')">Delete</button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        })
        .catch(error => console.error("Error loading users:", error));
}

// ==========================================
// 2. ADD USER MODAL (CREATE)
// ==========================================
function openAddModal() {
    const modal = document.getElementById('addUserModal');
    if (modal) modal.classList.remove('hidden');
}

function closeAddModal() {
    const modal = document.getElementById('addUserModal');
    if (modal) modal.classList.add('hidden');
    
    // Reset fields on close
    document.getElementById('newUsername').value = "";
    document.getElementById('newEmail').value = "";
    document.getElementById('newPassword').value = "";
    document.getElementById('newRole').value = "Admin";
}

function submitNewUser() {
    const user = document.getElementById('newUsername').value;
    const email = document.getElementById('newEmail').value;
    const pass = document.getElementById('newPassword').value;
    const role = document.getElementById('newRole').value;

    if (!user || !email || !pass) {
        alert("Please fill in all required fields.");
        return;
    }

    const payload = {
        Username: user,
        Email: email,
        Password: pass,
        Role: role
    };

    fetch('http://localhost:5291/api/user/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
    .then(response => {
        if (response.ok) {
            alert("User added successfully!");
            closeAddModal();
            loadUsers();
        } else {
            alert("Failed to add user. Check database or API constraints.");
        }
    })
    .catch(error => console.error("Error adding user:", error));
}

// ==========================================
// 3. EDIT USER MODAL (UPDATE)
// ==========================================
function triggerEdit(buttonElement) {
    // Safely pull embedded strings directly from the clicked button's data attributes
    const id = buttonElement.getAttribute('data-id');
    const username = buttonElement.getAttribute('data-username');
    const email = buttonElement.getAttribute('data-email');
    const role = buttonElement.getAttribute('data-role');
    const isActive = buttonElement.getAttribute('data-active');

    // Populate the form fields explicitly
    document.getElementById('editUserId').value = id;
    document.getElementById('editUsername').value = username;
    document.getElementById('editEmail').value = email;
    document.getElementById('editRole').value = role;
    document.getElementById('editStatus').value = (isActive === "true" || isActive === true || isActive === "1") ? "true" : "false";

    // Unhide modal
    const modal = document.getElementById('editUserModal');
    if (modal) modal.classList.remove('hidden');
}

function closeEditModal() {
    const modal = document.getElementById('editUserModal');
    if (modal) {
        modal.classList.add('hidden'); // Fully hides the modal window allowing page interaction again
    }
    
    // Clear input cache to prevent stale state persistence
    document.getElementById('editUserId').value = "";
    document.getElementById('editUsername').value = "";
    document.getElementById('editEmail').value = "";
}

function submitEditUser() {
    const idStr = document.getElementById('editUserId').value;
    const username = document.getElementById('editUsername').value;
    const email = document.getElementById('editEmail').value;
    const role = document.getElementById('editRole').value;
    const isActiveStr = document.getElementById('editStatus').value;

    const numericId = parseInt(idStr, 10);

    if (isNaN(numericId) || numericId <= 0) {
        alert("CRITICAL ERROR: User ID failed to bind. Ensure you clicked Edit from a populated row.");
        return;
    }

    const payload = {
        UserId: numericId,
        Username: username,
        Email: email,
        Role: role,
        IsActive: (isActiveStr === "true")
    };

    fetch('http://localhost:5291/api/user/update', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
    .then(async response => {
        if (response.ok) {
            alert("User profile updated successfully!");
            closeEditModal();
            loadUsers(); // Live refresh of the table data
        } else {
            const err = await response.json();
            alert("Update rejected by server:\n" + JSON.stringify(err));
        }
    })
    .catch(error => console.error("Network Update Error:", error));
}

// ==========================================
// 4. DELETION PROTOCOL (DELETE)
// ==========================================
function requestDelete(userId, username) {
    // 1. Stage the ID in the hidden input field
    const pendingInput = document.getElementById('pendingDeleteId');
    if (pendingInput) {
        pendingInput.value = userId;
    } else {
        console.error("CRITICAL: Hidden input 'pendingDeleteId' not found in HTML.");
        return;
    }

    // 2. Update the confirmation text
    const targetLabel = document.getElementById('deleteTargetUser');
    if (targetLabel) {
        targetLabel.innerText = `${username} (ID: ${userId})`;
    }

    // 3. Reveal the modal overlay
    const modal = document.getElementById('deleteConfirmModal');
    if (modal) {
        modal.classList.remove('hidden');
        modal.style.display = "flex";
    }
}

function closeDeleteModal() {
    const modal = document.getElementById('deleteConfirmModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.style.display = "none";
    }
    // Clear the pending cache
    const pendingInput = document.getElementById('pendingDeleteId');
    if (pendingInput) pendingInput.value = "";
}

function executeDelete() {
    const pendingInput = document.getElementById('pendingDeleteId');
    const idStr = pendingInput ? pendingInput.value : "";
    const numericId = parseInt(idStr, 10);

    if (isNaN(numericId) || numericId <= 0) {
        alert("System Error: Unresolvable target ID. Please close and try again.");
        return;
    }

    // Transmit the HTTP DELETE request straight to the C# API controller
    fetch(`http://localhost:5291/api/user/delete/${numericId}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(async response => {
        if (response.ok) {
            alert("Success: Record permanently purged from system.");
            closeDeleteModal();
            loadUsers(); // Instantly reload the table to visually sync the deletion
        } else {
            // Attempt to read server error message safely
            let errorMsg = "Unknown backend rejection.";
            try {
                const errData = await response.json();
                errorMsg = errData.message || JSON.stringify(errData);
            } catch(e) {
                errorMsg = `HTTP Status ${response.status}`;
            }
            alert(`Destruction request rejected:\n${errorMsg}`);
        }
    })
    .catch(error => {
        console.error("Deletion Pipeline Fault:", error);
        alert("Network failure: Unable to communicate with the C# backend server.");
    });
}