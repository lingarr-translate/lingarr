<template>
    <div class="ui-panel relative min-w-0 p-5 sm:p-6">
        <h2
            v-if="title"
            class="text-primary-content mb-4 text-lg font-semibold tracking-tight sm:text-xl">
            {{ title }}
        </h2>
        <p v-if="$slots.description" class="text-secondary-content mb-6 text-sm leading-relaxed">
            <slot name="description"></slot>
        </p>

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
