<template>
    <CardComponent title="Translation Request">
        <template #description>
            Modify translation request settings by changing retry options, batch size, or fallback behavior if available in the service.
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

            <template v-if="useBatchTranslation == 'true'">
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Batch size:</span>
                    Amount of subtitle lines in a single batch.
                </div>
                <InputComponent
                    v-model="maxBatchSize"
                    :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                    @update:validation="(val) => (isValid.maxBatchSize = val)" />
            </template>

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">Request timeout:</span>
                Maximum time in minutes to wait for a translation response before the request is
                cancelled. Increase this if you run a slow local AI on minimal hardware for example.
            </div>
            <InputComponent
                v-model="requestTimeout"
                :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                @update:validation="(val) => (isValid.requestTimeout = val)" />

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

            <div class="flex flex-col space-x-2">
                <span class="font-semibold">Fallback translation service:</span>
                If the primary provider still fails after retries, Lingarr will try this provider
                instead. Leave it disabled if you do not want a fallback.
            </div>
            <SelectComponent
                v-model:selected="fallbackServiceType"
                :options="fallbackServiceOptions"
                placeholder="Disabled" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive, watch } from 'vue'
import { useSettingStore } from '@/store/setting'
import { INPUT_VALIDATION_TYPE, SERVICE_TYPE, SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import SelectComponent, { type ISelectOption } from '@/components/common/SelectComponent.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const isValid = reactive({
    maxBatchSize: true,
    requestTimeout: true,
    maxRetries: true,
    retryDelay: true,
    retryDelayMultiplier: true
})
const serviceType = computed(() => settingsStore.getSetting(SETTINGS.SERVICE_TYPE))

const serviceOptions: ISelectOption[] = [
    { value: SERVICE_TYPE.ANTHROPIC, label: 'Anthropic' },
    { value: SERVICE_TYPE.BING, label: 'Bing' },
    { value: SERVICE_TYPE.DEEPL, label: 'DeepL' },
    { value: SERVICE_TYPE.DEEPSEEK, label: 'DeepSeek' },
    { value: SERVICE_TYPE.GEMINI, label: 'Gemini' },
    { value: SERVICE_TYPE.GOOGLE, label: 'Google' },
    { value: SERVICE_TYPE.LIBRETRANSLATE, label: 'LibreTranslate' },
    { value: SERVICE_TYPE.LOCALAI, label: 'OpenAI-compatible API (Custom)' },
    { value: SERVICE_TYPE.MICROSOFT, label: 'Microsoft' },
    { value: SERVICE_TYPE.OPENAI, label: 'OpenAI' },
    { value: SERVICE_TYPE.YANDEX, label: 'Yandex' }
]

const fallbackServiceOptions = computed<ISelectOption[]>(() => [
    { value: '', label: 'Disabled' },
    ...serviceOptions.filter((option) => option.value !== serviceType.value)
])

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

const requestTimeout = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.REQUEST_TIMEOUT) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.REQUEST_TIMEOUT, newValue, isValid.requestTimeout)
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

const fallbackServiceType = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.FALLBACK_SERVICE_TYPE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.FALLBACK_SERVICE_TYPE, newValue, true)
        saveNotification.value?.show()
    }
})

watch(serviceType, (newServiceType) => {
    if (fallbackServiceType.value && fallbackServiceType.value === newServiceType) {
        fallbackServiceType.value = ''
    }
})
</script>