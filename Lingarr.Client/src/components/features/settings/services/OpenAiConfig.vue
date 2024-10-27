<template>
    <div class="flex flex-col space-y-2">
        <div>
            Automation is:
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{ automationEnabled == 'true' ? 'Enabled' : 'Disabled' }}
            </span>
        </div>
        <p class="text-xs">
            AI translation is very costly in terms of pricing. Only use it when you know what you
            are doing and make sure automation is disabled.
        </p>

        <label class="mb-1 block text-sm">AI Model</label>
        <SelectComponent
            v-model:selected="aiModel"
            :options="[
                { label: 'GPT-4o', value: 'gpt-4o' },
                { label: 'GPT-4o mini', value: 'gpt-4o-mini' },
                { label: 'GPT-4 Turbo 0125', value: 'gpt-3.5-turbo-0125' },
                { label: 'GPT-3.5 Turbo 0125', value: 'gpt-3.5-turbo-0125' }
            ]" />

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            label="API key"
            :min-length="1"
            error-message="API Key must not be empty"
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

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const apiKeyIsValid = ref(false)

const automationEnabled = computed(() => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED))

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.OPENAI_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.OPENAI_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.OPENAI_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.OPENAI_API_KEY, newValue, apiKeyIsValid.value)
        if (apiKeyIsValid.value) {
            emit('save')
        }
    }
})
</script>
