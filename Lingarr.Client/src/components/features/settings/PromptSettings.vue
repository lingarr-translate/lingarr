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
