/** @type {import('tailwindcss').Config} */
export default {
    darkMode: 'class',
    content: ['./index.html', './src/**/*.{ts,vue}'],
    theme: {
        extend: {
            colors: {
                primary: 'var(--primary)',
                'primary-content': 'var(--primary-content)',
                secondary: 'var(--secondary)',
                'secondary-content': 'var(--secondary-content)',
                tertiary: 'var(--tertiary)',
                'tertiary-content': 'var(--tertiary-content)',
                accent: 'var(--accent)',
                'accent-content': 'var(--accent-content)'
            },
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
                },
                ellipsis: {
                    '0%': { opacity: 0 },
                    '50%': { opacity: 1 },
                    '100%': { opacity: 0 }
                }
            },
            animation: {
                loading: 'loading 1.5s infinite linear',
                ellipsis: 'ellipsis 1.5s infinite'
            }
        }
    },

    plugins: [
        function ({ addUtilities }) {
            addUtilities({
                '.mask-gradient': {
                    'mask-image':
                        'linear-gradient(to bottom, rgba(0, 0, 0, 0) 0%, rgba(0, 0, 0, 0.7) 100%)',
                    '-webkit-mask-image':
                        'linear-gradient(to bottom, rgba(0, 0, 0, 0) 0%, rgba(0, 0, 0, 0.7) 100%)'
                },
                '.animation-delay-300': {
                    'animation-delay': '0.3s'
                },
                '.animation-delay-600': {
                    'animation-delay': '0.6s'
                }
            })
        }
    ]
}
