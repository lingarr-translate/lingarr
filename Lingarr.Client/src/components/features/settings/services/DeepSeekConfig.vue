<template>
    <div class="flex flex-col space-y-2">
        <div>
            {{ translate('settings.services.deepseekWarningIntro') }}
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{
                    automationEnabled == 'true'
                        ? translate('settings.services.deepseekEnabled')
                        : translate('settings.services.deepseekDisabled')
                }}
            </span>
        </div>
        <p class="text-xs">
            {{ translate('settings.services.deepseekDescription') }}
        </p>

        <label class="mb-1 block text-sm">
            {{ translate('settings.services.deepseekAiModel') }}
        </label>
        <SelectComponent v-model:selected="aiModel" :options="options" />

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            :label="translate('settings.services.deepseekApiKey')"
            :min-length="1"
            :error-message="translate('settings.services.deepseekError')"
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
    { label: 'Chat', value: 'deepseek-chat' },
    { label: 'Reasoner', value: 'deepseek-reasoner' }
]
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const apiKeyIsValid = ref(false)

const automationEnabled = computed(() => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED))

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.DEEPSEEK_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.DEEPSEEK_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.DEEPSEEK_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.DEEPSEEK_API_KEY, newValue, apiKeyIsValid.value)
        if (apiKeyIsValid.value) {
            emit('save')
        }
    }
})
</script>
