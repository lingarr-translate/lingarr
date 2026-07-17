import { defineConfig } from 'vitepress'

export default defineConfig({
    base: '/lingarr/',
    vite: {
        server: {
            watch: {
                usePolling: true
            }
        }
    },
    title: 'Lingarr',
    description: 'Documentation for Lingarr, the subtitle translation service.',
    themeConfig: {
        outline: { level: [2, 3] },
        nav: [
            { text: 'Home', link: '/' },
            { text: 'Getting Started', link: '/getting-started/installation' },
            { text: 'Plugins', link: '/developers/plugins' }
        ],
        sidebar: [
            {
                text: 'Getting Started',
                items: [
                    { text: 'Installation', link: '/getting-started/installation' },
                    { text: 'Configuration', link: '/getting-started/configuration' }
                ]
            },
            {
                text: 'Translation Services',
                items: [
                    { text: 'Overview', link: '/translation-services/' },
                    { text: 'AI Services', link: '/translation-services/ai-services' },
                    { text: 'Machine Translation', link: '/translation-services/machine-translation' }
                ]
            },
            {
                text: 'Developers',
                items: [{ text: 'Plugin API', link: '/developers/plugins' }]
            }
        ],
        socialLinks: [{ icon: 'github', link: 'https://github.com/lingarr-translate/lingarr' }],
        search: {
            provider: 'local'
        }
    }
})
