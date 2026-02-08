import { createApp } from 'vue'
import { createPinia } from 'pinia'

import router from '@/router'
import App from './App.vue'

import { highlight, showTitle } from '@/directives'
import '@/assets/style.css'
import './utils'

const pinia = createPinia()
const app = createApp(App)

new Promise((resolve) => resolve(true))
    .then(() => {
        app.directive('highlight', highlight)
        app.directive('show-title', showTitle)
    })
    .then(() => {
        app.use(pinia)
        app.use(router)
    })
    .finally(() => {
        app.mount('#app')
    })
