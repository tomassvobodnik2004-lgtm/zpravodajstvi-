function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);

    const themeIcon = document.getElementById('themeToggleIcon');
    if (themeIcon) {
        if (theme === 'dark') {
            themeIcon.textContent = '🌙 Black';
        } else {
            themeIcon.textContent = '☀️ Light';
        }
    }
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    applyTheme(newTheme);
}

document.addEventListener('DOMContentLoaded', function () {
    const savedTheme = localStorage.getItem('theme') || 'light';
    applyTheme(savedTheme);
});
