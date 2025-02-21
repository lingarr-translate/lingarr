<template>
    <div class="flex flex-col space-y-2">
        <div>
            {{ translate('settings.services.geminiWarningIntro') }}
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{
                    automationEnabled == 'true'
                        ? translate('settings.services.geminiEnabled')
                        : translate('settings.services.geminiDisabled')
                }}
            </span>
        </div>
        <p class="text-xs">
            {{ translate('settings.services.geminiDescription') }}
        </p>

        <label class="mb-1 block text-sm">
            {{ translate('settings.services.geminiAiModel') }}
        </label>
        <SelectComponent v-model:selected="aiModel" :options="options" />

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            :label="translate('settings.services.geminiApiKey')"
            :min-length="1"
            :error-message="translate('settings.services.geminiError')"
            @update:validation="(val) => (apiKeyIsValid = val)" />

        <AiPromptConfig @save="emit('save')" />
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import SelectComponent from '@/components/common/SelectComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import AiPromptConfig from '@/components/features/settings/services/AiPromptConfig.vue'

const options = [
    { label: 'Gemini 2.0 Flash', value: 'gemini-2.0-flash' },
    { label: 'Gemini 2.0 Flash Lite', value: 'gemini-2.0-flash-lite-preview-02-05' },
    { label: 'Gemini 1.5 Flash', value: 'gemini-1.5-flash' },
    { label: 'Gemini 1.5 Flash 8B', value: 'gemini-1.5-flash-8b' },
    { label: 'Gemini 1.5 Pro', value: 'gemini-1.5-pro' }
]
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const apiKeyIsValid = ref(false)

const automationEnabled = computed(() => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED))

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.GEMINI_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.GEMINI_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.GEMINI_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.GEMINI_API_KEY, newValue, apiKeyIsValid.value)
        if (apiKeyIsValid.value) {
            emit('save')
        }
    }
})
</script>
