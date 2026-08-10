<template>
    <CardComponent title="Proofread Prompt">
        <template #description>
            Define the prompts used when proofreading an already completed translation.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Proofread system prompt</span>
                    Define the AI's behavior when comparing a translated line against its source
                    line.
                </div>
                <TextAreaComponent
                    v-model="proofreadPrompt"
                    :rows="10"
                    :min-height="100"
                    :placeholders="[PLACEHOLDER.SOURCE_LANGUAGE, PLACEHOLDER.TARGET_LANGUAGE]"
                    :required-placeholders="['{sourceLanguage}', '{targetLanguage}']"
                    @update:validation="(val) => (isSystemPromptValid = val)" />

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Proofread user prompt</span>
                    Sent together with the source line and its existing translation for each
                    proofread request.
                </div>
                <TextAreaComponent
                    v-model="proofreadUserPrompt"
                    :rows="6"
                    :min-height="60"
                    :placeholders="[PLACEHOLDER.SOURCE_LINE, PLACEHOLDER.TRANSLATED_LINE]"
                    :required-placeholders="['{sourceLine}', '{translatedLine}']"
                    @update:validation="(val) => (isUserPromptValid = val)" />
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { PLACEHOLDER, SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'

const settingsStore = useSettingStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const isSystemPromptValid = ref(false)
const isUserPromptValid = ref(false)

const proofreadPrompt = computed({
    get: () => (settingsStore.getSetting(SETTINGS.PROOFREAD_PROMPT) as string) ?? '',
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.PROOFREAD_PROMPT, newValue, isSystemPromptValid.value)
        if (isSystemPromptValid.value) {
            saveNotification.value?.show()
        }
    }
})

const proofreadUserPrompt = computed({
    get: () => (settingsStore.getSetting(SETTINGS.PROOFREAD_USER_PROMPT) as string) ?? '',
    set: (newValue: string) => {
        settingsStore.updateSetting(
            SETTINGS.PROOFREAD_USER_PROMPT,
            newValue,
            isUserPromptValid.value
        )
        if (isUserPromptValid.value) {
            saveNotification.value?.show()
        }
    }
})
</script>
