<template>
    <TextAreaComponent
        v-model="aiPrompt"
        :rows="10"
        :min-height="100"
        :label="translate('settings.services.aIPromptLabel')"
        :placeholders="[
            {
                placeholder: '{sourceLanguage}',
                placeholderText: translate('settings.prompt.insertPlaceholder').format({
                    placeholder: '{sourceLanguage}'
                }),
                title: translate('settings.prompt.placeholders.sourceLanguage.title'),
                description: translate('settings.prompt.placeholders.sourceLanguage.description'),
                required: true
            },
            {
                placeholder: '{targetLanguage}',
                placeholderText: translate('settings.prompt.insertPlaceholder').format({
                    placeholder: '{targetLanguage}'
                }),
                title: translate('settings.prompt.placeholders.targetLanguage.title'),
                description: translate('settings.prompt.placeholders.targetLanguage.description'),
                required: true
            },
            {
                placeholder: '{lineToTranslate}',
                placeholderText: translate('settings.prompt.insertPlaceholder').format({
                    placeholder: '{lineToTranslate}'
                }),
                title: translate('settings.prompt.placeholders.lineToTranslate.title'),
                description: translate('settings.prompt.placeholders.lineToTranslate.description'),
                required: false
            },
            {
                placeholder: '{contextBefore}',
                placeholderText: translate('settings.prompt.insertPlaceholder').format({
                    placeholder: '{contextBefore}'
                }),
                title: translate('settings.prompt.placeholders.contextBefore.title'),
                description: translate('settings.prompt.placeholders.contextBefore.description'),
                required: false
            },
            {
                placeholder: '{contextAfter}',
                placeholderText: translate('settings.prompt.insertPlaceholder').format({
                    placeholder: '{contextAfter}'
                }),
                title: translate('settings.prompt.placeholders.contextAfter.title'),
                description: translate('settings.prompt.placeholders.contextAfter.description'),
                required: false
            }
        ]"
        :required-placeholders="['{sourceLanguage}', '{targetLanguage}']"
        @update:validation="(val) => (isValid = val)" />
    <p class="text-xs">
        {{ translate('settings.services.aIPromptDescription') }}
    </p>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'

const isValid = ref(false)
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const aiPrompt = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_PROMPT) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_PROMPT, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>
