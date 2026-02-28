<template>
    <CardComponent title="Integrations">
        <template #description>Configure the settings for Radarr and Sonarr integrations.</template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Radarr Settings:</span>
                <InputComponent
                    v-model="radarrUrl"
                    :validation-type="INPUT_VALIDATION_TYPE.URL"
                    label="Address"
                    error-message="Please enter a valid URL (e.g., http://localhost:7878 or https://example.com)"
                    @update:validation="(val) => (isValid.radarrUrl = val)" />
                <InputComponent
                    v-model="radarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    :validation-type="INPUT_VALIDATION_TYPE.STRING"
                    :type="INPUT_TYPE.PASSWORD"
                    label="API key"
                    error-message="API Key must be {minLength} characters"
                    @update:validation="(val) => (isValid.radarrApiKey = val)" />
            </div>
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Sonarr Settings:</span>
                <InputComponent
                    v-model="sonarrUrl"
                    :validation-type="INPUT_VALIDATION_TYPE.URL"
                    label="Address"
                    error-message="Please enter a valid URL (e.g., http://localhost:8989 or https://example.com)"
                    @update:validation="(val) => (isValid.sonarrUrl = val)" />
                <InputComponent
                    v-model="sonarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    :validation-type="INPUT_VALIDATION_TYPE.STRING"
                    :type="INPUT_TYPE.PASSWORD"
                    label="API key"
                    error-message="API Key must be {minLength} characters"
                    @update:validation="(val) => (isValid.sonarrApiKey = val)" />
            </div>
            <div>
                No media visible? Try reindexing by starting a sync task
                <a href="/settings/tasks" class="underline">here</a>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import SaveNotification from '@/components/common/SaveNotification.vue'
import { INPUT_TYPE, INPUT_VALIDATION_TYPE, SETTINGS, ENCRYPTED_SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const isValid = reactive({
    radarrUrl: false,
    radarrApiKey: false,
    sonarrUrl: false,
    sonarrApiKey: false
})
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const radarrApiKey = computed({
    get: (): string => settingsStore.getEncryptedSetting(ENCRYPTED_SETTINGS.RADARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateEncryptedSetting(ENCRYPTED_SETTINGS.RADARR_API_KEY, newValue, isValid.radarrApiKey)
        if (isValid.radarrApiKey) {
            saveNotification.value?.show()
        }
    }
})
const sonarrApiKey = computed({
    get: (): string => settingsStore.getEncryptedSetting(ENCRYPTED_SETTINGS.SONARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateEncryptedSetting(ENCRYPTED_SETTINGS.SONARR_API_KEY, newValue, isValid.sonarrApiKey)
        if (isValid.sonarrApiKey) {
            saveNotification.value?.show()
        }
    }
})
const radarrUrl = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RADARR_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RADARR_URL, newValue, isValid.radarrUrl)
        if (isValid.radarrUrl) {
            saveNotification.value?.show()
        }
    }
})
const sonarrUrl = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SONARR_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SONARR_URL, newValue, isValid.sonarrUrl)
        if (isValid.sonarrUrl) {
            saveNotification.value?.show()
        }
    }
})
</script>
