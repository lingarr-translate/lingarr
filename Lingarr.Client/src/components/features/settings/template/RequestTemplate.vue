<template>
    <CardComponent title="AI Request Body Template">
        <template #description>
            Customize the JSON structure sent to your AI provider. Use placeholders {model},
            {systemPrompt}, and {userMessage} which will be replaced when performing translations.
        </template>
        <template #content>
            <div class="space-y-4">
                <div class="flex flex-wrap items-center gap-3 justify-between">
                    <div class="flex flex-wrap items-center gap-2">
                        <span class="text-secondary-content/50 mr-1 text-xs font-medium uppercase tracking-wider">
                            Presets
                        </span>
                        <ButtonComponent
                            v-for="preset in presetOptions"
                            :key="preset.value"
                            :variant="selectedPreset === preset.value ? 'secondary' : 'ghost'"
                            size="xs"
                            @click="loadPreset(preset.value)">
                            {{ preset.label }}
                        </ButtonComponent>
                    </div>
                    <div
                        class="border-accent/30 inline-flex overflow-hidden rounded-md border">
                        <button
                            class="cursor-pointer px-3 py-1 text-xs font-medium transition-colors duration-200"
                            :class="mode === 'builder'
                                ? 'bg-accent text-accent-content'
                                : 'text-secondary-content/70 hover:bg-accent/10 hover:text-accent-content'"
                            @click="mode = 'builder'">
                            Builder
                        </button>
                        <button
                            class="border-accent/30 cursor-pointer border-l px-3 py-1 text-xs font-medium transition-colors duration-200"
                            :class="mode === 'json'
                                ? 'bg-accent text-accent-content'
                                : 'text-secondary-content/70 hover:bg-accent/10 hover:text-accent-content'"
                            @click="mode = 'json'">
                            JSON
                        </button>
                    </div>
                </div>

                <div class="grid grid-cols-1 gap-4 xl:grid-cols-2">
                    <div>
                        <Transition name="fade" mode="out-in">
                            <JsonTreeBuilder
                                v-if="mode === 'builder'"
                                key="builder"
                                :model-value="templateValue"
                                @update:model-value="onTemplateChange" />
                            <TextAreaComponent
                                v-else
                                key="json"
                                v-model="jsonModel"
                                :rows="16"
                                :placeholders="placeholderItems" />
                        </Transition>
                    </div>
                    <JsonPreview :template="templateValue" :service-type="serviceType" />
                </div>

                <SaveNotification ref="saveNotification" />
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { SETTINGS, SERVICE_TYPE, ISettings } from '@/ts'
import { useSettingStore } from '@/store/setting'
import useDebounce from '@/composables/useDebounce'
import services from '@/services'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'
import JsonTreeBuilder from './JsonTreeBuilder.vue'
import JsonPreview from './JsonPreview.vue'

const settingsStore = useSettingStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const mode = ref<'builder' | 'json'>('builder')
const jsonText = ref('')
const selectedPreset = ref('')

const placeholderItems = [
    {
        placeholder: '{model}',
        placeholderText: 'insert {model}',
        title: 'Model',
        description: 'The AI model identifier configured for the selected service'
    },
    {
        placeholder: '{systemPrompt}',
        placeholderText: 'insert {systemPrompt}',
        title: 'System Prompt',
        description: 'The system prompt with source and target language instructions'
    },
    {
        placeholder: '{userMessage}',
        placeholderText: 'insert {userMessage}',
        title: 'User Message',
        description: 'The subtitle text that needs to be translated'
    }
]

const serviceType = computed(() => (settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string) ?? '')

const templateMap: Record<string, keyof ISettings> = {
    [SERVICE_TYPE.OPENAI]: SETTINGS.OPENAI_REQUEST_TEMPLATE,
    [SERVICE_TYPE.ANTHROPIC]: SETTINGS.ANTHROPIC_REQUEST_TEMPLATE,
    [SERVICE_TYPE.LOCALAI]: SETTINGS.LOCAL_AI_CHAT_REQUEST_TEMPLATE,
    [SERVICE_TYPE.GEMINI]: SETTINGS.GEMINI_REQUEST_TEMPLATE,
    [SERVICE_TYPE.DEEPSEEK]: SETTINGS.DEEPSEEK_REQUEST_TEMPLATE
}

const localAiEndpoint = computed(
    () => settingsStore.getSetting(SETTINGS.LOCAL_AI_ENDPOINT) as string
)
const isLocalAiGenerate = computed(() => {
    const endpoint = localAiEndpoint.value || ''
    return !endpoint.trimEnd().replace(/\/$/, '').endsWith('completions')
})

const activeTemplateKey = computed((): keyof ISettings | '' => {
    if (serviceType.value === SERVICE_TYPE.LOCALAI) {
        return isLocalAiGenerate.value
            ? SETTINGS.LOCAL_AI_GENERATE_REQUEST_TEMPLATE
            : SETTINGS.LOCAL_AI_CHAT_REQUEST_TEMPLATE
    }
    return templateMap[serviceType.value] || ''
})

const templateValue = computed(() => {
    if (!activeTemplateKey.value) return '{}'
    return (settingsStore.getSetting(activeTemplateKey.value) as string) || '{}'
})

const prettyTemplate = computed(() => {
    try {
        return JSON.stringify(JSON.parse(templateValue.value), null, 2)
    } catch {
        return templateValue.value
    }
})

const jsonModel = computed({
    get: () => jsonText.value || prettyTemplate.value,
    set: (value: string) => onJsonInput(value)
})

const debouncedSave = useDebounce((key: keyof ISettings, value: string) => {
    settingsStore.updateSetting(key, value, true)
    saveNotification.value?.show()
}, 500)

function onTemplateChange(value: string) {
    const key = activeTemplateKey.value
    if (!key) return
    debouncedSave(key, value)
}

function onJsonInput(value: string) {
    jsonText.value = value
    const key = activeTemplateKey.value
    if (!key) return
    try {
        const parsed = JSON.parse(value)
        const compact = JSON.stringify(parsed)
        debouncedSave(key, compact)
    } catch {
        // Don't save invalid JSON (user could be still typing)
    }
}

const presets = ref<Record<string, string>>({})
const presetOptions = ref<{ value: string; label: string }[]>([])

const presetLabelMap: Record<string, string> = {
    openai_request_template: 'OpenAI Chat',
    anthropic_request_template: 'Anthropic Messages',
    gemini_request_template: 'Gemini Content',
    local_ai_generate_request_template: 'Ollama Generate',
    local_ai_chat_request_template: 'LocalAI Chat',
    deepseek_request_template: 'DeepSeek Chat'
}

onMounted(async () => {
    try {
        const defaults =
            await services.requestTemplate.getDefaults<Record<string, string>>()
        presets.value = defaults
        presetOptions.value = Object.keys(defaults).map((key) => ({
            value: key,
            label: presetLabelMap[key] || key
        }))
    } catch {
        // Ignore failed default retrieval
    }
})

function loadPreset(presetKey: string) {
    const template = presets.value[presetKey]
    const key = activeTemplateKey.value
    if (!template || !key) {
        return
    }
    selectedPreset.value = presetKey
    settingsStore.updateSetting(key, template, true)
    saveNotification.value?.show()
    if (mode.value === 'json') {
        syncJsonText()
    }
}

function syncJsonText() {
    jsonText.value = prettyTemplate.value
}

watch(mode, (newMode) => {
    if (newMode === 'json') {
        syncJsonText()
    }
})

watch(activeTemplateKey, () => {
    syncJsonText()
})

watch(serviceType, () => {
    selectedPreset.value = ''
})
</script>
