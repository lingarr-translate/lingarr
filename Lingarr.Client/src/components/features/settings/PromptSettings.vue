<template>
    <CardComponent title="System & Context Prompts">
        <template #description>
            Define custom instructions (system prompt) and an optional context prompt to control AI response logic.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        System prompt
                    </span>
                    Define the AI’s behavior and tone by setting global instructions.
                </div>
                <AiSystemPrompt @save="saveNotification?.show()" />

                <div v-if="useBatchTranslation == 'false'" class="space-y-4">
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">
                            Enable context prompt:
                        </span>
                    </div>
                    <ToggleButton v-model="aiContextPromptEnabled">
                        <span class="text-primary-content text-sm font-medium">
                            {{ aiContextPromptEnabled == 'true' ? 'Enable' : 'Disabled' }}
                        </span>
                    </ToggleButton>
                    <div v-if="aiContextPromptEnabled == 'true'" class="flex flex-col space-x-2">
                        <span class="font-semibold">
                            Context prompt
                        </span>
                        Provide the surrounding context, including lines before and after the current subtitle, to help the AI generate a more accurate translation.
                    </div>
                    <AiContextPrompt
                        v-if="aiContextPromptEnabled == 'true'"
                        @save="saveNotification?.show()" />

                    <InputComponent
                        v-if="aiContextPromptEnabled == 'true'"
                        v-model="contextBefore"
                        type="number"
                        validation-type="number"
                        label="Context before"
                        @update:validation="(val) => (isValid.contextBefore = val)" />
                    <InputComponent
                        v-if="aiContextPromptEnabled == 'true'"
                        v-model="contextAfter"
                        type="number"
                        validation-type="number"
                        label="Context after"
                        @update:validation="(val) => (isValid.contextAfter = val)" />
                </div>
                <div v-else class="text-xs">
                    Context prompt is not supported and has been disabled when sending subtitles in batch.
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
</script>
