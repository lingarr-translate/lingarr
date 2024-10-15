<template>
    <div class="flex flex-col space-y-2">
        <p class="text-xs">
            Local AI addresses usually consist of the following path
            <span class="rounded-md bg-primary p-1">/v1/chat/completions</span>
            and should follow the
            <a
                href="https://platform.openai.com/docs/api-reference/chat/create"
                class="underline"
                target="_blank">
                Open AI
            </a>
            API specification.
        </p>

        <InputComponent v-model="address" validation-type="url" label="Address" />

        <InputComponent v-model="aiModel" validation-type="string" label="Model" />

        <InputComponent v-model="apiKey" validation-type="string" label="API key" type="password" />
        <p class="text-xs">API key is optional and can be left empty.</p>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import InputComponent from '@/components/common/InputComponent.vue'

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.LOCAL_AI_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.LOCAL_AI_MODEL, newValue)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.LOCAL_AI_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.LOCAL_AI_API_KEY, newValue)
        emit('save')
    }
})

const address = computed({
    get: () => settingsStore.getSetting(SETTINGS.LOCAL_AI_ENDPOINT) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.LOCAL_AI_ENDPOINT, newValue)
        emit('save')
    }
})
</script>
