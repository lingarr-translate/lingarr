<template>
    <CardComponent title="Automation">
        <template #description>
            Set up automation. Note that if automation is implemented, you also need to configure
            the necessary
            <a
                class="cursor-pointer underline"
                @click="router.push({ name: 'integration-settings' })">
                services
            </a>
            .
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">Indexing schedule:</span>
                <SelectComponent
                    v-model:selected="movieSchedule"
                    label="Set movie indexer:"
                    :options="cronOptions" />
                <SelectComponent
                    v-model:selected="showSchedule"
                    label="Set show indexer:"
                    :options="cronOptions" />
            </div>
            <div class="flex flex-col space-y-2 pb-4">
                <span class="font-semibold">Automated translation:</span>
                <ToggleButton v-model="automationEnabled" />
            </div>
            <div class="flex flex-col space-y-2 pb-4">
                <SelectComponent
                    v-model:selected="translationSchedule"
                    label="Set translation schedule:"
                    :options="cronOptions" />
            </div>
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Limits:</span>
                <InputComponent
                    v-model="maxTranslationsPerRun"
                    input-type="number"
                    validation-type="number"
                    :min-length="0"
                    label="Limit the amount of translations per schedule" />
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

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const router = useRouter()

const automationEnabled: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.AUTOMATION_ENABLED, newValue)
        saveNotification.value?.show()
    }
})
const movieSchedule: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MOVIE_SCHEDULE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MOVIE_SCHEDULE, newValue)
        saveNotification.value?.show()
    }
})
const showSchedule: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SHOW_SCHEDULE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SHOW_SCHEDULE, newValue)
        saveNotification.value?.show()
    }
})
const translationSchedule: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.TRANSLATION_SCHEDULE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.TRANSLATION_SCHEDULE, newValue)
        saveNotification.value?.show()
    }
})
const maxTranslationsPerRun: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MAX_TRANSLATIONS_PER_RUN) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MAX_TRANSLATIONS_PER_RUN, newValue)
        saveNotification.value?.show()
    }
})

const cronOptions = [
    {
        value: '*/15 * * * *',
        label: 'Every 15 minutes'
    },
    {
        value: '*/30 * * * *',
        label: 'Every 30 minutes'
    },
    {
        value: '0 * * * *',
        label: 'Hourly'
    },
    {
        value: '0 */2 * * *',
        label: 'Every 2 hours'
    },
    {
        value: '0 */4 * * *',
        label: 'Every 4 hours'
    },
    {
        value: '0 */6 * * *',
        label: 'Every 6 hours'
    },
    {
        value: '0 */12 * * *',
        label: 'Twice daily (Every 12 hours)'
    },
    {
        value: '0 0 * * *',
        label: 'Daily at midnight'
    },
    {
        value: '0 4 * * *',
        label: 'Daily at 04:00'
    },
    {
        value: '0 0 * * SUN',
        label: 'Weekly on Sunday at midnight'
    }
]
</script>
