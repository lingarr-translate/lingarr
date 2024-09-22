import { createThemes } from 'tw-colors'

/** @type {import('tailwindcss').Config} */
export default {
    darkMode: 'class',
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

    plugins: [
        function ({ addUtilities }) {
            addUtilities({
                '.mask-gradient': {
                    'mask-image':
                        'linear-gradient(to bottom, rgba(0, 0, 0, 0) 0%, rgba(0, 0, 0, 0.7) 100%)',
                    '-webkit-mask-image':
                        'linear-gradient(to bottom, rgba(0, 0, 0, 0) 0%, rgba(0, 0, 0, 0.7) 100%)'
                }
            })
        },
        createThemes({
            'solarized-light': {
                primary: '#fdf6e3',
                'primary-content': '#586e75',
                secondary: '#eee8d5',
                'secondary-content': '#657b83',
                tertiary: '#f5efdc',
                'tertiary-content': '#586e75',
                accent: '#268bd2',
                'accent-content': '#657b83'
            },
            'solarized-dark': {
                primary: '#002b36',
                'primary-content': '#fdf6e3',
                secondary: '#073642',
                'secondary-content': '#eee8d5',
                tertiary: '#04313c',
                'tertiary-content': '#fdf6e3',
                accent: '#2aa198',
                'accent-content': '#fdf6e3'
            },
            dracula: {
                primary: '#282a36',
                'primary-content': '#f8f8f2',
                secondary: '#44475a',
                'secondary-content': '#f8f8f2',
                tertiary: '#363948',
                'tertiary-content': '#f8f8f2',
                accent: '#bd93f9',
                'accent-content': '#f8f8f2'
            },
            nord: {
                primary: '#2e3440',
                'primary-content': '#d8dee9',
                secondary: '#3b4252',
                'secondary-content': '#e5e9f0',
                tertiary: '#353b49',
                'tertiary-content': '#d8dee9',
                accent: '#5e81ac',
                'accent-content': '#e5e9f0'
            },
            monokai: {
                primary: '#272822',
                'primary-content': '#f8f8f2',
                secondary: '#3a3b35',
                'secondary-content': '#f8f8f2',
                tertiary: '#31322c',
                'tertiary-content': '#f8f8f2',
                accent: '#66d9ef',
                'accent-content': '#f8f8f2'
            },
            'material-dark': {
                primary: '#212121',
                'primary-content': '#ffffff',
                secondary: '#303030',
                'secondary-content': '#ffffff',
                tertiary: '#292929',
                'tertiary-content': '#ffffff',
                accent: '#03a9f4',
                'accent-content': '#ffffff'
            },
            gotham: {
                primary: '#0a0f19',
                'primary-content': '#c5c8c6',
                secondary: '#17202a',
                'secondary-content': '#d6d8d6',
                tertiary: '#111822',
                'tertiary-content': '#c5c8c6',
                accent: '#5e81ac',
                'accent-content': '#d6d8d6'
            },
            gruvbox: {
                primary: '#1d2021',
                'primary-content': '#ebdbb2',
                secondary: '#282828',
                'secondary-content': '#ebdbb2',
                tertiary: '#232425',
                'tertiary-content': '#ebdbb2',
                accent: '#fb4934',
                'accent-content': '#ebdbb2'
            },
            'cyberpunk-neon': {
                primary: '#121212',
                'primary-content': '#ffffff',
                secondary: '#1e1e1e',
                'secondary-content': '#00ffff',
                tertiary: '#181818',
                'tertiary-content': '#ff00ff',
                accent: '#ff0084',
                'accent-content': '#ffff00'
            },
            horizon: {
                primary: '#29113f',
                'primary-content': '#fff5f5',
                secondary: '#371950',
                'secondary-content': '#fff5f5',
                tertiary: '#301548',
                'tertiary-content': '#fff5f5',
                accent: '#f472b6',
                'accent-content': '#fff5f5'
            },
            lingarr: {
                primary: '#1a202c',
                'primary-content': '#c0c8d2',
                secondary: '#242d3c',
                'secondary-content': '#d2d7dc',
                tertiary: '#1f2734',
                'tertiary-content': '#c0c8d2',
                accent: '#466e8c',
                'accent-content': '#c0c8d2'
            }
        })
    ]
}
