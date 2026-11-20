<template>
    <CardComponent title="AI Request Body Template">
        <template #description>
            Customize the JSON structure sent to your AI provider. Placeholders are replaced across
            the whole template when performing translations and can be used in any field.
        </template>
        <template #content>
            <div class="space-y-4">
                <div class="flex flex-wrap items-center justify-between gap-3">
                    <div class="flex flex-wrap items-center gap-2">
                        <span
                            class="text-secondary-content/50 mr-1 text-xs font-medium tracking-wider uppercase">
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
                    <div class="border-accent/30 inline-flex overflow-hidden rounded-md border">
                        <button
                            class="cursor-pointer px-3 py-1 text-xs font-medium transition-colors duration-200"
                            :class="
                                mode === 'builder'
                                    ? 'bg-accent text-accent-content'
                                    : 'text-secondary-content/70 hover:bg-accent/10 hover:text-accent-content'
                            "
                            @click="mode = 'builder'">
                            Builder
                        </button>
                        <button
                            class="border-accent/30 cursor-pointer border-l px-3 py-1 text-xs font-medium transition-colors duration-200"
                            :class="
                                mode === 'json'
                                    ? 'bg-accent text-accent-content'
                                    : 'text-secondary-content/70 hover:bg-accent/10 hover:text-accent-content'
                            "
                            @click="mode = 'json'">
                            JSON
                        </button>
                    </div>
                </div>

                <div class="flex items-center gap-2">
                    <span class="text-sm font-semibold">Language format:</span>
                    <ToggleButton v-model="languageCodeFormat">
                        <span class="text-primary-content text-sm font-medium">
                            {{ languageCodeFormat === 'true' ? 'Code (en)' : 'Name (English)' }}
                        </span>
                    </ToggleButton>
                </div>

                <div class="grid grid-cols-1 gap-4 xl:grid-cols-2">
                    <div>
                        <Transition name="fade" mode="out-in">
                            <JsonTreeBuilder
                                v-if="mode === 'builder'"
                                key="builder"
                                :model-value="templateValue"
                                :placeholders="placeholderItems"
                                @update:model-value="onTemplateChange" />
                            <TextAreaComponent
                                v-else
                                key="json"
                                v-model="jsonModel"
                                :rows="16"
                                :placeholders="placeholderItems" />
                        </Transition>
                    </div>
                    <JsonPreview
                        :template="templateValue"
                        :service-type="serviceType"
                        :service-id="serviceId" />
                </div>

                <SaveNotification ref="saveNotification" />
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { PLACEHOLDER, REQUEST_TEMPLATE_KEYS, SETTINGS, SERVICE_TYPE } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useTranslationServiceStore } from '@/store/translationService'
import useDebounce from '@/composables/useDebounce'
import services from '@/services'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import JsonTreeBuilder from './JsonTreeBuilder.vue'
import JsonPreview from './JsonPreview.vue'

const settingsStore = useSettingStore()
const translationServiceStore = useTranslationServiceStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const mode = ref<'builder' | 'json'>('builder')
const jsonText = ref('')
const selectedPreset = ref('')

const placeholderItems = [
    PLACEHOLDER.MODEL,
    PLACEHOLDER.SYSTEM_PROMPT,
    PLACEHOLDER.USER_MESSAGE,
    PLACEHOLDER.SOURCE_LANGUAGE,
    PLACEHOLDER.TARGET_LANGUAGE,
    PLACEHOLDER.LINE_TO_TRANSLATE,
    PLACEHOLDER.CONTEXT_BEFORE,
    PLACEHOLDER.CONTEXT_AFTER
]

const props = defineProps<{ serviceId: number }>()

const serviceType = computed(
    () => translationServiceStore.getService(props.serviceId)?.provider ?? ''
)

const languageCodeFormat = computed({
    get: (): string =>
        (settingsStore.getSetting(SETTINGS.LANGUAGE_CODE_FORMAT) as string) ?? 'false',
    set: (value: string) => {
        settingsStore.updateSetting(SETTINGS.LANGUAGE_CODE_FORMAT, value, true)
        saveNotification.value?.show()
    }
})

const localAiEndpoint = computed(
    () => translationServiceStore.getSetting(props.serviceId, 'local_ai_endpoint') ?? ''
)
const isLocalAiGenerate = computed(
    () => !localAiEndpoint.value.trimEnd().replace(/\/$/, '').endsWith('completions')
)

const activeTemplateKey = computed((): string => {
    const keys = REQUEST_TEMPLATE_KEYS[serviceType.value]
    if (!keys) {
        return ''
    }
    if (serviceType.value === SERVICE_TYPE.LOCALAI && isLocalAiGenerate.value) {
        return keys.generateKey ?? keys.key
    }
    return keys.key
})

const templateValue = computed(() => {
    if (!activeTemplateKey.value) {
        return '{}'
    }
    return translationServiceStore.getSetting(props.serviceId, activeTemplateKey.value) || '{}'
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

const debouncedSave = useDebounce((key: string, value: string) => {
    translationServiceStore.updateSetting(props.serviceId, key, value, true)
    saveNotification.value?.show()
}, 500)

function onTemplateChange(value: string) {
    const key = activeTemplateKey.value
    if (!key) {
        return
    }
    debouncedSave(key, value)
}

function onJsonInput(value: string) {
    jsonText.value = value
    const key = activeTemplateKey.value
    if (!key) {
        return
    }
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
    deepseek_request_template: 'DeepSeek Chat',
    mistral_request_template: 'Mistral Chat',
    xai_request_template: 'xAI Chat'
}

onMounted(async () => {
    try {
        const defaults = await services.requestTemplate.getDefaults<Record<string, string>>()
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
    translationServiceStore.updateSetting(props.serviceId, key, template, true)
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

watch(
    () => props.serviceId,
    (id) => {
        translationServiceStore.loadSettings(id)
    },
    { immediate: true }
)

watch(serviceType, () => {
    selectedPreset.value = ''
})
</script>
