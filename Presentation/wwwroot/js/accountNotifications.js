// accountNotifications.js
"use strict";

// Create global connection that can be reused by other pages
window.sharedSignalRConnection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

// Use the global connection
var connection = window.sharedSignalRConnection;

connection.on("ReceiveNewAccountNotification", function (message) {
    if (window.userRole === 1) { 
        toastr.info(message);
    }
});

connection.on("NewArticlePublished", function (message) {
        toastr.info(message);
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

function debounce(fn, ms) { let t; return function () { clearTimeout(t); t = setTimeout(fn, ms); }; }

connection.on("UpdateNewsArticle", function (articleId) {
    console.log("[SR] UpdateNewsArticle received for:", articleId,
        " currentArticleId:", window.currentArticleId,
        " isNewsList:", window.isNewsList);

    try {
        const sameArticle = String(window.currentArticleId) === String(articleId);
        if (sameArticle || window.isNewsList) {
            // nếu chưa có debounce, dùng setTimeout
            if (typeof debounce === "function") debounce(() => location.reload(), 300)();
            else setTimeout(() => location.reload(), 300);
        }
    } catch (e) { console.error(e); }
});





connection.on("ReceiveCreateCategoryNotification", function (message) {
    // show to all users
    toastr.info(message);
});

connection.on("ReloadCategoryList", function () {
    console.log("[SR] ReloadCategoryList received");
    try {
        if (window.isCategoryList) {
            if (typeof debounce === "function") {
                debounce(() => location.reload(), 300)();
            } else {
                setTimeout(() => location.reload(), 300);
            }
        }
    } catch (e) {
        console.error("[SR] Error during ReloadCategoryList:", e);
    }
});


// Function to start the connection and register the current user's connection
async function startSignalRConnection(accountId) {
    try {
        console.log("%c=== Starting SignalR Connection ===", "background: orange; color: white; padding: 5px; font-size: 14px;");
        console.log("Account ID:", accountId);
        console.log("Current state:", connection.state, `(0=Disconnected, 1=Connected, 2=Connecting)`);
        console.log("Hub URL:", connection.baseUrl || "/notificationHub");
        
        // Only start if not already connected or connecting
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            console.log("? Calling connection.start()...");
            
            try {
                await connection.start();
                console.log("%c? SignalR Connected Successfully!", "color: green; font-weight: bold; font-size: 16px;");
                console.log("Connection ID:", connection.connectionId);
                console.log("Connection State:", connection.state);
            } catch (startError) {
                console.error("%c? connection.start() FAILED!", "color: red; font-weight: bold; font-size: 16px; background: yellow; padding: 5px;");
                console.error("Error type:", startError.name);
                console.error("Error message:", startError.message);
                console.error("Error stack:", startError.stack);
                console.error("Full error object:", startError);
                
                // Check specific errors
                if (startError.message && startError.message.includes("negotiate")) {
                    console.error("? NEGOTIATE FAILED - Hub endpoint might not be accessible");
                    console.error("? Check if /notificationHub exists");
                } else if (startError.message && startError.message.includes("WebSocket")) {
                    console.error("? WEBSOCKET FAILED - Will fallback to Long Polling");
                }
                
                console.warn("? Will retry in 5 seconds...");
                setTimeout(() => startSignalRConnection(accountId), 5000);
                return;
            }
        } else {
            console.log("? Connection already in state:", connection.state);
        }
        
        // Only register connection if user is logged in
        if (accountId && accountId > 0) {
            try {
                await connection.invoke("RegisterConnection", accountId.toString());
                console.log("? Registered connection for account:", accountId);
            } catch (invokeError) {
                console.error("? RegisterConnection failed:", invokeError);
            }
        } else {
            console.log("? Guest user - connection started but not registered");
        }
    } catch (err) {
        console.error("%c? UNEXPECTED ERROR in startSignalRConnection:", "color: red; font-weight: bold; background: yellow; padding: 5px;");
        console.error("Error:", err);
        console.error("Retrying in 5 seconds...");
        setTimeout(() => startSignalRConnection(accountId), 5000);
    }
}







// Reconnect if connection is lost
connection.onclose(async () => {
    console.log("SignalR connection closed, attempting to reconnect...");
    await startSignalRConnection(window.currentAccountId);
});

// Make the function globally available
window.startSignalRConnection = startSignalRConnection;