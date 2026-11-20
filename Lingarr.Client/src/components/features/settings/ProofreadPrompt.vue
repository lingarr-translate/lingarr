<template>
    <CardComponent title="Proofread Prompt">
        <template #description>
            Define the prompts used when proofreading an already completed translation.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Own proofread prompts for this service</span>
                    When disabled, this service uses the global proofread prompts.
                </div>
                <ToggleButton v-model="hasOwnPrompts">
                    <span class="text-primary-content text-sm font-medium">
                        {{ hasOwnPrompts == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>

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
import { useTranslationServiceStore } from '@/store/translationService'
import { PLACEHOLDER, SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'

const props = defineProps<{
    serviceId: number
}>()

const translationServiceStore = useTranslationServiceStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const isSystemPromptValid = ref(false)
const isUserPromptValid = ref(false)

const hasOwnPrompts = computed({
    get: (): string =>
        translationServiceStore.getHasOwnPrompt(props.serviceId, SETTINGS.PROOFREAD_PROMPT)
            ? 'true'
            : 'false',
    set: async (newValue: string): Promise<void> => {
        await translationServiceStore.setOwnPrompts(
            props.serviceId,
            [SETTINGS.PROOFREAD_PROMPT, SETTINGS.PROOFREAD_USER_PROMPT],
            newValue === 'true'
        )
        saveNotification.value?.show()
    }
})

const proofreadPrompt = computed({
    get: () => translationServiceStore.getPrompt(props.serviceId, SETTINGS.PROOFREAD_PROMPT),
    set: (newValue: string) => {
        translationServiceStore.updatePrompt(
            props.serviceId,
            SETTINGS.PROOFREAD_PROMPT,
            newValue,
            isSystemPromptValid.value
        )
        if (isSystemPromptValid.value) {
            saveNotification.value?.show()
        }
    }
})

const proofreadUserPrompt = computed({
    get: () => translationServiceStore.getPrompt(props.serviceId, SETTINGS.PROOFREAD_USER_PROMPT),
    set: (newValue: string) => {
        translationServiceStore.updatePrompt(
            props.serviceId,
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
