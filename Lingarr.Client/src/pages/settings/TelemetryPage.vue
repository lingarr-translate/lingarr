<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-6 p-4 min-[1800px]:grid-cols-3 sm:p-6 2xl:grid-cols-2">
        <TelemetrySettings />
    </div>
</template>

<script setup lang="ts">
import TelemetrySettings from '@/components/features/settings/TelemetrySettings.vue'
</script>
