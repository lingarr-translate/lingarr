import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import * as path from 'path'

export default defineConfig({
    build: {
        terserOptions: {
            compress: {
                drop_console: true
            }
        }
    },
    plugins: [vue()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src')
        }
    },
    server: {
        proxy: {
            '/api': {
                target: 'http://Lingarr.Server:8080',
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
})
