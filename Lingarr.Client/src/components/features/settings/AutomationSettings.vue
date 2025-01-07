<template>
    <CardComponent :title="translate('settings.automation.title')">
        <template #description>
            {{ translate('settings.automation.description') }}

            <a class="cursor-pointer underline" @click="router.push({ name: 'services-settings' })">
                {{ translate('settings.automation.descriptionLink') }}
            </a>
            .
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">
                    {{ translate('settings.automation.indexingHeader') }}
                </span>
                <SelectComponent
                    v-model:selected="movieSchedule"
                    :label="translate('settings.automation.indexingMoviesLabel')"
                    :options="cronOptions" />
                <SelectComponent
                    v-model:selected="showSchedule"
                    :label="translate('settings.automation.indexingTvShowLabel')"
                    :options="cronOptions" />
            </div>
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">
                    {{ translate('settings.automation.automatedTranslationHeader') }}
                </span>
                <ToggleButton v-model="automationEnabled" />
            </div>
            <div class="flex flex-col space-y-2 pb-4">
                <SelectComponent
                    v-model:selected="translationSchedule"
                    :label="translate('settings.automation.translationScheduleLabel')"
                    :options="cronOptions" />
            </div>
            <div class="flex flex-col space-y-2">
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
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { WritableComputedRef, computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { useRouter } from 'vue-router'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import { useI18n } from '@/plugins/i18n'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const maxTranslationsPerRunIsValid = ref(false)
const settingsStore = useSettingStore()
const router = useRouter()
const { translate } = useI18n()

const automationEnabled: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.AUTOMATION_ENABLED, newValue, true)
        saveNotification.value?.show()
    }
})
const movieSchedule: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MOVIE_SCHEDULE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MOVIE_SCHEDULE, newValue, true)
        saveNotification.value?.show()
    }
})
const showSchedule: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SHOW_SCHEDULE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SHOW_SCHEDULE, newValue, true)
        saveNotification.value?.show()
    }
})
const translationSchedule: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.TRANSLATION_SCHEDULE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.TRANSLATION_SCHEDULE, newValue, true)
        saveNotification.value?.show()
    }
})
const maxTranslationsPerRun: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MAX_TRANSLATIONS_PER_RUN) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MAX_TRANSLATIONS_PER_RUN, newValue, true)
        saveNotification.value?.show()
    }
})

const cronOptions = [
    {
        value: '*/15 * * * *',
        label: translate('settings.cronOptions.everyFifteenMinutes')
    },
    {
        value: '*/30 * * * *',
        label: translate('settings.cronOptions.everyThirtyMinutes')
    },
    {
        value: '0 * * * *',
        label: translate('settings.cronOptions.hourly')
    },
    {
        value: '0 */2 * * *',
        label: translate('settings.cronOptions.everyTwoHours')
    },
    {
        value: '0 */4 * * *',
        label: translate('settings.cronOptions.everyFourHours')
    },
    {
        value: '0 */6 * * *',
        label: translate('settings.cronOptions.everySixHours')
    },
    {
        value: '0 */12 * * *',
        label: translate('settings.cronOptions.twiceADay')
    },
    {
        value: '0 0 * * *',
        label: translate('settings.cronOptions.dailyAtMidnight')
    },
    {
        value: '0 4 * * *',
        label: translate('settings.cronOptions.dailyAtFour')
    },
    {
        value: '0 0 * * SUN',
        label: translate('settings.cronOptions.weeklyOnSundayAtMidnight')
    }
]
</script>
