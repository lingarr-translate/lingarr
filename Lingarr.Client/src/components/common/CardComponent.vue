<template>
    <div class="bg-linear-to-br relative rounded-md from-secondary to-tertiary p-6 shadow-md">
        <h2 v-if="title" class="mb-2 text-2xl font-bold text-primary-content">{{ title }}</h2>
        <p class="text-secondary-content/80 mb-6"><slot name="description"></slot></p>

        <div class="space-y-4">
            <slot name="content"></slot>
        </div>
    </div>
</template>

<script setup lang="ts">
const { title } = defineProps<{
    title: string
}>()
</script>
