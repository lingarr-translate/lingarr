<template>
    <div class="group relative">
        <button
            type="button"
            class="border-accent hover:bg-accent flex w-full cursor-pointer items-center justify-between rounded-md border px-2 py-1 text-xs transition-colors hover:text-white"
            @click="$emit('insert', placeholder)">
            <span>{{ placeholderText }}</span>
            <ArrowDown class="ml-1 h-3.5 w-3.5" />
        </button>
        <div
            class="border-accent bg-primary pointer-events-none invisible absolute bottom-full left-0 z-10 mb-2 w-56 rounded-md border p-3 text-xs opacity-0 shadow-lg transition-all duration-200 group-hover:visible group-hover:opacity-100">
            <h4 class="text-accent mb-1 font-semibold">{{ title }}</h4>
            <p>{{ description }}</p>
            <div
                class="border-accent bg-primary absolute -bottom-1 left-4 h-2 w-2 rotate-45 transform border-r border-b"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
import ArrowDown from '@/components/icons/ArrowDown.vue'

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
