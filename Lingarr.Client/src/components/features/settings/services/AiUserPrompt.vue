<template>
    <TextAreaComponent
        v-model="aiUserPrompt"
        :rows="10"
        :min-height="100"
        :placeholders="[
            PLACEHOLDER.LINE_TO_TRANSLATE,
            PLACEHOLDER.SOURCE_LANGUAGE,
            PLACEHOLDER.TARGET_LANGUAGE,
            PLACEHOLDER.CONTEXT_BEFORE,
            PLACEHOLDER.CONTEXT_AFTER
        ]"
        :required-placeholders="['{lineToTranslate}']"
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

const aiUserPrompt = computed({
    get: () => (settingsStore.getSetting(SETTINGS.AI_USER_PROMPT) as string) ?? '',
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.AI_USER_PROMPT, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>
