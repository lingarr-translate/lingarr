<template>
    <div class="flex flex-col space-y-2">
        <div>
            Automation is:
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{ automationEnabled == 'true' ? 'Enabled' : 'Disabled' }}
            </span>
        </div>
        <p class="text-xs">
            OpenRouter gives you access to 100+ high-quality models (Claude, GPT, Gemini, Llama, Mistral, etc.)
            through a single API, often at competitive prices.
        </p>

        <InputComponent
            v-model="apiKey"
            :validation-type="INPUT_VALIDATION_TYPE.STRING"
            :type="INPUT_TYPE.PASSWORD"
            label="OpenRouter API Key"
            :min-length="1"
            error-message="API Key must not be empty"
            @update:validation="(val) => (apiKeyIsValid = val)" />

        <label class="mb-1 block text-sm">Model</label>
        <SelectComponent
            ref="selectRef"
            v-model:selected="aiModel"
            :options="options"
            :load-on-open="true"
            placeholder="Select OpenRouter model..."
            :no-options="errorMessage || 'Loading models...'"
            @fetch-options="loadOptions" />

        <p class="text-xs text-gray-500">
            Use the Refresh button in the model dropdown to fetch the latest models from OpenRouter.
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { INPUT_TYPE, INPUT_VALIDATION_TYPE, SETTINGS, ENCRYPTED_SETTINGS } from '@/ts'
import InputComponent from '@/components/common/InputComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import { useModelOptions } from '@/composables/useModelOptions'

// @ts-ignore selectRef
const { options, errorMessage, selectRef, loadOptions } = useModelOptions()

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const apiKeyIsValid = ref(false)

const automationEnabled = computed(() => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED))

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.OPENROUTER_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.OPENROUTER_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getEncryptedSetting(ENCRYPTED_SETTINGS.OPENROUTER_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateEncryptedSetting(ENCRYPTED_SETTINGS.OPENROUTER_API_KEY, newValue, apiKeyIsValid.value)
        if (apiKeyIsValid.value) {
            emit('save')
        }
    }
})
</script>
