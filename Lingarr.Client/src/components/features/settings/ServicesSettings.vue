<template>
    <CardComponent :title="translate('settings.services.title')">
        <template #description>
            {{ translate('settings.services.description') }}
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">
                    {{ translate('settings.services.serviceSelect') }}
                </span>
                <SelectComponent v-model:selected="serviceType" :options="serviceOptions" />
                <component
                    :is="serviceConfigComponent"
                    v-if="serviceConfigComponent"
                    @save="saveNotification?.show()" />
            </div>
            <div
                v-if="
                    [
                        SERVICE_TYPE.ANTHROPIC,
                        SERVICE_TYPE.GEMINI,
                        SERVICE_TYPE.LOCALAI,
                        SERVICE_TYPE.OPENAI
                    ].includes(serviceType as 'openai' | 'anthropic' | 'localai' | 'gemini')
                "
                class="flex flex-col space-y-4">
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.subtitle.useBatchTranslation') }}
                    </span>
                    {{ translate('settings.subtitle.useBatchTranslationDescription') }}
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
                    :label="translate('settings.subtitle.maxBatchSize')"
                    @update:validation="(val) => (isValid.maxBatchSize = val)" />
            </div>

            <SourceAndTarget @save="saveNotification?.show()" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS, SERVICE_TYPE } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import LibreTranslateConfig from '@/components/features/settings/services/LibreTranslateConfig.vue'
import DeepLConfig from '@/components/features/settings/services/DeepLConfig.vue'
import FreeServiceConfig from '@/components/features/settings/services/FreeServiceConfig.vue'
import AnthropicConfig from '@/components/features/settings/services/AnthropicConfig.vue'
import OpenAiConfig from '@/components/features/settings/services/OpenAiConfig.vue'
import LocalAiConfig from '@/components/features/settings/services/LocalAiConfig.vue'
import GeminiConfig from '@/components/features/settings/services/GeminiConfig.vue'
import DeepSeekConfig from '@/components/features/settings/services/DeepSeekConfig.vue'
import SourceAndTarget from '@/components/features/settings/SourceAndTarget.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const isValid = reactive({
    maxBatchSize: true
})

const serviceType = computed({
    get: () => settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.SERVICE_TYPE, newValue, true)
        saveNotification.value?.show()
    }
})

const serviceOptions = [
    { value: SERVICE_TYPE.ANTHROPIC, label: 'Anthropic' },
    { value: SERVICE_TYPE.BING, label: 'Bing' },
    { value: SERVICE_TYPE.DEEPL, label: 'DeepL' },
    { value: SERVICE_TYPE.DEEPSEEK, label: 'DeepSeek' },
    { value: SERVICE_TYPE.GEMINI, label: 'Gemini' },
    { value: SERVICE_TYPE.GOOGLE, label: 'Google' },
    { value: SERVICE_TYPE.LIBRETRANSLATE, label: 'LibreTranslate' },
    { value: SERVICE_TYPE.LOCALAI, label: 'Local AI (Custom)' },
    { value: SERVICE_TYPE.MICROSOFT, label: 'Microsoft' },
    { value: SERVICE_TYPE.OPENAI, label: 'OpenAI' },
    { value: SERVICE_TYPE.YANDEX, label: 'Yandex' }
]

const serviceConfigComponent = computed(() => {
    switch (serviceType.value) {
        case SERVICE_TYPE.LIBRETRANSLATE:
            return LibreTranslateConfig
        case SERVICE_TYPE.OPENAI:
            return OpenAiConfig
        case SERVICE_TYPE.ANTHROPIC:
            return AnthropicConfig
        case SERVICE_TYPE.LOCALAI:
            return LocalAiConfig
        case SERVICE_TYPE.DEEPL:
            return DeepLConfig
        case SERVICE_TYPE.GEMINI:
            return GeminiConfig
        case SERVICE_TYPE.DEEPSEEK:
            return DeepSeekConfig
        case SERVICE_TYPE.GOOGLE:
        case SERVICE_TYPE.BING:
        case SERVICE_TYPE.MICROSOFT:
        case SERVICE_TYPE.YANDEX:
            return FreeServiceConfig
        default:
            return null
    }
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
        settingsStore.updateSetting(SETTINGS.MAX_BATCH_SIZE, newValue, true)
        saveNotification.value?.show()
    }
})
</script>
