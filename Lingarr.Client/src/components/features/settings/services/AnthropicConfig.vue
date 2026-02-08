<template>
    <div class="flex flex-col space-y-2">
        <div>
            Automation is:
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{ automationEnabled == 'true' ? 'Enabled' : 'Disabled' }}
            </span>
        </div>
        <p class="text-xs">
            AI translation is very costly in terms of pricing. Only use it when you know what you are doing and make sure automation is disabled.
        </p>

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            label="API key"
            :min-length="1"
            error-message="API Key must not be empty"
            @update:validation="(val) => (isValid.apiKey = val)" />

        <InputComponent
            v-model="version"
            validation-type="string"
            label="Version"
            :min-length="1"
            error-message="Version must not be empty"
            @update:validation="(val) => (isValid.version = val)" />

        <label class="mb-1 block text-sm">
            AI Model
        </label>
        <SelectComponent
            ref="selectRef"
            v-model:selected="aiModel"
            :options="options"
            :load-on-open="true"
            placeholder="Select model..."
            no-options="errorMessage || 'Loading models...'"
            @fetch-options="loadOptions" />

        <p>
            Batch translation is available for this AI service. Configure batch settings
            <a class="cursor-pointer underline" @click="router.push({ name: 'subtitle-settings' })">
                here
            </a>
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import SelectComponent from '@/components/common/SelectComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import { useRouter } from 'vue-router'
import { useModelOptions } from '@/composables/useModelOptions'

const router = useRouter()
// @ts-ignore selectRef
const { options, errorMessage, selectRef, loadOptions } = useModelOptions()

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
