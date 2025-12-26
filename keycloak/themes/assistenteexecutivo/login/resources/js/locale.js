// Locale selector functionality
(function() {
    'use strict';
    
    document.addEventListener('DOMContentLoaded', function() {
        const localeLink = document.getElementById('kc-current-locale-link');
        const localeDropdown = document.getElementById('kc-locale-dropdown');
        const dropdownList = document.getElementById('kc-locale-dropdown-list');
        
        if (localeLink && localeDropdown && dropdownList) {
            // Toggle dropdown on click
            localeLink.addEventListener('click', function(e) {
                e.preventDefault();
                const isOpen = dropdownList.style.display === 'block';
                dropdownList.style.display = isOpen ? 'none' : 'block';
            });
            
            // Close dropdown when clicking outside
            document.addEventListener('click', function(e) {
                if (!localeDropdown.contains(e.target)) {
                    dropdownList.style.display = 'none';
                }
            });
            
            // Highlight selected locale
            const selectedItem = dropdownList.querySelector('.kc-locale-item-selected');
            if (selectedItem) {
                const selectedLink = selectedItem.querySelector('a');
                if (selectedLink) {
                    localeLink.textContent = selectedLink.textContent;
                }
            }
        }
    });
})();


