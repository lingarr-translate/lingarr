<template>
    <CardComponent :title="translate('settings.prompt.title')">
        <template #description>
            {{ translate('settings.prompt.description') }}
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.prompt.promptTitle') }}
                    </span>
                    {{ translate('settings.prompt.promptDescription') }}
                </div>
                <AiSystemPrompt @save="saveNotification?.show()" />

                <div>
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">
                            {{ translate('settings.prompt.contextPromptToggle') }}
                        </span>
                    </div>
                    <ToggleButton v-model="aiContextPromptEnabled">
                        <span class="text-primary-content text-sm font-medium">
                            {{
                                aiContextPromptEnabled == 'true'
                                    ? translate('common.enabled')
                                    : translate('common.disabled')
                            }}
                        </span>
                    </ToggleButton>
                    <div v-if="aiContextPromptEnabled == 'true'" class="flex flex-col space-x-2">
                        <span class="font-semibold">
                            {{ translate('settings.prompt.contextPromptTitle') }}
                        </span>
                        {{ translate('settings.prompt.contextPromptDescription') }}
                    </div>
                    <div v-if="aiContextPromptEnabled == 'true' && useBatchTranslation == 'true'" class="text-xs text-info mt-2">
                        {{ translate('settings.prompt.batchContextNote') }}
                    </div>
                    <AiContextPrompt
                        v-if="aiContextPromptEnabled == 'true'"
                        @save="saveNotification?.show()" />

                    <InputComponent
                        v-if="aiContextPromptEnabled == 'true'"
                        v-model="contextBefore"
                        type="number"
                        validation-type="number"
                        :label="translate('settings.prompt.contextBefore')"
                        @update:validation="(val) => (isValid.contextBefore = val)" />
                    <InputComponent
                        v-if="aiContextPromptEnabled == 'true'"
                        v-model="contextAfter"
                        type="number"
                        validation-type="number"
                        :label="translate('settings.prompt.contextAfter')"
                        @update:validation="(val) => (isValid.contextAfter = val)" />
                </div>

                <!-- Batch Context Instruction Section -->
                <div v-if="useBatchTranslation == 'true'" class="mt-6 border-t border-base-300 pt-4">
                    <div class="flex flex-col space-y-2">
                        <span class="font-semibold">
                            {{ translate('settings.prompt.batchContextInstructionTitle') }}
                        </span>
                        <p class="text-sm text-base-content/70">
                            {{ translate('settings.prompt.batchContextInstructionDescription') }}
                        </p>
                    </div>

                    <!-- CRITICAL Warning -->
                    <div class="alert alert-error mt-3">
                        <svg xmlns="http://www.w3.org/2000/svg" class="stroke-current shrink-0 h-6 w-6" fill="none" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                        <span class="text-sm">{{ translate('settings.prompt.batchContextInstructionWarning') }}</span>
                    </div>

                    <!-- Toggle for editing -->
                    <div class="form-control mt-3">
                        <label class="label cursor-pointer justify-start gap-3">
                            <input 
                                type="checkbox" 
                                v-model="batchContextInstructionEditable" 
                                class="checkbox checkbox-warning" />
                            <span class="label-text">
                                {{ translate('settings.prompt.batchContextInstructionAdvancedToggle') }}
                            </span>
                        </label>
                    </div>

                    <!-- Instruction textarea -->
                    <div class="mt-3">
                        <textarea
                            v-model="batchContextInstruction"
                            :disabled="!batchContextInstructionEditable"
                            class="textarea textarea-bordered w-full h-32 font-mono text-sm"
                            :class="{ 'opacity-60 cursor-not-allowed': !batchContextInstructionEditable }"
                        ></textarea>
                    </div>

                    <!-- Reset to default button -->
                    <div v-if="batchContextInstructionEditable" class="mt-2">
                        <button 
                            class="btn btn-outline btn-sm"
                            @click="resetBatchContextInstruction">
                            {{ translate('settings.prompt.batchContextInstructionResetDefault') }}
                        </button>
                    </div>
                </div>
            </div>
        </template>
    </CardComponent>
</template>
<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import AiSystemPrompt from '@/components/features/settings/services/AiSystemPrompt.vue'
import AiContextPrompt from '@/components/features/settings/services/AiContextPrompt.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import InputComponent from '@/components/common/InputComponent.vue'

// Default batch context instruction - MUST match the backend default
const DEFAULT_BATCH_CONTEXT_INSTRUCTION = `IMPORTANT: Some items in the batch are marked with "isContextOnly": true. These are provided ONLY for context to help you understand the conversation flow. Do NOT translate or include context-only items in your output. Only translate and return items where "isContextOnly" is false or not present.`

const settingsStore = useSettingStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const isValid = reactive({
    contextBefore: true,
    contextAfter: true
})

const useBatchTranslation = computed(
    () => settingsStore.getSetting(SETTINGS.USE_BATCH_TRANSLATION) as string
)

const aiContextPromptEnabled = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.AI_CONTEXT_PROMPT_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_PROMPT_ENABLED, newValue, true)
        saveNotification.value?.show()
    }
})

const contextBefore = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_CONTEXT_BEFORE) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_BEFORE, newValue, isValid.contextBefore)
        if (isValid.contextBefore) {
            saveNotification.value?.show()
        }
    }
})

const contextAfter = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_CONTEXT_AFTER) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_AFTER, newValue, isValid.contextAfter)
        if (isValid.contextAfter) {
            saveNotification.value?.show()
        }
    }
})

// Batch context instruction editable toggle (local state, not persisted)
const batchContextInstructionEditable = ref(false)

// Batch context instruction
const batchContextInstruction = computed({
    get: () => {
        const stored = settingsStore.getSetting(SETTINGS.AI_BATCH_CONTEXT_INSTRUCTION) as string
        return stored || DEFAULT_BATCH_CONTEXT_INSTRUCTION
    },
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_BATCH_CONTEXT_INSTRUCTION, newValue, true)
        saveNotification.value?.show()
    }
})

// Reset to default function
function resetBatchContextInstruction() {
    batchContextInstruction.value = DEFAULT_BATCH_CONTEXT_INSTRUCTION
    saveNotification.value?.show()
}
</script>
