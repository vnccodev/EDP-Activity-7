function submitLogin() {
    const user = document.getElementById('loginUsername').value;
    const pass = document.getElementById('loginPassword').value;

    if (!user || !pass) {
        alert("Please provide both username and password.");
        return;
    }

    const payload = {
        Username: user,
        Password: pass
    };

    // Route directly to your existing C# AuthController endpoint
    fetch('http://localhost:5291/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
    .then(async response => {
        if (response.ok) {
            // Write authorization state directly to local browser storage
            localStorage.setItem('aix_authenticated', 'true');
            localStorage.setItem('aix_active_user', user);
            
            // Redirect straight through the gate to the main application
            window.location.href = 'overview.html';
        } else {
            alert("Access Denied: Invalid credentials. Please verify your entries.");
        }
    })
    .catch(error => {
        console.error("Authentication gateway error:", error);
        alert("Unable to establish connection with the authentication server. Verify that your C# API is running via 'dotnet run'.");
    });
}