<template>
    <CardComponent :title="translate('settings.translation.title')">
        <template #description>
            {{ translate('settings.translation.description') }}
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">
                    {{ translate('settings.translation.useBatchTranslation') }}
                </span>
                {{ translate('settings.translation.useBatchTranslationDescription') }}
            </div>
            <ToggleButton v-model="useBatchTranslation">
                <span class="text-primary-content text-sm font-medium">
                    {{
                        useBatchTranslation == 'true'
                            ? translate('common.enabled')
                            : translate('common.disabled')
                    }}
                </span>
            </ToggleButton>
            <InputComponent
                v-if="useBatchTranslation == 'true'"
                v-model="maxBatchSize"
                validation-type="number"
                @update:validation="(val) => (isValid.maxBatchSize = val)" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">
                    {{ translate('settings.translation.maxRetries') }}
                </span>
                {{ translate('settings.translation.maxRetriesDescription') }}
            </div>
            <InputComponent
                v-model="maxRetries"
                validation-type="number"
                @update:validation="(val) => (isValid.maxRetries = val)" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">
                    {{ translate('settings.translation.retryDelay') }}
                </span>
                {{ translate('settings.translation.retryDelayDescription') }}
            </div>
            <InputComponent
                v-model="retryDelay"
                validation-type="number"
                @update:validation="(val) => (isValid.retryDelay = val)" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">
                    {{ translate('settings.translation.retryDelayMultiplier') }}
                </span>
                {{ translate('settings.translation.retryDelayMultiplierDescription') }}
            </div>
            <InputComponent
                v-model="retryDelayMultiplier"
                validation-type="number"
                @update:validation="(val) => (isValid.retryDelayMultiplier = val)" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const isValid = reactive({
    maxBatchSize: true,
    maxRetries: true,
    retryDelay: true,
    retryDelayMultiplier: true
})

const useBatchTranslation = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.USE_BATCH_TRANSLATION) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.USE_BATCH_TRANSLATION, newValue, true)
        saveNotification.value?.show()
    }
})

const maxBatchSize = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MAX_BATCH_SIZE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MAX_BATCH_SIZE, newValue, isValid.maxBatchSize)
        saveNotification.value?.show()
    }
})

const maxRetries = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.MAX_RETRIES) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.MAX_RETRIES, newValue, isValid.maxRetries)
        saveNotification.value?.show()
    }
})

const retryDelay = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RETRY_DELAY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.RETRY_DELAY, newValue, isValid.retryDelay)
        saveNotification.value?.show()
    }
})

const retryDelayMultiplier = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.RETRY_DELAY_MULTIPLIER) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(
            SETTINGS.RETRY_DELAY_MULTIPLIER,
            newValue,
            isValid.retryDelayMultiplier
        )
        saveNotification.value?.show()
    }
})
</script>
