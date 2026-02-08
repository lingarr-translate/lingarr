<template>
    <TextAreaComponent
        v-model="aiPrompt"
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
            }
        ]"
        :required-placeholders="['{sourceLanguage}', '{targetLanguage}']"
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
