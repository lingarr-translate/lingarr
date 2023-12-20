/** @type {import('tailwindcss').Config} */
export default {
    content: ['./index.html', './src/**/*.{ts,vue}'],
    theme: {
        extend: {
            keyframes: {
                loading: {
                    '0%': {
                        transform: 'translateX(0) scaleX(0)'
                    },
                    '40%': {
                        transform: 'translateX(0) scaleX(0.4)'
                    },
                    '100%': {
                        transform: 'translateX(100%) scaleX(0.5)'
                    }
                }
            },
            animation: {
                loading: 'loading 1.5s infinite linear'
            }
        }
    },
    plugins: [],
    keyframes: {}
}
