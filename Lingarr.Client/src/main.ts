import { createApp } from 'vue'
import { createPinia } from 'pinia'

import { useInstanceStore } from '@/store/instance'
import { useSettingStore } from '@/store/setting'
import { useScheduleStore } from '@/store/schedule'

import router from '@/router'
import App from './App.vue'

import '@/assets/style.css'
import './utils'
import { createSignalRPlugin } from '@/plugins/signalR'

const pinia = createPinia()
const app = createApp(App)

new Promise((resolve) => resolve(true))
    .then(() => {
        app.use(pinia)
        app.use(router)
        app.use(createSignalRPlugin({ url: '/hub/ScheduleProgress' }))
    })
    .then(async () => {
        await useSettingStore().applySettingsOnLoad()
        await useInstanceStore().applyVersionOnLoad()
        await useInstanceStore().applyThemeOnLoad()
        useScheduleStore().loadRunningJobs()
    })
    .finally(() => {
        app.mount('#app')
    })
