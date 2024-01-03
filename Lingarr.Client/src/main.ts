import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { useLanguageStore } from '@/store/language'
import { useInstanceStore } from '@/store/instance'
import { useResourceStore } from '@/store/resource'
import router from '@/router'
import App from './App.vue'

import '@/assets/style.css'
import './utils'

const pinia = createPinia()
const app = createApp(App)

router.beforeEach(async (to) => {
    if (to?.name && ['tvshows', 'movies'].includes(to.name.toString())) {
        useResourceStore().setMediaType(to.name.toString())
        useResourceStore().setResource()
    }
})

new Promise((resolve) => resolve(true))
    .then(() => {
        app.use(pinia)
        app.use(router)
    })
    .then(async () => {
        await useLanguageStore().setLanguages()
        await useInstanceStore().applyThemeOnLoad()
    })
    .finally(() => {
        app.mount('#app')
    })
