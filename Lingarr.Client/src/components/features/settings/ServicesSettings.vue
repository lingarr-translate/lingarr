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

            <SourceAndTarget @save="saveNotification?.show()" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
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
</script>
