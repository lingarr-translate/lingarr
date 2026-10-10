import { createApp } from 'vue'
import { createPinia } from 'pinia'

import router from '@/router'
import App from './App.vue'

import { highlight, showTitle } from '@/directives'
import tooltip from '@/directives/tooltip'
import '@/assets/style.css'
import './utils'

const pinia = createPinia()
const app = createApp(App)

new Promise((resolve) => resolve(true))
    .then(() => {
        app.directive('highlight', highlight)
        app.directive('show-title', showTitle)
        app.directive('tooltip', tooltip)
    })
    .then(() => {
        app.use(pinia)
        app.use(router)
    })
    .finally(() => {
        app.mount('#app')
    })
