<template>
    <CardComponent :title="translate('settings.integrations.title')">
        <template #description>
            {{ translate('settings.integrations.description') }}
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">
                    {{ translate('settings.integrations.radarrHeader') }}
                </span>
                <InputComponent
                    v-model="radarrUrl"
                    validation-type="url"
                    :label="translate('settings.integrations.radarrAddress')"
                    :error-message="translate('settings.integrations.radarrAddressError')"
                    @update:validation="(val) => (isValid.radarrUrl = val)" />
                <InputComponent
                    v-model="radarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    validation-type="string"
                    type="password"
                    :label="translate('settings.integrations.radarrApiKey')"
                    :error-message="translate('settings.integrations.radarrApiKeyError')"
                    @update:validation="(val) => (isValid.radarrApiKey = val)" />
            </div>
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">
                    {{ translate('settings.integrations.sonarrHeader') }}
                </span>
                <InputComponent
                    v-model="sonarrUrl"
                    validation-type="url"
                    :label="translate('settings.integrations.sonarrAddress')"
                    :error-message="translate('settings.integrations.sonarrAddressError')"
                    @update:validation="(val) => (isValid.sonarrUrl = val)" />
                <InputComponent
                    v-model="sonarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    validation-type="string"
                    type="password"
                    :label="translate('settings.integrations.sonarrApiKey')"
                    :error-message="translate('settings.integrations.sonarrApiKeyError')"
                    @update:validation="(val) => (isValid.sonarrApiKey = val)" />
            </div>
            <div v-translate="'settings.integrations.reindexTask'" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import SaveNotification from '@/components/common/SaveNotification.vue'
import { SETTINGS } from '@/ts'
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
    get: (): string => settingsStore.getSetting(SETTINGS.RADARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RADARR_API_KEY, newValue, isValid.radarrApiKey)
        if (isValid.radarrApiKey) {
            saveNotification.value?.show()
        }
    }
})
const sonarrApiKey = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SONARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SONARR_API_KEY, newValue, isValid.sonarrApiKey)
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
