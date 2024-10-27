<template>
    <TextAreaComponent
        v-model="aiPrompt"
        :rows="10"
        label="Prompt"
        @update:validation="(val) => (isValid = val)" />
    <p class="text-xs">
        The prompt is used to start a translation with an AI model. Note that extensive prompts cost
        more, while simple prompts may lead to more hallucinations.
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
