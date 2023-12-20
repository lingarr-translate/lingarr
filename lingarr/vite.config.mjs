import {defineConfig} from 'vite'
import vue from '@vitejs/plugin-vue'
import * as path from 'path'

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src')
        }
    },
    server: {
        proxy: {
            "/api": {
                target: "http://lingarr_api:9876",
                changeOrigin: true,
                secure: false
            },
        },
        watch: {
            usePolling: true
        },
        host: '0.0.0.0',
        strictPort: true,
        port: 9877
    }
})
