import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { useLanguageStore } from '@/store/language'
import router from './router'
import App from './App.vue'

import '@/assets/style.css'
import './utils'

const pinia = createPinia()
const app = createApp(App)

new Promise((resolve) => resolve(true))
    .then(() => {
        app.use(pinia)
        app.use(router)
    })
    .then(async () => {
        await useLanguageStore().setLanguages()
    })
    .finally(() => {
        app.mount('#app')
    })
