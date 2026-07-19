<template>
    <CardComponent title="User Prompt">
        <template #description>
            Define the user message sent with each translation.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div v-if="useBatchTranslation !== 'true'" class="space-y-4">
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">User prompt</span>
                        Use the context placeholders to include lines before and after the current
                        subtitle, helping the AI generate a more accurate translation.
                    </div>
                    <AiUserPrompt @save="saveNotification?.show()" />

                    <InputComponent
                        v-model="contextBefore"
                        :type="INPUT_TYPE.NUMBER"
                        :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                        label="Context before"
                        @update:validation="(val) => (isValid.contextBefore = val)" />
                    <InputComponent
                        v-model="contextAfter"
                        :type="INPUT_TYPE.NUMBER"
                        :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                        label="Context after"
                        @update:validation="(val) => (isValid.contextAfter = val)" />
                </div>
                <div v-else class="text-xs">
                    The user prompt is not applied when sending subtitles in batch; the subtitle
                    batch is sent directly as the user message.
                </div>
            </div>
        </template>
    </CardComponent>
</template>
<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { INPUT_TYPE, INPUT_VALIDATION_TYPE, SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import AiUserPrompt from '@/components/features/settings/services/AiUserPrompt.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
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
