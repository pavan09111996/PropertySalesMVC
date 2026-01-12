function submitAdminLogin() {

    var adminId = document.getElementById("AdminID").value;
    var password = document.getElementById("AdminPassword").value;
    var errorDiv = document.getElementById("loginError");

    errorDiv.innerText = "";

    if (adminId === "" || password === "") {
        errorDiv.innerText = "Please enter Admin ID and Password";
        return;
    }

    fetch('/Admin/Login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body:
            'adminId=' + encodeURIComponent(adminId) +
            '&password=' + encodeURIComponent(password)
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                window.location.href = '/Admin/Dashboard';
            } else {
                errorDiv.innerText = data.message;
            }
        })
        .catch(() => {
            errorDiv.innerText = "Server error. Please try again.";
        });
}
