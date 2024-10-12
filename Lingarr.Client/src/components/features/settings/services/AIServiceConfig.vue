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
        <SelectComponent v-model:selected="aiModel" :options="modelOptions" />

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            label="API key"
            :min-length="1"
            error-message="API Key must not be empty" />

        <InputComponent
            v-if="serviceType === 'anthropic'"
            v-model="version"
            validation-type="string"
            label="Version"
            :min-length="1"
            error-message="Version must not be empty" />
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import SelectComponent from '@/components/common/SelectComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const serviceType = computed(() => settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string)
const automationEnabled = computed(
    () => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED) as string
)

const aiModel = computed({
    get: () =>
        settingsStore.getSetting(
            serviceType.value === 'openai' ? SETTINGS.OPENAI_MODEL : SETTINGS.ANTHROPIC_MODEL
        ) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            serviceType.value === 'openai' ? SETTINGS.OPENAI_MODEL : SETTINGS.ANTHROPIC_MODEL,
            newValue
        )
        emit('save')
    }
})

const apiKey = computed({
    get: () =>
        settingsStore.getSetting(
            serviceType.value === 'openai' ? SETTINGS.OPENAI_API_KEY : SETTINGS.ANTHROPIC_API_KEY
        ) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            serviceType.value === 'openai' ? SETTINGS.OPENAI_API_KEY : SETTINGS.ANTHROPIC_API_KEY,
            newValue
        )
        emit('save')
    }
})

const version = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_VERSION) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_VERSION, newValue)
        emit('save')
    }
})

const modelOptions = computed(() => {
    if (serviceType.value === 'openai') {
        return [
            { label: 'gpt-4o', value: 'GPT-4o' },
            { label: 'GPT-4o mini', value: 'gpt-4o-mini' },
            { label: 'GPT-4 Turbo 0125', value: 'gpt-3.5-turbo-0125' },
            { label: 'GPT-3.5 Turbo 0125', value: 'gpt-3.5-turbo-0125' }
        ]
    } else {
        return [{ label: 'claude-3-5-sonnet-20240620', value: 'Claude 3.5 Sonnet' }]
    }
})
</script>
