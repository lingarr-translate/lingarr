<template>
    <input
        :value="modelValue.searchQuery"
        type="text"
        placeholder="Search media..."
        class="block w-full max-w-96 rounded-md border border-accent bg-primary py-1 pl-8 pr-4 text-sm text-primary-content placeholder-primary/60 focus:border-accent focus:ring-2 focus:ring-accent"
        @input="search" />
    <SearchIcon
        class="absolute left-2 top-1/2 h-4 w-4 -translate-y-1/2 transform text-accent-content" />
</template>

<script lang="ts" setup>
import { IFilter } from '@/ts'
import SearchIcon from '@/components/icons/SearchIcon.vue'

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
