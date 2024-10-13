<template>
    <CardComponent title="Integrations">
        <template #description>Configure the settings for Radarr and Sonarr integrations.</template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">Radarr Settings:</span>
                <InputComponent
                    v-model="radarrUrl"
                    validation-type="url"
                    label="Address"
                    error-message="Please enter a valid URL (e.g., http://localhost:3000 or https://api.example.com)" />
                <InputComponent
                    v-model="radarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    validation-type="string"
                    label="API key"
                    error-message="API Key must be {minLength} characters" />
            </div>
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Sonarr Settings:</span>
                <InputComponent
                    v-model="sonarrUrl"
                    validation-type="url"
                    label="Address"
                    error-message="Please enter a valid Address (e.g., http://localhost:3000 or https://api.example.com)" />
                <InputComponent
                    v-model="sonarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    validation-type="string"
                    label="API key"
                    error-message="API Key must be {minLength} characters" />
            </div>
            <div class="flex items-center gap-2 pt-4">
                <ButtonComponent
                    :class="isReindexing ? 'text-primary-content/50' : 'text-primary-content'"
                    @click="reindex()">
                    Sync libraries
                </ButtonComponent>
                <div v-if="isReindexing" class="inline-flex overflow-hidden text-green-500">
                    updating
                    <span class="animate-ellipsis">.</span>
                    <span class="animate-ellipsis animation-delay-300">.</span>
                    <span class="animate-ellipsis animation-delay-600">.</span>
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { WritableComputedRef, computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import SaveNotification from '@/components/common/SaveNotification.vue'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import { useScheduleStore } from '@/store/schedule'

const isReindexing = ref(false)
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const scheduleStore = useScheduleStore()

const radarrApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RADARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RADARR_API_KEY, newValue)
        saveNotification.value?.show()
    }
})
const sonarrApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SONARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SONARR_API_KEY, newValue)
        saveNotification.value?.show()
    }
})
const radarrUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RADARR_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RADARR_URL, newValue)
        saveNotification.value?.show()
    }
})
const sonarrUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SONARR_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SONARR_URL, newValue)
        saveNotification.value?.show()
    }
})

async function reindex() {
    if (isReindexing.value) return
    isReindexing.value = true
    await scheduleStore.reindex()
    setTimeout(() => {
        isReindexing.value = false
    }, 5000)
}
</script>
