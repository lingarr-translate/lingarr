<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2 xl:grid-cols-2 2xl:grid-cols-3">
        <ServicesSettings />
        <TranslationSettings />
    </div>
</template>

<script setup lang="ts">
import ServicesSettings from '@/components/features/settings/ServicesSettings.vue'
import TranslationSettings from '@/components/features/settings/TranslationSettings.vue'
</script>
