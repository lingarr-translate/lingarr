<template>
    <CardComponent title="System Prompt">
        <template #description>
            Define custom instructions (system prompt) and an optional context prompt to control AI
            response logic.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">System prompt</span>
                    Define the AI’s behavior and tone by setting global instructions.
                </div>
                <AiSystemPrompt @save="saveNotification?.show()" />
            </div>
        </template>
    </CardComponent>
</template>
<script setup lang="ts">
import { ref } from 'vue'
import CardComponent from '@/components/common/CardComponent.vue'
import AiSystemPrompt from '@/components/features/settings/services/AiSystemPrompt.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
</script>
