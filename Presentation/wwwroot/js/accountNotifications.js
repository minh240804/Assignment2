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
    // If this is the deactivated user, redirect to logout
    if (window.currentAccountId === accountId) {
        toastr.warning("Your account has been deactivated");
        setTimeout(() => {
            window.location.href = '/Account/Logout';
        }, 2000);
    }
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