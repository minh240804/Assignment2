// accountNotifications.js
"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNewAccountNotification", function (message) {
    // Show notification to staff users
    if (window.userRole === 1) { // Staff role
        toastr.info(message);
    }
});

connection.on("AccountDeactivated", function (accountId) {
    // Convert both to numbers for comparison since SignalR might send it as a string
    const currentId = parseInt(window.currentAccountId);
    const deactivatedId = parseInt(accountId);
    
    console.log("Received deactivation for account:", deactivatedId);
    console.log("Current account:", currentId);

    // If this is the deactivated user, redirect to logout
    if (currentId === deactivatedId) {
        toastr.warning("Your account has been deactivated", null, {
            timeOut: 0,
            extendedTimeOut: 0,
            closeButton: true,
            tapToDismiss: false
        });
        
        // Submit the logout form
        const logoutForm = document.createElement('form');
        logoutForm.method = 'post';
        logoutForm.action = '/AccountManagement/Login?handler=Logout';
        document.body.appendChild(logoutForm);
        logoutForm.submit();
    }
});

connection.on("ReceiveCreateCategoryNotification", function (message) {
    // show to all users
    console.log(message);
    toastr.info(message);
});

// Function to start the connection and register the current user's connection
async function startSignalRConnection(accountId) {
    try {
        await connection.start();
        if (accountId) {
            await connection.invoke("RegisterConnection", accountId);
        }
    } catch (err) {
        console.error(err);
        setTimeout(() => startSignalRConnection(accountId), 5000);
    }
}

// Reconnect if connection is lost
connection.onclose(async () => {
    await startSignalRConnection(window.currentAccountId);
});