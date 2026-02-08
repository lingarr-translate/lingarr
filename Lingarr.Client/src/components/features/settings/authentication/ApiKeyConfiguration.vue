<template>
    <CardComponent v-if="authEnabled == 'true'" title="API Key">
        <template #description></template>
        <template #content>
            <div class="flex flex-col space-y-2 pb-4">
            <div>
                <h2 class="mb-4 text-xl font-semibold text-white">{{ title }}</h2>
                <p class="mb-6 text-sm text-gray-400">
                    Click the button below to (re)generate your API key
                </p>
            </div>

            <div class="space-y-4">
                <ButtonComponent variant="primary" :loading="loading" @click="generateApiKey()">
                    {{ loading ? 'Generating...' : 'Generate API Key' }}
                </ButtonComponent>

                <div class="rounded-lg bg-gray-900 p-4">
                    <div class="mb-2 flex items-center justify-between">
                        <label class="text-sm font-medium text-gray-400">API Key</label>
                    </div>
                    <code class="block rounded bg-gray-800 p-3 font-mono text-sm break-all text-green-400">
                        {{ apiKey }}
                    </code>
                </div>

                <div class="rounded-lg border border-gray-700 bg-gray-900 p-6">
                    <h3 class="mb-3 text-lg font-semibold text-white">How to Use</h3>
                    <p class="mb-3 text-sm text-gray-400">
                        Include the API key in your HTTP requests using the
                        <code class="rounded bg-gray-800 px-2 py-1 text-green-400">X-Api-Key</code>
                        header:
                    </p>
                    <code class="block rounded bg-gray-800 p-3 font-mono text-xs break-all text-green-400">
                        curl -H "X-Api-Key: {{ apiKey }}" http://lingarr:9876/api/endpoint
                    </code>
                </div>
            </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import services from '@/services'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'

const settingsStore = useSettingStore()
const { title } = defineProps<{
    title?: string
}>()

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const loading = ref(false)

const authEnabled = computed({
    get: () => settingsStore.getSetting(SETTINGS.AUTH_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.AUTH_ENABLED, newValue.toString(), true)
        saveNotification.value?.show()
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.API_KEY, newValue.toString(), true)
        saveNotification.value?.show()
    }
})

const generateApiKey = async () => {
    const response = await services.auth.generateApiKey()
    await settingsStore.updateSetting(SETTINGS.API_KEY, response.apiKey, true)
}
</script>
