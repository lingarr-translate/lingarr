<template>
    <CardComponent title="Indexing">
        <template #description>
            The media indexing schedule controls the iteration with which Lingarr should sync with
            Sonarr and Radarr.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">Set movie indexer:</span>
                <InputComponent
                    v-model="movieSchedule"
                    label="Cron format: minute hour day month weekday (e.g., '0 * * * *' for hourly)"
                    :placeholder="'0 * * * *'"
                    validation-type="cron"
                    @update:validation="(val) => (movieScheduleIsValid = val)" />
                <span class="font-semibold">Set tv show indexer:</span>
                <InputComponent
                    v-model="showSchedule"
                    label="Cron format: minute hour day month weekday (e.g., '0 * * * *' for hourly)"
                    :placeholder="'0 * * * *'"
                    validation-type="cron"
                    @update:validation="(val) => (showScheduleIsValid = val)" />
            </div>
        </template>
    </CardComponent>

    <CardComponent title="Automation">
        <template #description>
            Set up automation. Note that if automation is implemented, you also need to configure
            the necessary
            <a class="cursor-pointer underline" @click="router.push({ name: 'services-settings' })">
                services
            </a>
            .
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <div class="flex items-center space-x-2">
                    <span>Automated translation:</span>
                    <ToggleButton v-model="automationEnabled">
                        <span class="text-sm font-medium text-primary-content">
                            {{ automationEnabled === 'true' ? 'Enabled' : 'Disabled' }}
                        </span>
                    </ToggleButton>
                </div>

                <span class="font-semibold">Set translation schedule:</span>
                <InputComponent
                    v-model="translationSchedule"
                    label="Cron format: minute hour day month weekday (e.g., '0 * * * *' for hourly)"
                    :placeholder="'0 * * * *'"
                    validation-type="cron"
                    @update:validation="(val) => (translationScheduleIsValid = val)" />

                <span class="font-semibold">Limits:</span>
                <InputComponent
                    v-model="maxTranslationsPerRun"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    label="Limit the amount of translations per schedule"
                    @update:validation="(val) => (maxTranslationsPerRunIsValid = val)" />

                <span class="font-semibold">Default file age delay for translation:</span>
                <InputComponent
                    v-model="movieAgeThreshold"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    label="Movie file age delay in 'hours'"
                    @update:validation="(val) => (movieAgeThresholdIsValid = val)" />
                <InputComponent
                    v-model="showAgeThreshold"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    label="TV Show file age delay in 'hours'"
                    @update:validation="(val) => (showAgeThresholdIsValid = val)" />
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { useRouter } from 'vue-router'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const maxTranslationsPerRunIsValid = ref(false)
const movieAgeThresholdIsValid = ref(false)
const showAgeThresholdIsValid = ref(false)
const movieScheduleIsValid = ref(false)
const showScheduleIsValid = ref(false)
const translationScheduleIsValid = ref(false)
const settingsStore = useSettingStore()
const router = useRouter()

const automationEnabled = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.AUTOMATION_ENABLED, newValue, true)
        saveNotification.value?.show()
    }
})
const movieSchedule = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MOVIE_SCHEDULE) as string,
    set: (newValue: string): void => {
        if (movieScheduleIsValid.value) {
            settingsStore.updateSetting(SETTINGS.MOVIE_SCHEDULE, newValue, true)
            saveNotification.value?.show()
        }
    }
})
const showSchedule = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SHOW_SCHEDULE) as string,
    set: (newValue: string): void => {
        if (showScheduleIsValid.value) {
            settingsStore.updateSetting(SETTINGS.SHOW_SCHEDULE, newValue, true)
            saveNotification.value?.show()
        }
    }
})
const translationSchedule = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.TRANSLATION_SCHEDULE) as string,
    set: (newValue: string): void => {
        if (translationScheduleIsValid.value) {
            settingsStore.updateSetting(SETTINGS.TRANSLATION_SCHEDULE, newValue, true)
            saveNotification.value?.show()
        }
    }
})
const maxTranslationsPerRun = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MAX_TRANSLATIONS_PER_RUN) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MAX_TRANSLATIONS_PER_RUN, newValue, true)
        saveNotification.value?.show()
    }
})

const movieAgeThreshold = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MOVIE_AGE_THRESHOLD) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MOVIE_AGE_THRESHOLD, newValue, true)
        saveNotification.value?.show()
    }
})

const showAgeThreshold = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SHOW_AGE_THRESHOLD) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SHOW_AGE_THRESHOLD, newValue, true)
        saveNotification.value?.show()
    }
})
</script>
