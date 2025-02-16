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
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
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

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const serviceType = computed({
    get: () => settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.SERVICE_TYPE, newValue, true)
        saveNotification.value?.show()
    }
})

const serviceOptions = [
    { value: 'anthropic', label: 'Anthropic' },
    { value: 'bing', label: 'Bing' },
    { value: 'deepl', label: 'DeepL' },
    { value: 'deepseek', label: 'DeepSeek' },
    { value: 'gemini', label: 'Gemini' },
    { value: 'google', label: 'Google' },
    { value: 'libretranslate', label: 'LibreTranslate' },
    { value: 'localai', label: 'Local AI' },
    { value: 'microsoft', label: 'Microsoft' },
    { value: 'openai', label: 'OpenAI' },
    { value: 'yandex', label: 'Yandex' }
]

const serviceConfigComponent = computed(() => {
    switch (serviceType.value) {
        case 'libretranslate':
            return LibreTranslateConfig
        case 'openai':
            return OpenAiConfig
        case 'anthropic':
            return AnthropicConfig
        case 'localai':
            return LocalAiConfig
        case 'deepl':
            return DeepLConfig
        case 'gemini':
            return GeminiConfig
        case 'deepseek':
            return DeepSeekConfig
        case 'google':
        case 'bing':
        case 'microsoft':
        case 'yandex':
            return FreeServiceConfig
        default:
            return null
    }
})
</script>
