<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 xl:grid-cols-2 2xl:grid-cols-3">
        <IntegrationSettings />
        <ServicesSettings />
    </div>
</template>

<script setup lang="ts">
import IntegrationSettings from '@/components/features/settings/IntegrationSettings.vue'
import ServicesSettings from '@/components/features/settings/ServicesSettings.vue'
</script>
