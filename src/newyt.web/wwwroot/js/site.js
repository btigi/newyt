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
