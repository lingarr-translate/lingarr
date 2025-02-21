<template>
    <div class="flex flex-col space-y-2">
        <div>
            {{ translate('settings.services.anthropicWarningIntro') }}
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{
                    automationEnabled == 'true'
                        ? translate('settings.services.anthropicEnabled')
                        : translate('settings.services.anthropicDisabled')
                }}
            </span>
        </div>
        <p class="text-xs">
            {{ translate('settings.services.anthropicDescription') }}
        </p>

        <label class="mb-1 block text-sm">
            {{ translate('settings.services.anthropicAiModel') }}
        </label>
        <SelectComponent v-model:selected="aiModel" :options="options" />

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            :label="translate('settings.services.anthropicApiKey')"
            :min-length="1"
            :error-message="translate('settings.services.anthropicApiKeyError')"
            @update:validation="(val) => (isValid.apiKey = val)" />

        <InputComponent
            v-model="version"
            validation-type="string"
            :label="translate('settings.services.anthropicVersion')"
            :min-length="1"
            :error-message="translate('settings.services.anthropicError')"
            @update:validation="(val) => (isValid.version = val)" />

        <AiPromptConfig @save="emit('save')" />
    </div>
</template>

<script setup lang="ts">
import { computed, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import SelectComponent from '@/components/common/SelectComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import AiPromptConfig from '@/components/features/settings/services/AiPromptConfig.vue'

const options = [{ label: 'Claude 3.5 Sonnet', value: 'claude-3-5-sonnet-20240620' }]
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const isValid = reactive({
    apiKey: false,
    version: false
})

const automationEnabled = computed(
    () => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED) as string
)

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_API_KEY, newValue, isValid.apiKey)
        if (isValid.apiKey) {
            emit('save')
        }
    }
})

const version = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_VERSION) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_VERSION, newValue, isValid.version)
        if (isValid.version) {
            emit('save')
        }
    }
})
</script>
