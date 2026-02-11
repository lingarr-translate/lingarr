<template>
    <div class="group relative">
        <button
            type="button"
            class="flex w-full cursor-pointer items-center justify-between rounded-md border border-accent px-2 py-1 text-xs transition-colors hover:bg-accent hover:text-white"
            @click="$emit('insert', placeholder)">
            <span>{{ placeholderText }}</span>
            <ArrowUp class="ml-1 h-3.5 w-3.5" />
        </button>
        <div
            class="pointer-events-none invisible absolute bottom-full left-0 z-10 mb-2 w-56 rounded-md border border-accent bg-primary p-3 text-xs opacity-0 shadow-lg transition-all duration-200 group-hover:visible group-hover:opacity-100">
            <h4 class="mb-1 font-semibold text-accent">{{ title }}</h4>
            <p>{{ description }}</p>
            <div
                class="absolute -bottom-1 left-4 h-2 w-2 rotate-45 transform border-b border-r border-accent bg-primary"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
import ArrowUp from '@/components/icons/ArrowUp.vue'

defineProps<{
    placeholder: string
    placeholderText: string
    title: string
    description: string
    label?: string
}>()

defineEmits<{
    (e: 'insert', placeholder: string): void
}>()
</script>
