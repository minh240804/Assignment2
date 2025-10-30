// accountNotifications.js
"use strict";

/* =========================
   1) Tạo kết nối toàn cục
   ========================= */
window.connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

/* =========================
   2) Utils
   ========================= */
function debounce(fn, ms) {
    let t;
    return function () {
        clearTimeout(t);
        t = setTimeout(fn, ms);
    };
}
const debouncedReload = debounce(() => location.reload(), 300);

/* =========================
   3) Start / Register / Unregister
   ========================= */

// Start (guard không start trùng)
async function startSignalRConnection(accountId) {
    try {
        if (connection.state === "Disconnected") {
            console.log("[SR] start()");
            await connection.start();
            console.log("[SR] started. id:", connection.connectionId);
        } else {
            console.log("[SR] skip start, state:", connection.state);
        }

        // Nếu có accountId được truyền từ layout -> join group account (ACK nếu hub trả string)
        if (accountId) {
            try {
                const ack = await connection.invoke("RegisterConnection", String(accountId));
                console.log("[SR] Joined account group (on start):", ack || "(no ack)");
            } catch (e) {
                console.warn("[SR] RegisterConnection (on start) failed:", e);
            }
        }
    } catch (err) {
        console.error("[SR] start failed:", err);
        setTimeout(() => startSignalRConnection(accountId), 2000);
    }
}

// Register groups theo role + account sau khi đã Connected
async function registerRoleAndAccount() {
    if (connection.state !== "Connected") {
        console.log("[SR] wait Connected…", connection.state);
        setTimeout(registerRoleAndAccount, 200);
        return;
    }
    try {
        // Role
        if (Number.isInteger(window.userRole) && window.userRole >= 0) {
            const ackRole = await connection.invoke("RegisterUserRole", Number(window.userRole));
            console.log("[SR] Joined role group:", ackRole || "(none)");
        } else {
            console.log("[SR] No userRole -> skip");
        }

        // Account
        if (window.currentAccountId && Number(window.currentAccountId) > 0) {
            const ackAcc = await connection.invoke("RegisterConnection", String(window.currentAccountId));
            console.log("[SR] Joined account group:", ackAcc || "(none)");
        } else {
            console.log("[SR] No accountId -> skip");
        }
    } catch (e) {
        console.error("[SR] registerRoleAndAccount error:", e);
    }
}
window.startSignalRConnection = startSignalRConnection;
window.registerRoleAndAccount = registerRoleAndAccount;

// Unregister role rồi stop (dùng trước khi logout)
async function unregisterRoleThenStop() {
    try {
        if (connection.state === "Connected" &&
            Number.isInteger(window.userRole) && window.userRole >= 0) {
            await connection.invoke("UnregisterUserRole", Number(window.userRole));
            console.log("[SR] Unregistered role group.");
        } else {
            console.log("[SR] Skip UnregisterUserRole. state:", connection.state, "userRole:", window.userRole);
        }
    } catch (e) {
        console.warn("[SR] UnregisterUserRole failed:", e);
    } finally {
        try {
            if (connection.state !== "Disconnected") {
                await connection.stop();
                console.log("[SR] Stopped connection.");
            }
        } catch (err) {
            console.warn("[SR] stop failed:", err);
        }
    }
}
window.unregisterRoleThenStop = unregisterRoleThenStop;

/* =========================
   4) Handlers từ server
   ========================= */

// Tạo account mới (ví dụ chỉ Staff thấy như bạn đã set trước đó)
connection.on("ReceiveNewAccountNotification", function (message) {
    // 0=Admin, 1=Staff, 2=Lecturer (theo mapping bạn đang dùng)
    if (window.userRole === 1) {
        if (window.toastr) toastr.info(message);
        else console.log("[Toast]", message);
    }
});

// Publish bài mới — server có thể gửi 1 param (message) hoặc 2 param (authorName, articleTitle)
// Hỗ trợ cả 2 dạng:
connection.on("NewArticlePublished", function (...args) {
    let text;
    if (args.length >= 2) {
        const [authorName, articleTitle] = args;
        text = `New article: ${articleTitle} — by ${authorName}`;
    } else {
        text = args[0] || "New article published";
    }
    if (window.toastr) toastr.success(text);
    else console.log("[Toast]", text);

    // Nếu đang ở trang danh sách thì tự reload
    try {
        if (window.isNewsList) {
            debouncedReload();
        }
    } catch (e) { console.error(e); }
});

// Cập nhật bài — nếu đang xem bài đó hoặc đang ở list -> reload
connection.on("UpdateNewsArticle", function (articleId) {
    try {
        const sameArticle = String(window.currentArticleId || "") === String(articleId);
        if (sameArticle || window.isNewsList) {
            debouncedReload();
        }
    } catch (e) { console.error(e); }
});

// Cập nhật nội dung bài cho viewers đang trong group bài (toast nhẹ)
connection.on("ArticleUpdated", function (articleId, title /*, content */) {
    if (window.toastr) toastr.info(`Article updated: ${title || articleId}`);
    else console.log("[Toast] Article updated:", title || articleId);
});

// Xóa bài — cảnh báo và nếu đang ở trang bài đó thì quay về index
connection.on("ArticleDeleted", function (articleId, title) {
    const msg = `Article deleted: ${title || articleId}`;
    if (window.toastr) toastr.warning(msg);
    else console.log("[Toast]", msg);

    const sameArticle = String(window.currentArticleId || "") === String(articleId);
    if (sameArticle) window.location.href = "/NewsArticleManagement/Index";
});

// Deactivate account — nếu là account hiện tại thì logout
connection.on("AccountDeactivated", function (accountId) {
    const currentId = parseInt(window.currentAccountId);
    const deactivatedId = parseInt(accountId);
    console.log("[SR] AccountDeactivated:", { currentId, deactivatedId });

    if (currentId === deactivatedId) {
        if (window.toastr) {
            toastr.warning("Your account has been deactivated", null, {
                timeOut: 0, extendedTimeOut: 0, closeButton: true, tapToDismiss: false
            });
        }
        const form = document.createElement("form");
        form.method = "post";
        form.action = "/AccountManagement/Login?handler=Logout";
        document.body.appendChild(form);
        form.submit();
    }
});

// ForceLogout theo group account
connection.on("ForceLogout", function (payload) {
    if (window.toastr) {
        toastr.warning("You have been logged out: " + (payload?.reason || "forced"));
    } else {
        console.log("[Toast] Forced logout:", payload);
    }
    const form = document.createElement("form");
    form.method = "post";
    form.action = "/AccountManagement/Login?handler=Logout";
    document.body.appendChild(form);
    form.submit();
});

// Thông báo tạo Category
connection.on("ReceiveCreateCategoryNotification", function (message) {
    console.log("[SR] CreateCategory:", message);
    if (window.toastr) toastr.info(message);
    else console.log("[Toast]", message);
});

/* =========================
   5) Lifecycle logs & re-register
   ========================= */
connection.onreconnecting(err => console.warn("[SR] reconnecting:", err, "state:", connection.state));
connection.onreconnected(async () => {
    console.log("[SR] reconnected. id:", connection.connectionId);
    // Sau reconnect -> join lại group
    try { await registerRoleAndAccount(); } catch (e) { console.error(e); }
});
connection.onclose(async (err) => {
    console.warn("[SR] closed:", err, "state:", connection.state);
    // Tự start lại cho thân thiện (nếu còn đăng nhập)
    try {
        if (window.currentAccountId && Number(window.currentAccountId) > 0) {
            await startSignalRConnection(window.currentAccountId);
            await registerRoleAndAccount();
        }
    } catch (e) { console.error(e); }
});
