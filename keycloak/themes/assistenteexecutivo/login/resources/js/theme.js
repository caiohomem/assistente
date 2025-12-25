// Theme toggle functionality
(function() {
    'use strict';
    
    // Initialize theme on page load
    function initTheme() {
        const savedTheme = localStorage.getItem('kc-theme') || 'light';
        const html = document.documentElement;
        
        if (savedTheme === 'dark') {
            html.classList.add('dark');
        } else {
            html.classList.remove('dark');
        }
        
        updateThemeIcon(savedTheme);
    }
    
    // Update theme icon visibility
    function updateThemeIcon(theme) {
        const lightIcon = document.querySelector('.theme-icon-light');
        const darkIcon = document.querySelector('.theme-icon-dark');
        
        if (lightIcon && darkIcon) {
            if (theme === 'dark') {
                lightIcon.style.display = 'none';
                darkIcon.style.display = 'inline';
            } else {
                lightIcon.style.display = 'inline';
                darkIcon.style.display = 'none';
            }
        }
    }
    
    // Toggle theme
    function toggleTheme() {
        const html = document.documentElement;
        const isDark = html.classList.contains('dark');
        
        if (isDark) {
            html.classList.remove('dark');
            localStorage.setItem('kc-theme', 'light');
            updateThemeIcon('light');
        } else {
            html.classList.add('dark');
            localStorage.setItem('kc-theme', 'dark');
            updateThemeIcon('dark');
        }
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheme);
    } else {
        initTheme();
    }
    
    // Attach toggle handler
    document.addEventListener('DOMContentLoaded', function() {
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', toggleTheme);
        }
    });
})();

