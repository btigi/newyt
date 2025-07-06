// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Dark Mode Toggle Functionality
(function() {
    // Get the current theme from localStorage or default to 'light'
    const getTheme = () => localStorage.getItem('theme') || 'light';
    
    // Set the theme on the document
    const setTheme = (theme) => {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        updateToggleIcon(theme);
    };
    
    // Update the toggle button icon
    const updateToggleIcon = (theme) => {
        const toggleButton = document.getElementById('theme-toggle');
        if (toggleButton) {
            const icon = toggleButton.querySelector('i');
            if (icon) {
                if (theme === 'dark') {
                    icon.className = 'bi bi-sun';
                    toggleButton.setAttribute('title', 'Switch to light mode');
                } else {
                    icon.className = 'bi bi-moon';
                    toggleButton.setAttribute('title', 'Switch to dark mode');
                }
            }
        }
    };
    
    // Toggle between light and dark themes
    const toggleTheme = () => {
        const currentTheme = getTheme();
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        setTheme(newTheme);
    };
    
    // Initialize theme on page load
    document.addEventListener('DOMContentLoaded', function() {
        const savedTheme = getTheme();
        setTheme(savedTheme);
        
        // Add event listener to toggle button
        const toggleButton = document.getElementById('theme-toggle');
        if (toggleButton) {
            toggleButton.addEventListener('click', toggleTheme);
        }
    });
    
    // Set initial theme immediately (before DOMContentLoaded) to prevent flash
    setTheme(getTheme());
})();

// View Transitions API for Smooth Navigation
(function() {
    // Check if View Transitions API is supported
    if (!document.startViewTransition) {
        return; // Graceful fallback to normal navigation
    }
    
    // Handle navigation with view transitions
    const handleNavigation = (url) => {
        // Ensure theme is applied before transition
        const currentTheme = localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', currentTheme);
        
        // Start view transition
        document.startViewTransition(() => {
            window.location.href = url;
        });
    };
    
    // Intercept navigation clicks
    document.addEventListener('click', function(e) {
        // Check if clicked element is a navigation link
        const link = e.target.closest('a');
        if (!link) return;
        
        const href = link.getAttribute('href');
        if (!href) return;
        
        // Only handle internal navigation (same origin)
        try {
            const url = new URL(href, window.location.href);
            if (url.origin !== window.location.origin) return;
            
            // Skip if it's a hash link or has target="_blank"
            if (href.startsWith('#') || link.target === '_blank') return;
            
            // Skip if it's a form submission or has download attribute
            if (link.download || link.closest('form')) return;
            
            // Prevent default navigation and use view transition
            e.preventDefault();
            handleNavigation(href);
            
        } catch (err) {
            // If URL parsing fails, let normal navigation proceed
            return;
        }
    });
    
    // Handle form submissions with view transitions
    document.addEventListener('submit', function(e) {
        const form = e.target;
        if (!form.matches('form')) return;
        
        // Only handle GET forms (POST forms need their own handling)
        if (form.method.toLowerCase() !== 'get') return;
        
        // Ensure theme is maintained during form navigation
        const currentTheme = localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', currentTheme);
        
        // Let the form submit normally but ensure theme persistence
        setTimeout(() => {
            document.documentElement.setAttribute('data-theme', currentTheme);
        }, 0);
    });
})();

// Sort functionality
function changeSortOrder(sortValue) {
    const url = new URL(window.location);
    url.searchParams.set('SortBy', sortValue);
    
    // Use view transition if supported
    if (document.startViewTransition) {
        document.startViewTransition(() => {
            window.location.href = url.toString();
        });
    } else {
        window.location.href = url.toString();
    }
}

// AJAX functionality for mark watched/unwatched
function markVideoWatched(videoId, handler = 'MarkWatchedAjax') {
    const button = document.querySelector(`[data-video-id="${videoId}"]`);
    if (!button) return;
    
    // Store original button state
    const originalText = button.innerHTML;
    const originalDisabled = button.disabled;
    
    // Show loading state
    button.disabled = true;
    button.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Loading...';
    
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    // Prepare form data
    const formData = new FormData();
    formData.append('videoId', videoId);
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }
    
    // Build URL with handler
    const url = `${window.location.pathname}?handler=${handler}`;
    
    // Make AJAX request
    fetch(url, {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        // Check if response is actually JSON
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            throw new Error(`Expected JSON response but got: ${contentType}`);
        }
        
        return response.json();
    })
    .then(data => {
        if (data.success) {
            // Update UI based on the action
            if (handler === 'MarkWatchedAjax') {
                updateVideoCardAsWatched(videoId);
            } else if (handler === 'MarkUnwatchedAjax') {
                updateVideoCardAsUnwatched(videoId);
            }
            
            // Show success message
            showNotification(data.message, 'success');
        } else {
            // Show error message
            showNotification(data.message, 'error');
            
            // Restore original button state
            button.disabled = originalDisabled;
            button.innerHTML = originalText;
        }
    })
    .catch(error => {
        console.error('Error details:', error);
        showNotification('An error occurred. Please try again.', 'error');
        
        // Restore original button state
        button.disabled = originalDisabled;
        button.innerHTML = originalText;
    });
}

function updateVideoCardAsWatched(videoId) {
    const card = document.querySelector(`[data-video-id="${videoId}"]`).closest('.card');
    if (!card) return;
    
    // Check if we're on the index page (only shows unwatched videos)
    const isIndexPage = window.location.pathname === '/' || window.location.pathname === '/Index';
    
    // Check if we're on channel page with "Unwatched" filter
    const isChannelPage = window.location.pathname.startsWith('/channel/');
    const urlParams = new URLSearchParams(window.location.search);
    const currentFilter = urlParams.get('Filter') || 'Unwatched'; // Default filter is Unwatched
    
    if (isIndexPage || (isChannelPage && currentFilter === 'Unwatched')) {
        // On index page OR channel page with unwatched filter, fade out and remove the card
        card.style.transition = 'opacity 0.5s ease-out';
        card.style.opacity = '0';
        
        setTimeout(() => {
            card.closest('.col-lg-6').remove();
            
            // Check if there are no more video cards and show empty state
            const videoCards = document.querySelectorAll('.card');
            if (videoCards.length === 0) {
                const videoContainer = document.querySelector('.row');
                if (videoContainer) {
                    if (isIndexPage) {
                        videoContainer.innerHTML = `
                            <div class="col-12">
                                <div class="text-center py-5">
                                    <div class="mb-4">
                                        <i class="bi bi-check-circle-fill" style="font-size: 4rem; color: #198754;"></i>
                                    </div>
                                    <h4>All caught up!</h4>
                                    <p class="text-muted">You've watched all available videos.</p>
                                    <a href="/Channels" class="btn btn-outline-info me-2">View Channels</a>
                                    <a href="/AddChannel" class="btn btn-primary">Add New Channel</a>
                                </div>
                            </div>
                        `;
                    } else {
                        videoContainer.innerHTML = `
                            <div class="col-12">
                                <div class="text-center py-5">
                                    <div class="mb-4">
                                        <i class="bi bi-check-circle-fill" style="font-size: 4rem; color: #198754;"></i>
                                    </div>
                                    <h4>All caught up!</h4>
                                    <p class="text-muted">All videos from this channel have been watched!</p>
                                    <div class="mt-3">
                                        <button class="btn btn-outline-secondary" onclick="changeFilter('All')">Show All Videos</button>
                                        <button class="btn btn-outline-secondary" onclick="changeFilter('Watched')">Show Watched Videos</button>
                                    </div>
                                </div>
                            </div>
                        `;
                    }
                }
            }
        }, 500);
    } else {
        // On channel page with "All" or "Watched" filter, update the card to show watched state
        // Add watched styling
        card.classList.add('border-success');
        
        // Add watched header if it doesn't exist
        if (!card.querySelector('.card-header')) {
            const cardHeader = document.createElement('div');
            cardHeader.className = 'card-header bg-success text-white py-1';
            cardHeader.innerHTML = `<small><i class="bi bi-check-circle"></i> Watched ${new Date().toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' })}</small>`;
            card.insertBefore(cardHeader, card.firstChild);
        }
        
        // Update button from "Mark Watched" to "Mark Unwatched"
        const button = card.querySelector(`[data-video-id="${videoId}"]`);
        if (button) {
            button.className = 'btn btn-warning btn-sm';
            button.innerHTML = '<i class="bi bi-arrow-counterclockwise"></i> Mark Unwatched';
            button.onclick = () => markVideoWatched(videoId, 'MarkUnwatchedAjax');
        }
        
        // Update "Watch" button text
        const watchButton = card.querySelector('a[href*="youtube.com"]');
        if (watchButton) {
            watchButton.innerHTML = '<i class="bi bi-play-circle"></i> Watch Again';
        }
        
        // Add watched badges to thumbnail areas
        const thumbnailArea = card.querySelector('.position-relative');
        if (thumbnailArea && !thumbnailArea.querySelector('.position-absolute')) {
            const badge = document.createElement('div');
            badge.className = 'position-absolute top-0 start-0 p-2';
            badge.innerHTML = '<span class="badge bg-success"><i class="bi bi-check-circle"></i> Watched</span>';
            thumbnailArea.appendChild(badge);
        }
    }
}

function updateVideoCardAsUnwatched(videoId) {
    const card = document.querySelector(`[data-video-id="${videoId}"]`).closest('.card');
    if (!card) return;
    
    // Check if we're on different pages
    const isWatchedVideosPage = window.location.pathname === '/WatchedVideos';
    const isChannelPage = window.location.pathname.startsWith('/channel/');
    const urlParams = new URLSearchParams(window.location.search);
    const currentFilter = urlParams.get('Filter') || 'Unwatched'; // Default filter is Unwatched
    
    if (isWatchedVideosPage || (isChannelPage && currentFilter === 'Watched')) {
        // On WatchedVideos page OR channel page with "Watched" filter, fade out and remove the card
        card.style.transition = 'opacity 0.5s ease-out';
        card.style.opacity = '0';
        
        setTimeout(() => {
            card.closest('.col-lg-6').remove();
            
            // Check if there are no more video cards and show empty state
            const videoCards = document.querySelectorAll('.card');
            if (videoCards.length === 0) {
                const videoContainer = document.querySelector('.row');
                if (videoContainer) {
                    if (isWatchedVideosPage) {
                        videoContainer.innerHTML = `
                            <div class="col-12">
                                <div class="text-center py-5">
                                    <div class="mb-4">
                                        <i class="bi bi-check-circle" style="font-size: 4rem; color: #198754;"></i>
                                    </div>
                                    <h4>No watched videos yet</h4>
                                    <p class="text-muted">Videos you mark as watched will appear here.</p>
                                    <a href="/" class="btn btn-primary">Go to Unwatched Videos</a>
                                </div>
                            </div>
                        `;
                    } else {
                        videoContainer.innerHTML = `
                            <div class="col-12">
                                <div class="text-center py-5">
                                    <div class="mb-4">
                                        <i class="bi bi-search" style="font-size: 4rem; color: #6c757d;"></i>
                                    </div>
                                    <h4>No videos found</h4>
                                    <p class="text-muted">No videos from this channel have been watched yet.</p>
                                    <div class="mt-3">
                                        <button class="btn btn-outline-secondary" onclick="changeFilter('All')">Show All Videos</button>
                                        <button class="btn btn-outline-secondary" onclick="changeFilter('Unwatched')">Show Unwatched Videos</button>
                                    </div>
                                </div>
                            </div>
                        `;
                    }
                }
            }
        }, 500);
    } else {
        // Update the card to show unwatched state
        // Remove watched styling
        card.classList.remove('border-success');
        
        // Remove watched header
        const cardHeader = card.querySelector('.card-header');
        if (cardHeader) {
            cardHeader.remove();
        }
        
        // Update button from "Mark Unwatched" to "Mark Watched"
        const button = card.querySelector(`[data-video-id="${videoId}"]`);
        if (button) {
            button.className = 'btn btn-success btn-sm';
            button.innerHTML = '<i class="bi bi-check-circle"></i> Mark Watched';
            button.onclick = () => markVideoWatched(videoId, 'MarkWatchedAjax');
        }
        
        // Update "Watch Again" button text
        const watchButton = card.querySelector('a[href*="youtube.com"]');
        if (watchButton) {
            watchButton.innerHTML = '<i class="bi bi-play-circle"></i> Watch';
        }
        
        // Remove watched badges from thumbnail areas
        const watchedBadges = card.querySelectorAll('.position-absolute .badge.bg-success');
        watchedBadges.forEach(badge => {
            if (badge.textContent.includes('Watched')) {
                badge.parentElement.remove();
            }
        });
    }
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 1050; max-width: 400px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Add to page
    document.body.appendChild(notification);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}
