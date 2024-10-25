<template>
    <router-view></router-view>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useSignalR } from '@/composables/useSignalR'
import { ISettings } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useTranslationRequestStore } from '@/store/translationRequest'

const translationRequestStore = useTranslationRequestStore()
const settingStore = useSettingStore()
const signalR = useSignalR()

onMounted(async () => {
    const settingUpdated = await signalR.connect('SettingUpdates', '/signalr/SettingUpdates')
    await settingUpdated.joinGroup({ group: 'SettingUpdates' })
    settingUpdated.on('SettingUpdate', (setting: { key: keyof ISettings; value: string }) => {
        settingStore.storeSetting(setting.key, setting.value)
    })

    const translationRequest = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequests'
    )
    await translationRequest.joinGroup({ group: 'TranslationRequests' })
    translationRequest.on('RequestActive', (request: { count: number }) => {
        translationRequestStore.setActiveCount(request.count)
    })
})

onUnmounted(async () => {
    const translationRequest = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequest'
    )
    translationRequest.off('SettingUpdate', () => {})
    translationRequest.off('RequestActive', () => {})
})
</script>
