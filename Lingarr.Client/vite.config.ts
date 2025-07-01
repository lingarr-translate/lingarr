import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import * as path from 'path'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ command, mode }) => {
    const isProduction = mode === 'production'

    return {
        esbuild: {
            drop: isProduction ? ['console'] : []
        },
        plugins: [vue(), tailwindcss()],
        resolve: {
            alias: {
                '@': path.resolve(__dirname, './src')
            }
        },
        server: {
            proxy: {
                '/api': {
                    target: 'http://Lingarr.Server:9876',
                    changeOrigin: true
                },
                '/signalr': {
                    target: 'http://Lingarr.Server:9876',
                    ws: true,
                    changeOrigin: true
                }
            },
            watch: {
                usePolling: true
            },
            host: '0.0.0.0',
            strictPort: true,
            port: 9876
        }
    }
})
