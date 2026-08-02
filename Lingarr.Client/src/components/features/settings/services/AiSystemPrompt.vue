<template>
    <TextAreaComponent
        v-model="aiPrompt"
        :rows="10"
        :min-height="100"
        :placeholders="placeholderItems"
        :required-placeholders="['{sourceLanguage}', '{targetLanguage}']"
        @update:validation="(val) => (isValid = val)" />
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { PLACEHOLDER, SETTINGS } from '@/ts'
import TextAreaComponent from '@/components/common/TextAreaComponent.vue'

const isValid = ref(false)
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const placeholderItems = computed(() => {
    const items = [PLACEHOLDER.SOURCE_LANGUAGE, PLACEHOLDER.TARGET_LANGUAGE]
    if (settingsStore.getSetting(SETTINGS.USE_BATCH_TRANSLATION) !== 'true') {
        items.push(
            PLACEHOLDER.LINE_TO_TRANSLATE,
            PLACEHOLDER.CONTEXT_BEFORE,
            PLACEHOLDER.CONTEXT_AFTER
        )
    }
    return items
})

const aiPrompt = computed({
    get: () => (settingsStore.getSetting(SETTINGS.AI_PROMPT) as string) ?? '',
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_PROMPT, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>
