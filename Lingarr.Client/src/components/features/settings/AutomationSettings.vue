<template>
    <CardComponent :title="translate('settings.indexing.title')">
        <template #description>
            {{ translate('settings.indexing.description') }}
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">
                    {{ translate('settings.indexing.indexingMoviesLabel') }}
                </span>
                <InputComponent
                    v-model="movieSchedule"
                    label="Cron format: minute hour day month weekday (e.g., '0 * * * *' for hourly)"
                    :placeholder="'0 * * * *'"
                    validation-type="cron"
                    @update:validation="(val) => (movieScheduleIsValid = val)" />
                <span class="font-semibold">
                    {{ translate('settings.indexing.indexingMoviesLabel') }}
                </span>
                <InputComponent
                    v-model="showSchedule"
                    label="Cron format: minute hour day month weekday (e.g., '0 * * * *' for hourly)"
                    :placeholder="'0 * * * *'"
                    validation-type="cron"
                    @update:validation="(val) => (showScheduleIsValid = val)" />
            </div>
        </template>
    </CardComponent>

    <CardComponent :title="translate('settings.automation.title')">
        <template #description>
            {{ translate('settings.automation.description') }}

            <a class="cursor-pointer underline" @click="router.push({ name: 'services-settings' })">
                {{ translate('settings.automation.descriptionLink') }}
            </a>
            .
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <div class="flex items-center space-x-2">
                    <span>{{ translate('settings.automation.enableAutomatedTranslation') }}</span>
                    <ToggleButton v-model="automationEnabled">
                        <span class="text-primary-content text-sm font-medium">
                            {{
                                automationEnabled === 'true'
                                    ? translate('common.enabled')
                                    : translate('common.disabled')
                            }}
                        </span>
                    </ToggleButton>
                </div>

                <span class="font-semibold">
                    {{ translate('settings.automation.translationScheduleLabel') }}
                </span>
                <InputComponent
                    v-model="translationSchedule"
                    label="Cron format: minute hour day month weekday (e.g., '0 * * * *' for hourly)"
                    :placeholder="'0 * * * *'"
                    validation-type="cron"
                    @update:validation="(val) => (translationScheduleIsValid = val)" />

                <span class="font-semibold">
                    {{ translate('settings.automation.limitsHeader') }}
                </span>
                <InputComponent
                    v-model="maxTranslationsPerRun"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    :label="translate('settings.automation.scheduleLimitLabel')"
                    @update:validation="(val) => (maxTranslationsPerRunIsValid = val)" />

                <span class="font-semibold">
                    {{ translate('settings.automation.defaultAgeThresholdLabel') }}
                </span>
                <InputComponent
                    v-model="movieAgeThreshold"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    :label="translate('settings.automation.movieAgeThresholdLabel')"
                    @update:validation="(val) => (movieAgeThresholdIsValid = val)" />
                <InputComponent
                    v-model="showAgeThreshold"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    :label="translate('settings.automation.showAgeThresholdLabel')"
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
import { useI18n } from '@/plugins/i18n'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const maxTranslationsPerRunIsValid = ref(false)
const movieAgeThresholdIsValid = ref(false)
const showAgeThresholdIsValid = ref(false)
const movieScheduleIsValid = ref(false)
const showScheduleIsValid = ref(false)
const translationScheduleIsValid = ref(false)
const settingsStore = useSettingStore()
const router = useRouter()
const { translate } = useI18n()

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
