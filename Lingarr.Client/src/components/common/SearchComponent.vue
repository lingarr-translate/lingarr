<template>
    <div class="relative w-full sm:max-w-sm">
        <input
            :value="modelValue.searchQuery"
            type="search"
            aria-label="Search media"
            placeholder="Search media..."
            class="ui-input block w-full px-11 py-2 text-sm [&::-webkit-search-cancel-button]:appearance-none"
            @input="search" />
        <SearchIcon
            class="text-primary-content absolute top-1/2 left-4 h-4 w-4 -translate-y-1/2 transform" />
        <button
            v-if="modelValue.searchQuery"
            type="button"
            class="ui-icon-button absolute top-0 right-0"
            aria-label="Clear search"
            v-tooltip="'Clear search'"
            @click="clear">
            <TimesIcon class="h-4 w-4" />
        </button>
    </div>
</template>

<script lang="ts" setup>
import { IFilter } from '@/ts'
import SearchIcon from '@/components/icons/SearchIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'

const emit = defineEmits(['update:modelValue'])
const { modelValue } = defineProps<{
    modelValue: IFilter
}>()

const searchQuery = (value: string) => {
    emit('update:modelValue', {
        ...modelValue,
        searchQuery: value
    })
}

const search = (event: Event) => {
    searchQuery((event.target as HTMLInputElement).value)
}

const clear = () => {
    searchQuery('')
}
</script>
