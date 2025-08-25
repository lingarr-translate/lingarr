import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import * as path from 'path'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ command, mode }) => {
    const isProduction = mode === 'production'
    const env = loadEnv(mode, process.cwd(), "VITE_");
    const baseServerURL = env.VITE_BASE_SERVER_URL || "Lingarr.Server:9876";

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
                    target: `http://${baseServerURL}`,
                    changeOrigin: true
                },
                '/signalr': {
                    target: `http://${baseServerURL}`,
                    ws: true,
                    changeOrigin: true
                }
            },
            watch: {
                usePolling: true
            },
            host: '0.0.0.0',
            strictPort: true,
            port: env.VITE_PORT || 9876
        }
    }
})
