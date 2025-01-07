<template>
    <CardComponent :title="translate('settings.integrations.title')">
        <template #description>
            {{ translate('settings.integrations.description') }}
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2 pb-4">
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
            <div class="flex items-center gap-2 pb-4">
                <ButtonComponent
                    :class="indexingMovies ? 'text-primary-content/50' : 'text-primary-content'"
                    @click="indexMovies()">
                    {{ translate('settings.integrations.radarrSyncButton') }}
                </ButtonComponent>
                <div v-if="indexingMovies" class="inline-flex overflow-hidden text-green-500">
                    {{ translate('settings.integrations.radarrUpdating') }}
                    <span class="animate-ellipsis">.</span>
                    <span class="animate-ellipsis animation-delay-300">.</span>
                    <span class="animate-ellipsis animation-delay-600">.</span>
                </div>
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
            <div class="flex items-center gap-2 pt-4">
                <ButtonComponent
                    :class="indexingShows ? 'text-primary-content/50' : 'text-primary-content'"
                    @click="indexShows()">
                    {{ translate('settings.integrations.sonarrSyncButton') }}
                </ButtonComponent>
                <div v-if="indexingShows" class="inline-flex overflow-hidden text-green-500">
                    {{ translate('settings.integrations.sonarrUpdating') }}
                    <span class="animate-ellipsis">.</span>
                    <span class="animate-ellipsis animation-delay-300">.</span>
                    <span class="animate-ellipsis animation-delay-600">.</span>
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { WritableComputedRef, computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { useScheduleStore } from '@/store/schedule'
import SaveNotification from '@/components/common/SaveNotification.vue'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import { delay } from '@/utils/delay'

const isValid = reactive({
    radarrUrl: false,
    radarrApiKey: false,
    sonarrUrl: false,
    sonarrApiKey: false
})
const indexingShows = ref(false)
const indexingMovies = ref(false)
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const scheduleStore = useScheduleStore()

const radarrApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RADARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RADARR_API_KEY, newValue, isValid.radarrApiKey)
        if (isValid.radarrApiKey) {
            saveNotification.value?.show()
        }
    }
})
const sonarrApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SONARR_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SONARR_API_KEY, newValue, isValid.sonarrApiKey)
        if (isValid.sonarrApiKey) {
            saveNotification.value?.show()
        }
    }
})
const radarrUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RADARR_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RADARR_URL, newValue, isValid.radarrUrl)
        if (isValid.radarrUrl) {
            saveNotification.value?.show()
        }
    }
})
const sonarrUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SONARR_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SONARR_URL, newValue, isValid.sonarrUrl)
        if (isValid.sonarrUrl) {
            saveNotification.value?.show()
        }
    }
})

async function indexShows() {
    if (indexingShows.value) return
    indexingShows.value = true
    await scheduleStore.indexShows()
    await delay(5000)
    indexingShows.value = false
}

async function indexMovies() {
    if (indexingMovies.value) return
    indexingMovies.value = true
    await scheduleStore.indexMovies()
    await delay(5000)
    indexingMovies.value = false
}
</script>
