<template>
    <TextAreaComponent
        v-model="aiContextPrompt"
        :rows="10"
        :min-height="100"
        :placeholders="[
            {
                placeholder: '{sourceLanguage}',
                placeholderText: 'insert {sourceLanguage}',
                title: 'Source Language',
                description: 'The language the provided subtitle line is in',
                required: true
            },
            {
                placeholder: '{targetLanguage}',
                placeholderText: 'insert {targetLanguage}',
                title: 'Target Language',
                description: 'The language the provided subtitle line needs to be translated to',
                required: true
            },
            {
                placeholder: '{lineToTranslate}',
                placeholderText: 'insert {lineToTranslate}',
                title: 'Subtitle line',
                description: 'Subtitle line to translate',
                required: false
            },
            {
                placeholder: '{contextBefore}',
                placeholderText: 'insert {contextBefore}',
                title: 'Context',
                description:
                    'Subtitles before the provided subtitle line that can be used as context',
                required: false
            },
            {
                placeholder: '{contextAfter}',
                placeholderText: 'insert {contextAfter}',
                title: 'Context',
                description:
                    'Subtitles after the provided subtitle line that can be used as context',
                required: false
            }
        ]"
        @update:validation="(val) => (isValid = val)" />
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'

const isValid = ref(false)
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const aiContextPrompt = computed({
    get: () => settingsStore.getSetting(SETTINGS.AI_CONTEXT_PROMPT) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_CONTEXT_PROMPT, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>
