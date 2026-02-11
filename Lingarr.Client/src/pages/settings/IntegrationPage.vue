<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2 xl:grid-cols-2 2xl:grid-cols-3">
        <IntegrationSettings />
        <WebhookInstructions />
    </div>
</template>

<script setup lang="ts">
import IntegrationSettings from '@/components/features/settings/IntegrationSettings.vue'
import WebhookInstructions from '@/components/features/settings/WebhookInstructions.vue'
</script>
