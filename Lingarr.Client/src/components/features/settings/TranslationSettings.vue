<template>
    <CardComponent title="Translation Request">
        <template #description>
            Modify translation request settings by changing retry options or batch size if available in the service.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />

            <template
                v-if="
                [
                    SERVICE_TYPE.ANTHROPIC,
                    SERVICE_TYPE.DEEPSEEK,
                    SERVICE_TYPE.GEMINI,
                    SERVICE_TYPE.LOCALAI,
                    SERVICE_TYPE.OPENAI
                ].includes(
                    serviceType as 'openai' | 'anthropic' | 'localai' | 'gemini' | 'deepseek'
                )
            ">
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Use batch translation</span>
                    Process multiple subtitle lines together in batches to improve translation
                    efficiency and context awareness. Note that single-line translations with context
                    are still more reliable and of higher quality.
                </div>
                <ToggleButton v-model="useBatchTranslation">
                    <span class="text-sm font-medium text-primary-content">
                        {{ useBatchTranslation == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>
            </template>
            <InputComponent
                v-if="useBatchTranslation == 'true'"
                v-model="maxBatchSize"
                :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                @update:validation="(val) => (isValid.maxBatchSize = val)" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">Max translation retries:</span>
                Maximum number of retries per line or batch.
            </div>
            <InputComponent
                v-model="maxRetries"
                :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                @update:validation="(val) => (isValid.maxRetries = val)" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">Delay between retries:</span>
                Initial delay before retrying, in seconds.
            </div>
            <InputComponent
                v-model="retryDelay"
                :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                @update:validation="(val) => (isValid.retryDelay = val)" />

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">Retry delay multiplier:</span>
                Factor by which the delay increases after each retry.
            </div>
            <InputComponent
                v-model="retryDelayMultiplier"
                :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                @update:validation="(val) => (isValid.retryDelayMultiplier = val)" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { INPUT_VALIDATION_TYPE, SERVICE_TYPE, SETTINGS } from '@/ts'
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
const serviceType = computed(() => settingsStore.getSetting(SETTINGS.SERVICE_TYPE))

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
