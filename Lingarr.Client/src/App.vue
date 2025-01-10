<template>
    <router-view></router-view>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useSignalR } from '@/composables/useSignalR'
import { Hub, ISettings } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useTranslationRequestStore } from '@/store/translationRequest'

const translationRequestStore = useTranslationRequestStore()
const settingStore = useSettingStore()
const signalR = useSignalR()
const settingHubConnection = ref<Hub>()
const requestHubConnection = ref<Hub>()

onMounted(async () => {
    settingHubConnection.value = await signalR.connect('SettingUpdates', '/signalr/SettingUpdates')
    await settingHubConnection.value.joinGroup({ group: 'SettingUpdates' })
    settingHubConnection.value.on(
        'SettingUpdate',
        (setting: { key: keyof ISettings; value: string }) => {
            settingStore.storeSetting(setting.key, setting.value)
        }
    )

    requestHubConnection.value = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequests'
    )
    await requestHubConnection.value.joinGroup({ group: 'TranslationRequests' })
    requestHubConnection.value.on('RequestActive', (request: { count: number }) => {
        translationRequestStore.setActiveCount(request.count)
    })
})

onUnmounted(async () => {
    settingHubConnection.value?.off('SettingUpdate', () => {})
    requestHubConnection.value?.off('RequestActive', () => {})
})
</script>
