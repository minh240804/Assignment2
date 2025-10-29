// newsArticleSignalR.js
"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

// Handler for new article publications
connection.on("NewArticlePublished", function (authorName, articleTitle) {
    // Show notification to all users
    toastr.success(`📰 New Article Published by ${authorName}: ${articleTitle}`);
    
    // Refresh the article list if we're on the index page
    if (window.location.pathname.toLowerCase().includes('/newsarticle/index')) {
        refreshArticleList();
    }
});

// Handler for article updates
connection.on("ArticleUpdated", function (articleId, title, content) {
    // Update the article content if we're viewing it
    if (window.location.pathname.toLowerCase().includes(`/newsarticle/details/${articleId}`)) {
        updateArticleContent(title, content);
    }
    toastr.info(`Article "${title}" has been updated`);
});

// Handler for article deletions
connection.on("ArticleDeleted", function (articleId, title) {
    toastr.warning(`Article "${title}" has been deleted`);
    
    // Redirect if we're viewing the deleted article
    if (window.location.pathname.toLowerCase().includes(`/newsarticle/details/${articleId}`)) {
        window.location.href = '/NewsArticle/Index';
    }
    
    // Refresh the list if we're on the index page
    if (window.location.pathname.toLowerCase().includes('/newsarticle/index')) {
        refreshArticleList();
    }
});

// Handler for dashboard updates
connection.on("UpdateDashboardCounts", function () {
    if (window.location.pathname.toLowerCase().includes('/dashboard')) {
        refreshDashboardCounts();
    }
});

// Function to join article viewing group
function joinArticleGroup(articleId) {
    connection.invoke("JoinArticleGroup", articleId.toString()).catch(function (err) {
        console.error(err);
    });
}

// Function to leave article viewing group
function leaveArticleGroup(articleId) {
    connection.invoke("LeaveArticleGroup", articleId.toString()).catch(function (err) {
        console.error(err);
    });
}

// Function to refresh article list via AJAX
function refreshArticleList() {
    $.get(window.location.href, function(data) {
        var newDoc = new DOMParser().parseFromString(data, 'text/html');
        var newTable = newDoc.querySelector('.table');
        document.querySelector('.table').replaceWith(newTable);
    });
}

// Function to update article content
function updateArticleContent(title, content) {
    document.querySelector('.article-title').textContent = title;
    document.querySelector('.article-content').textContent = content;
}

// Function to refresh dashboard counts
function refreshDashboardCounts() {
    $.get('/Dashboard/GetCounts', function(data) {
        Object.keys(data).forEach(function(key) {
            document.querySelector(`#${key}Count`).textContent = data[key];
        });
    });
}

// Start the connection
async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.log(err);
        setTimeout(startConnection, 5000);
    }
};

connection.onclose(startConnection);
startConnection();