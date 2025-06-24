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
