<template>
    <input
        :value="modelValue.searchQuery"
        type="text"
        placeholder="Search media..."
        class="block w-full max-w-96 rounded-md border border-accent bg-primary py-1 pl-8 pr-4 text-sm text-primary-content placeholder-primary/60 focus:border-accent focus:ring-2 focus:ring-accent"
        @input="search" />
    <svg
        class="absolute left-2 top-1/2 h-4 w-4 -translate-y-1/2 transform text-accent-content"
        viewBox="0 0 20 20"
        fill="currentColor">
        <path
            fill-rule="evenodd"
            d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"
            clip-rule="evenodd" />
    </svg>
</template>

<script lang="ts" setup>
import { IFilter } from '@/ts'

const emit = defineEmits(['update:modelValue'])
const { modelValue } = defineProps<{
    modelValue: IFilter
}>()

const search = (event: Event) => {
    emit('update:modelValue', {
        ...modelValue,
        searchQuery: (event.target as HTMLSelectElement).value
    })
}
</script>
