<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 xl:grid-cols-2 2xl:grid-cols-3">
        <LanguageSettings />
        <AutomationSettings />
    </div>
</template>

<script setup lang="ts">
import LanguageSettings from '@/components/features/settings/LanguageSettings.vue'
import AutomationSettings from '@/components/features/settings/AutomationSettings.vue'
</script>
