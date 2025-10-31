/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: "class",
    content: [
        "./Components/**/*.razor",
        "./Pages/**/*.cshtml",
        "./wwwroot/**/*.html"
    ],
    theme: {
        container: {
            center: true,
        },
        extend: {
            colors: {
                primary: {
                    DEFAULT: '#3b82f6',
                    50: '#eff6ff',
                    100: '#dbeafe',
                    200: '#bfdbfe',
                    300: '#93c5fd',
                    400: '#60a5fa',
                    500: '#3b82f6',
                    600: '#2563eb',
                    700: '#1d4ed8',
                    800: '#1e40af',
                    900: '#1e3a8a'
                },
                secondary: {
                    DEFAULT: '#6B7280',
                    50: "#F1F2F3",
                    100: "#E0E2E5",
                    200: "#C2C5CC",
                    300: "#A6ABB5",
                    400: "#888E9B",
                    500: "#6B7280",
                    600: "#565C67",
                    700: "#41454E",
                    800: "#2A2D32",
                    900: "#151619"
                }
            }
        }
    },
    plugins: [
        require('@tailwindcss/forms')
    ]
}